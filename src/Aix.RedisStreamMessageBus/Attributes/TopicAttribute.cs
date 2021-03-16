using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RedisStreamMessageBus
{
    /// <summary>
    /// 配置topic名称 默认为typeof().Name
    /// </summary>
    public class TopicAttribute : Attribute
    {
        /// <summary>
        /// Topic
        /// </summary>
        public string Name { get; set; }

        public TopicAttribute()
        {
        }

        public static TopicAttribute GetTopicAttribute(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(TopicAttribute), true);
            return attrs != null && attrs.Length > 0 ? attrs[0] as TopicAttribute : null;
        }
    }

   
}
