using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RedisStreamMessageBus.Sample.Model
{
    [Flags]
    public enum ClientMode
    {
        Producer = 1,
        Consumer = 2,
        // Both = 3
    }

    /// <summary>
    /// 组件 commandlineparser
    /// </summary>
    public class CmdOptions
    {
        [Option('m', "mode", Required = false, Default = 2, HelpText = "1=生产者测试，2=消费者测试,3=同时测试")]
        public int Mode { get; set; }

        [Option('q', "quantity", Required = false, Default = 1, HelpText = "测试生产数量")]
        public int Count { get; set; }

        public static CmdOptions Options;
    }
}
