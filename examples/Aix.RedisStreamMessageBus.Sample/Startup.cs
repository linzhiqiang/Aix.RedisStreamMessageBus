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
            //控制台命令行启动参数解析
            var options = CmdOptions.Options;
            services.AddSingleton(options);

            #region messagebus相关

            var redisMessageBusOptions = context.Configuration.GetSection("redis-messagebus").Get<RedisMessageBusOptions>();
            //这里可以覆盖默认的序列化方式，内部默认是system.txt.json实现，这里替换为messagepack
            redisMessageBusOptions.Serializer = new MessagePackSerializerImpl();

            services.AddRedisMessageBus(redisMessageBusOptions); //stream实现
            //services.AddRedisMessageBusPubSub(redisMessageBusOptions);//发布订阅实现

            #endregion


            //这里为了测试方便，做了区分
            if ((options.Mode & (int)ClientMode.Consumer) > 0)
            {
                services.AddHostedService<MessageBusConsumeService>();
            }
            if ((options.Mode & (int)ClientMode.Producer) > 0)
            {
                services.AddHostedService<MessageBusProduerService>();
            }
        }

       
    }
}
