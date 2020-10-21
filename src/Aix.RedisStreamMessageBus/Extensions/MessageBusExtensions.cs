using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Aix.RedisStreamMessageBus
{
    public static class MessageBusExtensions
    {
        public static Task PublishAsync<T>(this IRedisMessageBus messageBus, T message)
        {
            return messageBus.PublishAsync(typeof(T), message);
        }

        public static Task PublishDelayAsync<T>(this IRedisMessageBus messageBus, T message, TimeSpan delay)
        {
            return messageBus.PublishDelayAsync(typeof(T), message, delay);
        }
    }
}
