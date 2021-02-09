using Aix.RedisStreamMessageBus.RedisImpl;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Aix.RedisStreamMessageBus.Model;
using System.Linq;
using Aix.RedisStreamMessageBus.Foundation;

namespace Aix.RedisStreamMessageBus.BackgroundProcess
{
    /// <summary>
    /// 暂时没用
    /// </summary>
    internal class ErrorWorkerProcess : IBackgroundProcess
    {
        private IServiceProvider _serviceProvider;
        private ILogger<WorkerProcess> _logger;
        private RedisMessageBusOptions _options;
        private RedisStorage _redisStorage;
        private ConnectionMultiplexer _redis = null;
        private IDatabase _database;


        int BatchCount = 10; //一次拉取多少条
        private volatile bool _isStart = true;
        long ExecuteTimeoutMilliseconds = 120 * 1000;
        int RetryCount = 5; //超时次数

        public ErrorWorkerProcess(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetService<ILogger<WorkerProcess>>();
            _options = _serviceProvider.GetService<RedisMessageBusOptions>();
            _redis = _serviceProvider.GetService<ConnectionMultiplexer>();
            _database = _redis.GetDatabase();
            _redisStorage = _serviceProvider.GetService<RedisStorage>();

            ExecuteTimeoutMilliseconds = _options.ExecuteTimeoutSecond * 1000;
        }
        public Task Start(BackgroundProcessContext context)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _isStart = false;
            _logger.LogInformation("关闭后台任务：redis失败任务处理");
        }

        public async Task Execute(BackgroundProcessContext context)
        {
            // 循环所有的stream和group进行处理，订阅时保存 stream和对应的group（一个stream可能有多个groupo）

            List<long> waitTimes = new List<long>();
            await _redisStorage.Lock("pendingreadlock", TimeSpan.FromSeconds(60), async () =>
               {
                   foreach (var item in context.SubscriberTopics)
                   {
                       if (_isStart == false) return;//及时关闭
                       var currentWaitTime = await ProcessPel(item);
                       waitTimes.Add(currentWaitTime);
                   }
               }, () => Task.CompletedTask);

            var waitTime = waitTimes.Any() ? waitTimes.Min() : ExecuteTimeoutMilliseconds / 2;
            await TaskEx.DelayNoException(TimeSpan.FromMilliseconds(waitTime), context.CancellationToken);
        }

        private async Task<long> ProcessPel(SubscriberTopicInfo subscriberTopicInfo)
        {
            //https://blog.csdn.net/weixin_39166924/article/details/104381231

            //每个消费者一个pel
            var streamName = subscriberTopicInfo.Topic;
            var groupName = subscriberTopicInfo.GroupName;
            long waitTime = 0;
            while (true)
            {
                //读取该消费组中所有消费者的pel消息，这里有个问题：消费者一次拉去100条数据时，这100条数据需要消费很长时间时，这里就认为超时了明显不妥。
                var pelList = await _database.StreamPendingMessagesAsync(streamName, groupName, BatchCount, RedisValue.Null);//StreamPendingMessageInfo
                if (pelList.Length == 0) break;
                //pelList = pelList.OrderBy(x=>x.MessageId).ToArray(); //本身应该是正序的
                foreach (var item in pelList)
                {
                    if (_isStart == false) return waitTime;//及时关闭
                    if (item.IdleTimeInMilliseconds >= ExecuteTimeoutMilliseconds) //处理时间超过120秒的认为当前消息处理时没有得到确认，可能是某个消费者关闭时未确认的消息，这里重新消费
                    {
                        await TimeoutTaskProcess(subscriberTopicInfo, item);
                    }
                    //else if (item.IdleTimeInMilliseconds < ExecuteTimeoutMilliseconds / 3) //一个优化，小于三分之一时间时 就停止该pel处理，后面的数据肯定更小
                    //{
                    //    return;
                    //}
                    else
                    {
                        waitTime = ExecuteTimeoutMilliseconds - item.IdleTimeInMilliseconds;
                        return waitTime;
                    }

                }
            }
            return waitTime;
        }

        private async Task TimeoutTaskProcess(SubscriberTopicInfo subscriberTopicInfo, StreamPendingMessageInfo item)
        {
            var streamName = subscriberTopicInfo.Topic;
            var groupName = subscriberTopicInfo.GroupName;

            var tempList = await _database.StreamRangeAsync(streamName, item.MessageId, item.MessageId, 1);
            if (tempList == null || tempList[0].Id != item.MessageId) return;

            var currentItem = tempList[0];
            JobData jobData = null;
            try
            {
                jobData = JobData.ToJobData(currentItem.Values);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"redis解析任务数据报错{streamName},{groupName}");
            }

            if (jobData == null || jobData.ErrorCount >= RetryCount)
            {
                _logger.LogError($"任务重启重试[任务超时或服务]{RetryCount}次依然失败,streamName:{streamName},groupName:{groupName},ErrorCount:{jobData.ErrorCount}");
                await _database.StreamAcknowledgeAsync(streamName, groupName, currentItem.Id);
                return;
            }

            _logger.LogError($"任务重新执行[任务超时或服务重启]streamName:{streamName},groupName:{groupName},ErrorCount:{jobData.ErrorCount}");

            jobData.ErrorCount++;
            jobData.ErrorGroup = groupName;

            var trans = _database.CreateTransaction();
#pragma warning disable CS4014
            _database.StreamAddAsync(streamName, jobData.ToNameValueEntries(), messageId: null, maxLength: _options.StreamMaxLength, useApproximateMaxLength: true);
            _database.StreamAcknowledgeAsync(streamName, groupName, currentItem.Id);
#pragma warning restore CS4014
            await trans.ExecuteAsync();
        }
    }
}
