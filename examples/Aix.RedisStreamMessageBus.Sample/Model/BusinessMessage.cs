using Aix.RedisStreamMessageBus.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RedisStreamMessageBus.Sample.Model
{
    [TopicAttribute(Name = "BusinessMessageDemo1")]
    public class BusinessMessage
    {
        // [RouteKeyAttribute]
        public string MessageId { get; set; }
        public string Content { get; set; }

        public DateTime CreateTime { get; set; }
    }

    [TopicAttribute(Name = "BusinessMessageDemo2")]
    public class BusinessMessage2
    {
        public string MessageId { get; set; }
        public string Content { get; set; }

        public DateTime CreateTime { get; set; }
    }
}
