using Aix.RedisStreamMessageBus.Serializer;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace Aix.RedisStreamMessageBus
{
    public class RedisMessageBusOptions
    {
        public static string DefaultGroupName = "default-group";
        //public static string DefaultConsumerName = "consumer";
        private int[] DefaultRetryStrategy = new int[] { 1, 5, 30, 60, 2 * 60, 5 * 60, 10 * 60 };
        public RedisMessageBusOptions()
        {
            this.TopicPrefix = "dog:messagebus:";
            this.Serializer = new MessagePackSerializer();
        }


        /// <summary>
        /// RedisConnectionString和ConnectionMultiplexer传一个即可
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        ///  RedisConnectionString和ConnectionMultiplexer传一个即可
        /// </summary>
        public ConnectionMultiplexer ConnectionMultiplexer { get; set; }

        /// <summary>
        /// topic前缀，为了防止重复，建议用项目名称
        /// </summary>
        public string TopicPrefix { get; set; }

        /// <summary>
        /// 自定义序列化，默认为MessagePack
        /// </summary>
        public ISerializer Serializer { get; set; }

        public int StreamMaxLength { get; set; } = 1000000;

        public string DefaultConsumerName { get; set; } = "consumer";

        /// <summary>
        /// 默认每个类型的消费线程数 默认2个
        /// </summary>
        public int DefaultConsumerThreadCount { get; set; } = 2;

        /// <summary>
        /// 消费者没数据时 间隔时间(没数据时) 100
        /// </summary>
        public int ConsumeIntervalMillisecond { get; set; } = 100;

        /// <summary>
        /// 执行超时时间，超过该时间，任务存在被重新执行的风险 默认60秒
        /// </summary>
        public int ExecuteTimeoutSecond { get; set; } = 60;

        /// <summary>
        /// 任务数据有效期 默认7天=168 单位  小时
        /// </summary>
        public int DataExpireHour { get; set; } = 168;


        /// <summary>
        /// 延迟任务处理间隔(没数据时)
        /// </summary>
        public int DelayIntervalMillisecond { get; set; } = 1000;

        /// <summary>
        /// 最大错误重试次数 默认10次
        /// </summary>
        public int MaxErrorReTryCount { get; set; } = 10;

        /// <summary>
        /// 是否为重试异常
        /// </summary>
        public static Func<Exception, Task<bool>> IsRetry { get; set; }

        /// <summary>
        /// 失败重试延迟策略 单位：秒 ,不要直接调用请调用GetRetryStrategy()  默认失败次数对应值延迟时间[ 1, 10, 30, 60, 2 * 60, 2 * 60, 2 * 60, 5 * 60, 5 * 60,10*60   ];
        /// </summary>
        public int[] RetryStrategy { get; set; }

        public int[] GetRetryStrategy()
        {
            if (RetryStrategy == null || RetryStrategy.Length == 0) return DefaultRetryStrategy;
            return RetryStrategy;
        }

    }
}
