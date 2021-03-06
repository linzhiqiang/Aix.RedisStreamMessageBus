﻿using Aix.RedisStreamMessageBus.RedisImpl;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Aix.RedisStreamMessageBus.Utils;
using Aix.RedisStreamMessageBus.Model;
using Aix.RedisStreamMessageBus.Foundation;

namespace Aix.RedisStreamMessageBus.BackgroundProcess
{
    /// <summary>
    /// 延迟任务处理
    /// </summary>
    internal class DelayedWorkProcess : IBackgroundProcess
    {
        private IServiceProvider _serviceProvider;
        private ILogger<DelayedWorkProcess> _logger;
        private RedisMessageBusOptions _options;
        private RedisStorage _redisStorage;
        private int BatchCount = 100; //一次拉取多少条
        private int PreReadSecond = 10; //提前读取多长数据
        private volatile bool _isStart = true;
        private string _delayTopicName;

        private TimeSpan lockTimeSpan = TimeSpan.FromMinutes(1);
        public DelayedWorkProcess(IServiceProvider serviceProvider,string delayTopicName)
        {
            _serviceProvider = serviceProvider;
            _delayTopicName = delayTopicName;
            _logger = _serviceProvider.GetService<ILogger<DelayedWorkProcess>>();
            _options = _serviceProvider.GetService<RedisMessageBusOptions>();
            _redisStorage = _serviceProvider.GetService<RedisStorage>();

            PreReadSecond = _options.DelayTaskPreReadSecond;
        }


        public Task Start(BackgroundProcessContext context)
        {
            return Task.CompletedTask;
        }
        public void Dispose()
        {
            _isStart = false;
            _logger.LogInformation("RedisMessageBus延迟任务已结束......");
        }

        public async Task Execute(BackgroundProcessContext context)
        {
            //var lockKey = $"{_options.TopicPrefix}delay:lock";
            var lockKey = $"{_delayTopicName}:lock";
            long delay = 0; //毫秒
            await _redisStorage.Lock(lockKey, lockTimeSpan, async () =>
            {
                var now = DateTime.Now;
                var maxScore = DateUtils.GetTimeStamp(now);
                var list = await _redisStorage.GetTopDueDealyJobId(_delayTopicName,maxScore + PreReadSecond * 1000, BatchCount); //多查询1秒的数据，便于精确控制延迟
                foreach (var item in list)
                {
                    if (context.IsShutdownRequested) return;
                    if (_isStart == false) return;//已经关闭了 就直接返回吧
                    if (item.Value > maxScore) //预拉去了PreReadSecond秒的数据，可能有还没到时间的
                    {
                        delay = item.Value - maxScore;
                        break;
                    }

                    var jobId = item.Key;
                    // 延时任务到期加入即时任务队列
                    var hashEntities = await _redisStorage.HashGetAll(Helper.GetJobHashId(_options, jobId));//这里要出错了呢
                    JobData jobData = null;
                    try
                    {
                        jobData = JobData.ToJobData(hashEntities);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"RedisMessageBus解析延迟任务数据报错");
                    }

                    if (jobData != null)
                    {
                        await _redisStorage.DueDealyJobEnqueue(_delayTopicName,jobData);
                    }
                    else
                    {
                        _logger.LogError("RedisMessageBus延迟任务解析出错为空，这里就从hash中删除了");
                        await _redisStorage.RemoveNullDealyJob(_delayTopicName,jobId);
                    }
                }

                if (list.Count == 0)//没有数据时
                {
                    delay = PreReadSecond * 1000;
                }

            }, async () => await TaskEx.DelayNoException(PreReadSecond * 1000, context.CancellationToken)); //出现并发也休息一会

            if (delay > 0)
            {
                var minDelay = Math.Min((int)delay, PreReadSecond * 1000);
                _redisStorage.WaitForDelayJob(TimeSpan.FromMilliseconds(minDelay), context.CancellationToken);
                //await TaskEx.DelayNoException(Math.Min((int)delay, PreReadSecond * 1000), context.CancellationToken);
            }
        }


    }
}
