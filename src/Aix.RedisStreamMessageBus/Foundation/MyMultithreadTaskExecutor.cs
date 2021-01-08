using Aix.MultithreadExecutor;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RedisStreamMessageBus.Foundation
{
    internal class MyMultithreadTaskExecutor : MultithreadTaskExecutor
    {
        public MyMultithreadTaskExecutor(Action<MultithreadExecutorOptions> setupOptions) : base(setupOptions)
        {
        }
    }
}
