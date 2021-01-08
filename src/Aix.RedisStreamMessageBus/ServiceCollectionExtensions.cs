using Aix.RedisStreamMessageBus.RedisImpl;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using Aix.MultithreadExecutor;
using Microsoft.Extensions.Logging;
using Aix.RedisStreamMessageBus.Foundation;
using System.Threading.Tasks;
using Aix.RedisStreamMessageBus.Utils;

namespace Aix.RedisStreamMessageBus
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRedisMessageBus(this IServiceCollection services, RedisMessageBusOptions options)
        {
            services.AddAddMultithreadExecutor(options.DefaultConsumerThreadCount);
            AddService(services, options);
            services.AddSingleton<IRedisMessageBus, RedisMessageBus>();
            return services;
        }

        public static IServiceCollection AddRedisMessageBusPubSub(this IServiceCollection services, RedisMessageBusOptions options)
        {
            AddService(services, options);
            services.AddSingleton<IRedisMessageBus, RedisMessageBus_Subscriber>();
            return services;
        }

        private static IServiceCollection AddService(IServiceCollection services, RedisMessageBusOptions options)
        {
            if (options.ConnectionMultiplexer != null)
            {
                services.AddSingleton(options.ConnectionMultiplexer);
            }
            else if (!string.IsNullOrEmpty(options.ConnectionString))
            {
                var redis = ConnectionMultiplexer.Connect(options.ConnectionString);
                services.AddSingleton(redis);
            }
            else
            {
                throw new Exception("ConnectionMultiplexer或RedisConnectionString为空");
            }

            services.AddSingleton(options);
            services.AddSingleton<RedisStorage>();
            return services;
        }

        private static void AddAddMultithreadExecutor(this IServiceCollection services, int consumerThreadCount)
        {
            AssertUtils.IsTrue(consumerThreadCount > 0, "RedisMessageBus消费者线程数必须大于0");
            services.AddSingleton(serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<MyMultithreadTaskExecutor>>();
                var taskExecutor = new MyMultithreadTaskExecutor(options =>
                {
                    options.ThreadCount = consumerThreadCount;// Environment.ProcessorCount * 2;
                });
                taskExecutor.OnException += ex =>
                {
                    logger.LogError(ex, "RedisMessageBus本地多线程任务执行器执行出错");
                    return Task.CompletedTask;
                };
                taskExecutor.Start();
                logger.LogInformation($"RedisMessageBus本地多线程任务执行器开始 ThreadCount={taskExecutor.ThreadCount}......");
                return taskExecutor;
            });
        }
    }
}
