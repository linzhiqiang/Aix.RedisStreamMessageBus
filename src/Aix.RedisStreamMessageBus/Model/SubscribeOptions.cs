using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RedisStreamMessageBus.Model
{
    /// <summary>
    /// 单个订阅的配置，针对当前订阅有效
    /// </summary>
    public class SubscribeOptions
    {
        /// <summary>
        /// 分组 默认取全局配置
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// 消费者线程数 默认取全局配置
        /// </summary>
        public int ConsumerThreadCount { get; set; }

        /// <summary>
        /// 默认 0-0
        /// </summary>
       public  string GroupPosition { get; set; }
    }
}
