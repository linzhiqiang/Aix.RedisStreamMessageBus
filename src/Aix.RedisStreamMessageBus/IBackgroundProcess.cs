using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Aix.RedisStreamMessageBus
{
    public interface IBackgroundProcess : IDisposable
    {
        Task Start(BackgroundProcessContext context);

        Task Execute(BackgroundProcessContext context);
    }
}
