using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Aix.RedisStreamMessageBus.Utils 
{
    internal static class With
    {
        public static void NoException(ILogger logger, Action action, string message)
        {
            if (action == null) return;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                //logger.LogError($"{message}, {ex.Message}, {ex.StackTrace}");
                logger.LogError(ex, message);
            }
        }

        public static async Task NoException(ILogger logger, Func<Task> action, string message)
        {
            if (action == null) return;
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message);
            }
        }

        /// <summary>
        /// 失败重试
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="action"></param>
        /// <param name="operationName"></param>
        /// <param name="reTryCount"></param>
        /// <returns></returns>
        public static async Task ReTry(ILogger logger, Func<Task> action, string operationName, int reTryCount = 3)
        {
            int index = 0; //失败重试次数
            while (index < reTryCount)
            {
                index++;
                try
                {
                    await action();
                    break;
                }
                catch (Exception ex)
                {
                    if (index < reTryCount) logger.LogError($"{operationName}出错后重试，第{index}次");
                    if (index == reTryCount)
                    {
                        logger.LogError(ex, $"{operationName}重试{reTryCount}次失败");
                        throw ex;
                    }
                }
            }//while
        }

        /// <summary>
        /// 失败重试
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="action"></param>
        /// <param name="operationName"></param>
        /// <param name="reTryCount"></param>
        /// <returns></returns>
        public static async Task<T> ReTry<T>(ILogger logger, Func<Task<T>> action, string operationName, int reTryCount = 3)
        {
            int index = 0; //失败重试次数
            var result = default(T);
            while (index < reTryCount)
            {
                index++;
                try
                {
                    result = await action();
                    break;
                }
                catch (Exception ex)
                {
                    if (index < reTryCount) logger.LogError($"{operationName}出错后重试，第{index}次");
                    if (index == reTryCount)
                    {
                        logger.LogError(ex, $"{operationName}重试{reTryCount}次失败");
                        throw ex;
                    }
                }
            }//while

            return result;
        }
    }
}
