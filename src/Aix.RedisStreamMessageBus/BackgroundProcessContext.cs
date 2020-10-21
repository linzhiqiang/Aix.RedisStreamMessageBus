using Aix.RedisStreamMessageBus.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Aix.RedisStreamMessageBus
{
    public class BackgroundProcessContext
    {
        public BackgroundProcessContext(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// 订阅topic列表
        /// </summary>
        public ConcurrentBag<SubscriberTopicInfo> SubscriberTopics { get; } = new ConcurrentBag<SubscriberTopicInfo>();

        public bool IsShutdownRequested => CancellationToken.IsCancellationRequested;
    }
}
