using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aix.RedisStreamMessageBus.Utils
{
    internal class IPUtils
    {
        /// <summary>
        /// 获取本机ip
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIP()
        {
            return System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
       .Select(p => p.GetIPProperties())
       .SelectMany(p => p.UnicastAddresses)
       .Where(p => p.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(p.Address))
       .FirstOrDefault()?.Address.ToString();
        }

        public static long IPToInt(String ipAddress)
        {
            //将目标IP地址字符串strIPAddress转换为数字
            string[] arrayIP = ipAddress.Split('.');
            long sip1 = long.Parse(arrayIP[0]);
            long sip2 = long.Parse(arrayIP[1]);
            long sip3 = long.Parse(arrayIP[2]);
            long sip4 = long.Parse(arrayIP[3]);

            long r1 = sip1 << 24;//sip1 * 256 * 256 * 256;
            long r2 = sip2 << 16;//sip2 * 256 * 256;
            long r3 = sip3 << 8;//sip3 * 256;
            long r4 = sip4;

            long result = r1 + r2 + r3 + r4;
            return result;
        }

        public static string IntToIP(long ipAddress)
        {
            long ui1 = ipAddress & 0xFF000000;
            ui1 = ui1 >> 24;
            long ui2 = ipAddress & 0x00FF0000;
            ui2 = ui2 >> 16;
            long ui3 = ipAddress & 0x0000FF00;
            ui3 = ui3 >> 8;
            long ui4 = ipAddress & 0x000000FF;
            string IPstr = $"{ui1}.{ui2}.{ui3}.{ui4}";
            return IPstr;
        }

    }
}
