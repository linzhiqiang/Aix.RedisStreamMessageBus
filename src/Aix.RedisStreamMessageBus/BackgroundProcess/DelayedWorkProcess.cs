using Aix.RedisStreamMessageBus.RedisImpl;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Aix.RedisStreamMessageBus.Utils;
using Aix.RedisStreamMessageBus.Model;

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
        int BatchCount = 100; //一次拉取多少条
        private volatile bool _isStart = true;

        private TimeSpan lockTimeSpan = TimeSpan.FromMinutes(1);
        public DelayedWorkProcess(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetService<ILogger<DelayedWorkProcess>>();
            _options = _serviceProvider.GetService<RedisMessageBusOptions>();
            _redisStorage = _serviceProvider.GetService<RedisStorage>();
        }


        public Task Start(BackgroundProcessContext context)
        {
            return Task.CompletedTask;
        }
        public void Dispose()
        {
            _isStart = false;
            _logger.LogInformation("关闭后台任务：redis延迟任务处理");
        }

        public async Task Execute(BackgroundProcessContext context)
        {
            var lockKey = $"{_options.TopicPrefix}delay:lock";
            long delay = 1000; //毫秒
            await _redisStorage.Lock(lockKey, lockTimeSpan, async () =>
            {
                var now = DateTime.Now;
                var maxScore = DateUtils.GetTimeStamp(now);
                var list = await _redisStorage.GetTopDueDealyJobId(maxScore + 1000, BatchCount); //多查询1秒的数据，便于精确控制延迟
                foreach (var item in list)
                {
                    if (context.IsShutdownRequested) return;
                    if (_isStart == false) return;//已经关闭了 就直接返回吧
                    if (item.Value > maxScore)
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
                        _logger.LogError(ex, $"redis解析延迟任务数据报错");
                    }

                    if (jobData != null)
                    {
                        await _redisStorage.DueDealyJobEnqueue(jobData);
                    }
                    else
                    {
                        _logger.LogError("延迟任务解析出错为空，这里就从hash中删除了");
                        await _redisStorage.KeyDelete(jobId);
                    }
                }

                if (list.Count > 0)
                {
                    delay = 0;
                }
            }, () => Task.CompletedTask);

            if (delay > 0)
            {
                await Task.Delay(Math.Min((int)delay, _options.DelayIntervalMillisecond), context.CancellationToken);
            }
        }

       
    }
}
