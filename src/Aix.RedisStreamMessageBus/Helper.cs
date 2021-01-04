using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RedisStreamMessageBus
{
   internal class Helper
    {
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

    }
}
