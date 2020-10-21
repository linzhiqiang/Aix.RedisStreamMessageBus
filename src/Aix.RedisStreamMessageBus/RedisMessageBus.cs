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


            _backgroundProcessContext = new BackgroundProcessContext(default(CancellationToken));
            _processExecuter = new ProcessExecuter(_serviceProvider, _backgroundProcessContext);

        }

        public async Task PublishAsync(Type messageType, object message)
        {
            AssertUtils.IsNotNull(message, "消息不能null");
            var topic = GetTopic(messageType);
            var jobData = JobData.CreateJobData(topic, _options.Serializer.Serialize(message));
            var result = await _redisStorage.StreamAdd(jobData);
            AssertUtils.IsNotEmpty(result, $"redis生产者数据失败,topic:{topic}");
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
            var jobData = JobData.CreateJobData(topic, _options.Serializer.Serialize(message));
            var result = await _redisStorage.EnqueueDealy(jobData, delay);
            AssertUtils.IsTrue(result, $"redis生产者数据失败,topic:{topic}");
        }

        public async Task SubscribeAsync<T>(Func<T, Task> handler, SubscribeOptions subscribeOptions = null, CancellationToken cancellationToken = default) where T : class
        {
            InitProcess();
            string topic = GetTopic(typeof(T));

            var groupId = subscribeOptions?.GroupId;
            groupId = !string.IsNullOrEmpty(groupId) ? groupId : RedisMessageBusOptions.DefaultGroupName;

            var threadCount = subscribeOptions?.ConsumerThreadCount ?? 0;
            threadCount = threadCount > 0 ? threadCount : _options.DefaultConsumerThreadCount;
            AssertUtils.IsTrue(threadCount > 0, "消费者线程数必须大于0");

            ValidateSubscribe(topic, groupId);

            _logger.LogInformation($"订阅Topic:{topic},GroupId:{groupId},ConsumerThreadCount:{threadCount}");

            var groupPosition = !string.IsNullOrEmpty(subscribeOptions?.GroupPosition) ? subscribeOptions.GroupPosition : "$";//0-0 $  
            await _redisStorage.CreateConsumerGroupIfNotExist(topic, groupId, groupPosition); //StreamPosition.NewMessages
#pragma warning disable CS4014
            Task.Run(async() =>
            {
                Func<JobData, Task> action = async message =>
                {
                    var realObj = _options.Serializer.Deserialize<T>(message.Data);
                    await handler(realObj);
                };
                //先处理当前消费者组中的pel数据 上次重启没有执行完成的 一台机器一个consumerName
                var consumerName = $"{_options.TopicPrefix}{_options.DefaultConsumerName}_{IPUtils.IPToInt(IPUtils.GetLocalIP())}";
                //var pelProcess = new WorkerProcess(_serviceProvider, topic, groupId, consumerName);
                //pelProcess.OnMessage += action;
                //await pelProcess.ProcessPel();



                for (int i = 0; i < threadCount; i++)
                {
                    var process = new WorkerProcess(_serviceProvider, topic, groupId, consumerName);
                    process.OnMessage += action;
                    if (i == 0) //目前用同一个消费者名称，所以只处理第一个pel就行了
                    {
                        await process.ProcessPel();
                    }
                    await _processExecuter.AddProcess(process, $"redis即时任务处理：{topic}");
                   
                }
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
            string topicName = type.Name;

            var topicAttr = TopicAttribute.GetTopicAttribute(type);
            if (topicAttr != null && !string.IsNullOrEmpty(topicAttr.Name))
            {
                topicName = topicAttr.Name;
            }

            return $"{_options.TopicPrefix ?? ""}{topicName}";
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
                await _processExecuter.AddProcess(new DelayedWorkProcess(_serviceProvider), "redis延迟任务处理");
                //await _processExecuter.AddProcess(new ErrorWorkerProcess(_serviceProvider), "redis失败任务处理");
            });
        }

        #endregion

    }
}
