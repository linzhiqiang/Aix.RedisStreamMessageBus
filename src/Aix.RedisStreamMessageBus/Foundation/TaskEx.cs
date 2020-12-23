using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aix.RedisStreamMessageBus.Foundation
{
    internal static class TaskEx
    {
        /// <summary>
        /// 无异常延迟
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task DelayNoException(TimeSpan delay, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
            }

        }

        /// <summary>
        /// 无异常延迟
        /// </summary>
        /// <param name="millisecondsDelay"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task DelayNoException(int millisecondsDelay, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DelayNoException(TimeSpan.FromMilliseconds(millisecondsDelay), cancellationToken);
        }
    }
}
