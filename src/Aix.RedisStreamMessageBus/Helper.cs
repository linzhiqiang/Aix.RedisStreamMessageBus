using Aix.RedisStreamMessageBus.Model;
using Aix.RedisStreamMessageBus.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Aix.RedisStreamMessageBus
{
    internal class Helper
    {
        /// <summary>
        /// topic缓存
        /// </summary>
        private static ConcurrentDictionary<Type, string> TopicCache = new ConcurrentDictionary<Type, string>();

        /// <summary>
        /// RouteKey缓存
        /// </summary>
        private static ConcurrentDictionary<Type, PropertyInfo> RouteKeyCache = new ConcurrentDictionary<Type, PropertyInfo>();

        public static string GetJobHashId(RedisMessageBusOptions options, string jobId)
        {
            return $"{options.TopicPrefix}jobdata:{jobId}";
        }

        public static string GetDelaySortedSetName(RedisMessageBusOptions options)
        {
            return $"{options.TopicPrefix}delay:jobid";
        }

        public static string GetDelayChannel(RedisMessageBusOptions options)
        {
            return $"{options.TopicPrefix}DelayJobChannel";
        }

       
        public static string GetTopic(RedisMessageBusOptions options, Type type)
        {
            string topicName = null;

            if (TopicCache.TryGetValue(type, out topicName))
            {
                return topicName;
            }

            topicName = type.Name;//默认等于该类型的名称
            var topicAttr = TopicAttribute.GetTopicAttribute(type);
            if (topicAttr != null && !string.IsNullOrEmpty(topicAttr.Name))
            {
                topicName = topicAttr.Name;
            }

            topicName = $"{options.TopicPrefix ?? ""}{topicName}";

            TopicCache.TryAdd(type, topicName);

            return topicName;
        }

        public static string GetRouteKey(object message)
        {
            if (message == null) return null;
            var type = message.GetType();
            PropertyInfo property = null;
            if (RouteKeyCache.TryGetValue(type, out property))
            {

            }
            else
            {
                property = AttributeUtils.GetProperty<RouteKeyAttribute>(message);
                RouteKeyCache.TryAdd(type, property);
            }

            var keyValue = property?.GetValue(message);
            return keyValue != null ? keyValue.ToString() : null;
        }
    }
}
