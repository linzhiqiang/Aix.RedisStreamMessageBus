
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System.Linq;
using Aix.RedisStreamMessageBus.RedisImpl;
using Aix.RedisStreamMessageBus.Model;
using Aix.RedisStreamMessageBus.Foundation;
using Aix.MultithreadExecutor;

namespace Aix.RedisStreamMessageBus.BackgroundProcess
{
    internal class WorkerProcess : IBackgroundProcess
    {
        private IServiceProvider _serviceProvider;
        private ILogger<WorkerProcess> _logger;
        private RedisMessageBusOptions _options;
        private RedisStorage _redisStorage;
        private ConnectionMultiplexer _redis = null;
        private IDatabase _database;
        private MyMultithreadTaskExecutor _taskExecutor;

        private string _topic;
        private string _groupName;
        private string _consumerName;

        public event Func<JobData, Task<bool>> OnMessage;
        int BatchCount = 10; //一次拉取多少条  目前只能取一个，区多个会导致取回来没有执行的 空闲时间增加导致错误处理有问题
        private volatile bool _isStart = true;
        public WorkerProcess(IServiceProvider serviceProvider, string topic, string groupName, string consumerName)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetService<ILogger<WorkerProcess>>();
            _options = _serviceProvider.GetService<RedisMessageBusOptions>();
            _redis = _serviceProvider.GetService<ConnectionMultiplexer>();
            _database = _redis.GetDatabase();
            _taskExecutor = _serviceProvider.GetService<MyMultithreadTaskExecutor>();

            _topic = topic;
            _groupName = groupName;
            _consumerName = consumerName;
            _redisStorage = _serviceProvider.GetService<RedisStorage>();

            BatchCount = _options.PerBatchPullCount > 0 ? _options.PerBatchPullCount : 100;

        }

        public async Task Start(BackgroundProcessContext context)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// 服务启动时先处理pel中的数据（上次服务结束时拉去到但是未执行的数据/未ack）
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task ProcessPel(BackgroundProcessContext context)
        {
            while (!context.IsShutdownRequested)
            {
                //读取pending 中的消息
                var list = await _database.StreamReadGroupAsync(_topic, _groupName, _consumerName, "0-0", 20);
                if (list == null || list.Length == 0)
                {
                    return;
                }

                await ProcessList(list, false);//每次启动时处理pel中的数据，要同步处理，等处理结束，再拉去新消息处理
            }
        }

        public void Dispose()
        {
            _isStart = false;
            _logger.LogInformation("RedisMessageBus即时任务已结束......");
        }

        public async Task Execute(BackgroundProcessContext context)
        {
            if (_taskExecutor.GetTaskCount() > _options.TaskExecutorMaxTaskCount) //队列任务积压超过1000就不要再拉取了，不然也处理不过来
            {
                await TaskEx.DelayNoException(TimeSpan.FromMilliseconds(_options.ConsumePullIntervalMillisecond), context.CancellationToken);
                return;
            }

            ////   >：读取未分配给其他消费者的消息(未被拉去的)  0-0或id： 读取pending 中的消息
            var list = await _database.StreamReadGroupAsync(_topic, _groupName, _consumerName, ">", BatchCount);
            if (list.Length == 0)
            {
                await TaskEx.DelayNoException(TimeSpan.FromMilliseconds(_options.ConsumePullIntervalMillisecond), context.CancellationToken);
                return;
            }

            await ProcessList(list, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <param name="asyncExecute">是否异步执行</param>
        /// <returns></returns>
        private async Task ProcessList(StreamEntry[] list, bool asyncExecute)
        {
            foreach (var item in list)
            {
                if (_isStart == false) return;//及时关闭
                JobData jobData = ParseJobData(item);
                if (asyncExecute)
                {
                    //相同routeKey的任务进入同一个线程处理
                    _taskExecutor.GetSingleThreadTaskExecutor(jobData.RouteKey).Execute(async (state) => //进入本地多线程执行器中
                    {
                        var thisObj = (WorkerProcess)state;
                        await thisObj.HandleWrap(item, jobData);
                    }, this);
                }
                else
                {
                    await HandleWrap(item, jobData);
                }
            }
        }

        private async Task HandleWrap(StreamEntry streamEntry, JobData jobData)
        {
            try
            {
                await Handle(streamEntry, jobData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"RedisMessageBus消费失败,topic:{_topic}，groupName:{_groupName}");
            }
            finally
            {
                await _database.StreamAcknowledgeAsync(_topic, _groupName, streamEntry.Id);
            }
        }

        private async Task Handle(StreamEntry streamEntry, JobData jobData)
        {
            //JobData jobData = ParseJobData(streamEntry);
            if (jobData == null) return;

            if (!string.IsNullOrEmpty(jobData.ErrorGroup) && _groupName != jobData.ErrorGroup) //是出错的任务需要重试，但不是该组的不处理
            {
                return;
            }
            var isSuccess = await HandleMessage(jobData);
            if (!isSuccess && jobData.ErrorCount < _options.MaxErrorReTryCount) //需要重试
            {
                var delaySecond = GetDelaySecond(jobData.ErrorCount);
                jobData.ErrorCount++;
                jobData.ErrorGroup = _groupName;

                await _redisStorage.EnqueueDealy(jobData, TimeSpan.FromSeconds(delaySecond));
                _logger.LogInformation($"RedisMessageBus消费失败,topic:{jobData.Topic},{delaySecond}秒后将进行{jobData.ErrorCount }次重试");
            }
        }

        private async Task<bool> HandleMessage(JobData jobData)
        {
            var isSuccess = true;
            try
            {
                isSuccess = await OnMessage(jobData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"RedisMessageBus消费失败,topic:{jobData.Topic}");
                isSuccess = false;
            }
            return isSuccess;
        }

        private JobData ParseJobData(StreamEntry streamEntry)
        {
            JobData jobData = null;
            try
            {
                jobData = JobData.ToJobData(streamEntry.Values);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"RedisMessageBus解析任务数据报错{_topic},{_groupName}");
            }
            return jobData;
        }

        private int GetDelaySecond(int errorCount)
        {
            //errorCount = errorCount > 0 ? errorCount - 1 : errorCount;
            var retryStrategy = _options.GetRetryStrategy();
            if (errorCount < retryStrategy.Length)
            {
                return retryStrategy[errorCount];
            }
            return retryStrategy[retryStrategy.Length - 1];
        }
    }
}
