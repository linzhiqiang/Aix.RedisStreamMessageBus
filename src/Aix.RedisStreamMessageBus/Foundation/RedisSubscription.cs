using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Aix.RedisStreamMessageBus.Foundation
{
    internal class RedisSubscription
    {
        private IServiceProvider _serviceProvider;
        private readonly ManualResetEvent _mre = new ManualResetEvent(false);
        private readonly ISubscriber _subscriber;

        public RedisSubscription(IServiceProvider serviceProvider, ISubscriber subscriber, string subscriberChannel)
        {
            _serviceProvider = serviceProvider;
            Channel = subscriberChannel;

            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
            _subscriber.Subscribe(Channel, (channel, value) =>
            {
                _mre.Set();
            });
        }

        public string Channel { get; }

        public void WaitForJob(TimeSpan timeout, CancellationToken cancellationToken)
        {
            _mre.Reset();
            WaitHandle.WaitAny(new[] { _mre, cancellationToken.WaitHandle }, timeout);
        }
    }
}
