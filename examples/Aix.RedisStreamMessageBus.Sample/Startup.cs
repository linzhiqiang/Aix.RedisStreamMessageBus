using Aix.RedisStreamMessageBus.Sample.HostedService;
using Aix.RedisStreamMessageBus.Sample.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Aix.RedisStreamMessageBus.Sample
{
    public class Startup
    {
        internal static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            Console.WriteLine("*************************");
            Console.WriteLine(context.HostingEnvironment.EnvironmentName);
            Console.WriteLine("*************************");

            var options = CmdOptions.Options;
            services.AddSingleton(options);

            #region messagebus相关
            var redisMessageBusOptions = context.Configuration.GetSection("redis-messagebus").Get<RedisMessageBusOptions>();
            //异常处理，该异常是否需要进行任务重试  最好根据错误码判断，只有系统异常才进行重试

            //ConfigurationOptions options1 = new ConfigurationOptions();
            //options1.EndPoints.Add("127.0.0.1:6379");
            //options1.EndPoints.Add("127.0.0.1:6380");
            //ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(options1);
            //redisMessageBusOptions.ConnectionMultiplexer = connection;

            services.AddRedisMessageBus(redisMessageBusOptions); //list实现
                                                                 //services.AddRedisMessageBusPubSub(redisMessageBusOptions);//发布订阅实现

            #endregion



            if ((options.Mode & (int)ClientMode.Consumer) > 0)
            {
                services.AddHostedService<MessageBusConsumeService>();
            }
            if ((options.Mode & (int)ClientMode.Producer) > 0)
            {
                services.AddHostedService<MessageBusProduerService>();
            }
        }

        #region private 




        #endregion
    }
}
