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
        CancellationTokenSource StoppingSource = new CancellationTokenSource();
        public BackgroundProcessContext()
        {
            CancellationToken = StoppingSource.Token;
        }
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// 订阅topic列表
        /// </summary>
        public ConcurrentBag<SubscriberTopicInfo> SubscriberTopics { get; } = new ConcurrentBag<SubscriberTopicInfo>();

        public bool IsShutdownRequested => CancellationToken.IsCancellationRequested;

        public void Stop()
        {
            lock (StoppingSource)
            {
                try
                {
                    ExecuteHandlers(StoppingSource);
                }
                catch (Exception)
                {

                }
            }
        }

        private void ExecuteHandlers(CancellationTokenSource cancel)
        {
            // Noop if this is already cancelled
            if (cancel.IsCancellationRequested)
            {
                return;
            }

            // Run the cancellation token callbacks
            cancel.Cancel(throwOnFirstException: false);
        }
    }
}
