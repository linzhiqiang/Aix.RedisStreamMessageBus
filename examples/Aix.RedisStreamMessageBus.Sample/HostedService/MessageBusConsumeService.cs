using Aix.RedisStreamMessageBus.Model;
using Aix.RedisStreamMessageBus.Sample.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aix.RedisStreamMessageBus.Sample.HostedService
{
    public class MessageBusConsumeService : IHostedService
    {
        private ILogger<MessageBusConsumeService> _logger;
        public IRedisMessageBus _messageBus;

        private int Count = 0;
        public MessageBusConsumeService(ILogger<MessageBusConsumeService> logger, IRedisMessageBus messageBus)
        {
            _logger = logger;
            _messageBus = messageBus;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            List<Task> taskList = new List<Task>();

            taskList.Add(Subscribe(cancellationToken));
            //taskList.Add(SubscribeWithOptions(cancellationToken));
            await Task.WhenAll(taskList.ToArray());
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("StopAsync");
            return Task.CompletedTask;
        }

        private async Task Subscribe(CancellationToken cancellationToken)
        {
            try
            {
                await _messageBus.SubscribeAsync<BusinessMessage>(async (message) =>
                {
                    var current = Interlocked.Increment(ref Count);

                    _logger.LogInformation($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")}消费--1--数据：MessageId={message.MessageId},Content={message.Content},count={current}");
                    // await Task.Delay(10);
                    //throw new System.ArgumentException("333");
                    // throw new BizException(1,"不重试");
                    //throw new Exception("重试异常");
                    await Task.CompletedTask;
                }, null, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "");
            }
        }

        private async Task SubscribeWithOptions(CancellationToken cancellationToken)
        {
            try
            {
                //订阅配置可以灵活的增加参数 支持参数如下
                SubscribeOptions subscribeOptions = new SubscribeOptions();
                subscribeOptions.ConsumerThreadCount = 2;
                subscribeOptions.GroupId = "g";

                await _messageBus.SubscribeAsync<BusinessMessage>(async (message) =>
                {
                    var current = Interlocked.Increment(ref Count);
                    _logger.LogInformation($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")}消费--2--数据：MessageId={message.MessageId},Content={message.Content},count={current}");
                    // await Task.Delay(100);
                    await Task.CompletedTask;
                }, subscribeOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "");
            }
        }
    }
}
