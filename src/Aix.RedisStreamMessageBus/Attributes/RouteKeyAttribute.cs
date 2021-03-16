using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RedisStreamMessageBus
{
    /// <summary>
    /// 路由键，相同的进入同一个线程处理
    /// </summary>
    public class RouteKeyAttribute : Attribute
    {

    }
}
