using Aix.RedisStreamMessageBus.BackgroundProcess;
using Aix.RedisStreamMessageBus.Model;
using Aix.RedisStreamMessageBus.RedisImpl;
using Aix.RedisStreamMessageBus.Utils;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aix.RedisStreamMessageBus
{
    public class RedisMessageBus : IRedisMessageBus
    {
        private IServiceProvider _serviceProvider;
        private ILogger<RedisMessageBus> _logger;
        private RedisMessageBusOptions _options;
        private RedisStorage _redisStorage;
        ProcessExecuter _processExecuter;
        BackgroundProcessContext _backgroundProcessContext;

        private volatile bool _isInit = false;
        private HashSet<string> Subscribers = new HashSet<string>();

        public RedisMessageBus(IServiceProvider serviceProvider, ILogger<RedisMessageBus> logger
            , RedisMessageBusOptions options
            , RedisStorage redisStorage)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options;
            _redisStorage = redisStorage;


            _backgroundProcessContext = new BackgroundProcessContext();
            _processExecuter = new ProcessExecuter(_serviceProvider, _backgroundProcessContext);

        }

        public async Task PublishAsync(Type messageType, object message)
        {
            AssertUtils.IsNotNull(message, "消息不能null");
            var topic = GetTopic(messageType);
            var routeKey = Helper.GetRouteKey(message);
            var jobData = JobData.CreateJobData(topic, _options.Serializer.Serialize(message), routeKey);
            var result = await _redisStorage.StreamAdd(jobData);
            AssertUtils.IsNotEmpty(result, $"RedisMessageBus生产者数据失败,topic:{topic}");
        }

        public async Task PublishDelayAsync(Type messageType, object message, TimeSpan delay)
        {
            AssertUtils.IsNotNull(message, "消息不能null");
            if (delay <= TimeSpan.Zero)
            {
                await PublishAsync(messageType, message);
                return;
            }
            var topic = GetTopic(messageType);
            var routeKey = Helper.GetRouteKey(message);
            var jobData = JobData.CreateJobData(topic, _options.Serializer.Serialize(message), routeKey);
            var result = await _redisStorage.EnqueueDealy(jobData, delay);
            AssertUtils.IsTrue(result, $"RedisMessageBus生产者数据失败,topic:{topic}");
        }

        public async Task SubscribeAsync<T>(Func<T, Task> handler, SubscribeOptions subscribeOptions = null, CancellationToken cancellationToken = default) where T : class
        {
            InitProcess();
            string topic = GetTopic(typeof(T));

            var groupId = subscribeOptions?.GroupId;
            groupId = !string.IsNullOrEmpty(groupId) ? groupId : RedisMessageBusOptions.DefaultGroupName;

            ValidateSubscribe(topic, groupId);

            _logger.LogInformation($"RedisMessageBus订阅Topic:{topic},GroupId:{groupId}");

            var groupPosition = !string.IsNullOrEmpty(subscribeOptions?.GroupPosition) ? subscribeOptions.GroupPosition : "$";//0-0 $  
            await _redisStorage.CreateConsumerGroupIfNotExist(topic, groupId, groupPosition); //StreamPosition.NewMessages
#pragma warning disable CS4014
            Task.Run(async () =>
            {
                Func<JobData, Task> action = async message =>
                {
                    var realObj = _options.Serializer.Deserialize<T>(message.Data);
                    await handler(realObj);
                };
                //先处理当前消费者组中的pel数据 上次重启没有执行完成的 一台机器一个consumerName
                var consumerName = GetConsumerName();
                var process = new WorkerProcess(_serviceProvider, topic, groupId, consumerName);
                process.OnMessage += action;
                await process.ProcessPel(_backgroundProcessContext);//目前用同一个消费者名称，所以只处理第一个pel就行了
                await _processExecuter.AddProcess(process, $"RedisMessageBus即时任务开始执行：{topic}......");

                _backgroundProcessContext.SubscriberTopics.Add(new SubscriberTopicInfo { Topic = topic, GroupName = groupId });//便于ErrorProcess处理
            });
#pragma warning restore CS4014
        }

        public void Dispose()
        {
            _processExecuter.Close();
        }

        #region private

        private void ValidateSubscribe(string topic, string groupId)
        {
            lock (Subscribers)
            {
                var key = $"{topic}_{groupId}";
                AssertUtils.IsTrue(!Subscribers.Contains(key), "重复订阅");
                Subscribers.Add(key);
            }
        }

        private string GetTopic(Type type)
        {
            return Helper.GetTopic(_options, type);
        }

        /// <summary>
        /// 只有消费端才启动这些
        /// </summary>
        private void InitProcess()
        {
            if (_isInit) return;
            lock (this)
            {
                if (_isInit) return;
                _isInit = true;
            }

            Task.Run(async () =>
            {
                await _processExecuter.AddProcess(new DelayedWorkProcess(_serviceProvider), "RedisMessageBus延迟任务开始执行......");
                //await _processExecuter.AddProcess(new ErrorWorkerProcess(_serviceProvider), "redis失败任务处理");
            });
        }

        private string GetConsumerName()
        {
            string consumerName;
            switch (_options.ConsumerNameType)
            {
                case ConsumerNameType.LocalIPPostfix:
                    consumerName = CreateConsumerNameByIP();
                    break;
                case ConsumerNameType.Default:
                    consumerName = RedisMessageBusOptions.DefaultConsumerName;
                    break;
                case ConsumerNameType.Custom:
                    AssertUtils.IsNotEmpty(_options.ConsumerName, "选择Custom模式，请配置ConsumerName");
                    consumerName = _options.ConsumerName;
                    break;
                default:
                    consumerName = CreateConsumerNameByIP();
                    break;
            }
            return consumerName;
        }

        private string CreateConsumerNameByIP()
        {
            return $"{RedisMessageBusOptions.DefaultConsumerName}_{IPUtils.IPToInt(IPUtils.GetLocalIP())}"; ;
        }

        #endregion

    }
}
