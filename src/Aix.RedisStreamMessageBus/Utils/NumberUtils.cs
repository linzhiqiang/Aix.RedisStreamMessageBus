using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RedisStreamMessageBus.Utils
{
    internal class NumberUtils
    {

        public static int ToInt(object obj, int defaultValue)
        {
            return (int)ToDecimal(obj, defaultValue);
            /*
            int result;
            if (obj != null && int.TryParse(obj.ToString(), out result))
            {
                return result;
            }
            else
            {
                return defaultValue;
            }*/
        }

        public static int ToInt(object obj)
        {
            return (int)ToDecimal(obj, 0);
        }

        public static int? ToIntNullable(object obj)
        {
            decimal result;
            if (obj != null && decimal.TryParse(obj.ToString(), out result))
            {
                return (int)result;
            }
            return null;
        }

        public static long? ToLongNullable(object obj)
        {
            long result;
            if (obj != null && long.TryParse(obj.ToString(), out result))
            {
                return result;
            }
            return null;
        }

        public static long ToLong(object obj, long defaultValue)
        {
            long result;
            if (obj != null && long.TryParse(obj.ToString(), out result))
            {
                return result;
            }
            else
            {
                return defaultValue;
            }
        }

        public static long ToLong(object obj)
        {
            return ToLong(obj, 0);
        }

        public static decimal ToDecimal(object obj, decimal defaultValue)
        {
            decimal result;
            if (obj != null && decimal.TryParse(obj.ToString(), out result))
            {
                return result;
            }
            else
            {
                return defaultValue;
            }
        }

        public static decimal ToDecimal(object obj)
        {
            return ToDecimal(obj, 0.0m);
        }

        public static double ToDouble(object obj, double defaultValue)
        {
            double result;
            if (obj != null && double.TryParse(obj.ToString(), out result))
            {
                return result;
            }
            else
            {
                return defaultValue;
            }
        }

        public static double ToDouble(object obj)
        {
            return ToDouble(obj, 0.0);
        }

        public static sbyte ToBoolSbyte(object obj)
        {
            int intValue = ToInt(obj);
            return intValue > 0 ? (sbyte)1 : (sbyte)0;
        }

        /// <summary>
        /// 获取小数位数
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static int GetDecimalPlaces(decimal val)
        {
            var str = val.ToString();
            var idx = str.IndexOf('.');
            if (idx < 0)
                return 0;
            return str.Substring(idx).Length - 1;
        }

        /// <summary>
        /// 保留几位小数
        /// </summary>
        /// <param name="value"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        public static decimal Round(decimal value, int decimals = 2)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }
}
