using Aix.RedisStreamMessageBus.Sample.HostedService;
using Aix.RedisStreamMessageBus.Sample.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            var options = CmdOptions.Options;
            services.AddSingleton(options);

            #region messagebus相关
            var redisMessageBusOptions = context.Configuration.GetSection("redis-messagebus").Get<RedisMessageBusOptions>();
            //异常处理，该异常是否需要进行任务重试  最好根据错误码判断，只有系统异常才进行重试
            RedisMessageBusOptions.IsRetry = ex =>
            {
                if (ex.GetType() != typeof(BizException)) //不是业务异常进行重试
                {
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            };

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
