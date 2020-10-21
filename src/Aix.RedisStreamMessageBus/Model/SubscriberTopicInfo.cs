using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RedisStreamMessageBus.Model
{
   public class SubscriberTopicInfo
    {
        public string Topic { get; set; }

        public string GroupName { get; set; }
    }
}
