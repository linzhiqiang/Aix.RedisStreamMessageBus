using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using Aix.RedisStreamMessageBus.Foundation;

namespace Aix.RedisStreamMessageBus.BackgroundProcess
{
    /// <summary>
    /// 循环任务执行器
    /// </summary>
    public class ProcessExecuter
    {
        private IServiceProvider _serviceProvider;
        private ILogger<ProcessExecuter> _logger;
        BackgroundProcessContext _backgroundProcessContext;

        private IList<IBackgroundProcess> _backgroundProcesses = new List<IBackgroundProcess>();
        private volatile bool _isStart = true;


        public ProcessExecuter(IServiceProvider serviceProvider, BackgroundProcessContext backgroundProcessContext)
        {
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetService<ILogger<ProcessExecuter>>();
            _backgroundProcessContext = backgroundProcessContext;
        }

        public Task AddProcess(IBackgroundProcess backgroundProcess, string name)
        {
            _logger.LogInformation($"开启后台任务：{name}");
            _backgroundProcesses.Add(backgroundProcess);

            Task.Factory.StartNew(() => RunProcess(backgroundProcess), TaskCreationOptions.LongRunning);
            return Task.CompletedTask;
        }

        public Task Close()
        {
            this._isStart = false;
            _backgroundProcessContext.Stop();
            foreach (var item in _backgroundProcesses)
            {
                item.Dispose();
            }

            return Task.CompletedTask;
        }

        private async Task RunProcess(IBackgroundProcess process)
        {
            await process.Start(_backgroundProcessContext);

            while (_isStart && !_backgroundProcessContext.IsShutdownRequested)
            {
                try
                {
                    await process.Execute(_backgroundProcessContext); //内部控制异常
                }
                catch (OperationCanceledException)
                {
                }
                catch (RedisException ex)
                {
                    _logger.LogError(ex, "redis错误");
                    await TaskEx.DelayNoException(TimeSpan.FromSeconds(10), _backgroundProcessContext.CancellationToken);
                }
                catch (Exception ex)
                {
                    string errorMsg = $"执行任务{process.GetType().FullName}异常";
                    _logger.LogError(ex, errorMsg);
                }

            }
        }


    }
}
