using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RedisStreamMessageBus.Utils
{
    internal class DateUtils
    {
        public static DateTime MinValue = new DateTime(2000, 1, 1);

        /// <summary>
        /// yyyy-MM-dd HH:mm:ss
        /// </summary>
        public static string Format = "yyyy-MM-dd HH:mm:ss";
        public static DateTime ToDateTime(object obj)
        {
            DateTime result;
            if (obj != null && DateTime.TryParse(obj.ToString(), out result))
            {
                return result;
            }
            else
            {
                return DateTime.MinValue;
            }

        }

        public static DateTime ToDateTime(object obj, DateTime defaultValue)
        {
            DateTime result;
            if (obj != null && DateTime.TryParse(obj.ToString(), out result))
            {
                return result;
            }
            else
            {
                return defaultValue;
            }

        }

        public static DateTime? ToDateTimeNullable(object obj)
        {

            DateTime result;
            if (obj != null && DateTime.TryParse(obj.ToString(), out result))
            {
                return result;
            }
            else
            {
                return null;
            }

        }

        public static string ToString(DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static DateTime SecondToZero(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0);
        }

        /// <summary>
        /// 计算日期所在天的开始时间
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetDayStartDate(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day);
        }

        /// <summary>
        /// 计算日期所在天的结束时间
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetDayEndDate(DateTime date)
        {
            return GetDayStartDate(date).AddDays(1).AddMilliseconds(-1);
        }

        /// <summary>
        /// 计算日期当前月初时间
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetMonthStartDate(DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }

        /// <summary>
        /// 计算日期当前月末时间
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetMonthEndDate(DateTime date)
        {
            return GetMonthStartDate(date).AddMonths(1).AddMilliseconds(-1);
        }

        public static DateTime GetYearStartDate(DateTime date)
        {
            return new DateTime(date.Year, 1, 1);
        }
        public static DateTime GetYearEndDate(DateTime date)
        {
            return GetYearStartDate(date).AddYears(1).AddMilliseconds(-1);
        }
        public static DateTime GetYearStartDate(int year)
        {
            return new DateTime(year, 1, 1);
        }
        public static DateTime GetYearEndDate(int year)
        {
            return GetYearStartDate(year).AddYears(1).AddMilliseconds(-1);
        }
        /// <summary>
        /// 计算所在周的周一的日期
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetMondayDateTime(DateTime date)
        {
            int span = date.DayOfWeek.GetHashCode() == 0 ? 7 : date.DayOfWeek.GetHashCode();
            return date.Date.AddDays(1 - span);
        }
        /// <summary>
        /// 计算所在周的周一的日期
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetSundayDateTime(DateTime date)
        {
            return GetMondayDateTime(date).AddDays(7).AddMilliseconds(-1);
        }

        /// <summary>
        /// 返回指定日期所在周中的某一天
        /// </summary>
        /// <param name="date">指定日期</param>
        /// <param name="week">希望返回的周中的某一天</param>
        /// <param name="isDayEnd">返回的天为一天的开始还是末尾，如果为true返回这天的23点59分59秒，如果为false返回这天的0点0分0秒</param>
        /// <returns></returns>
        public static DateTime GetDayByDayOfWeek(DateTime date, DayOfWeek week, bool isDayEnd)
        {
            DateTime day = new DateTime(date.Year, date.Month, date.Day);

            if (isDayEnd)
                day = day.AddDays(1).AddMilliseconds(-1);

            if (day.DayOfWeek == week)
                return day;

            return day.AddDays(week - day.DayOfWeek);
        }

        public static int GetYearDayCount(DateTime date)
        {
            return DateTime.IsLeapYear(date.Year) ? 366 : 365;
        }

        public static long GetTimeStamp()
        {
            DateTime theDate = DateTime.Now;
            DateTime d1 = new DateTime(1970, 1, 1);
            DateTime d2 = theDate.ToUniversalTime();
            TimeSpan ts = new TimeSpan(d2.Ticks - d1.Ticks);
            return (long)ts.TotalMilliseconds;
        }

        public static long GetTimeStamp(DateTime date)
        {
            DateTime theDate = date;
            DateTime d1 = new DateTime(1970, 1, 1);
            DateTime d2 = theDate.ToUniversalTime();
            TimeSpan ts = new TimeSpan(d2.Ticks - d1.Ticks);
            return (long)ts.TotalMilliseconds;
        }

        public static long GetTimeStampForSecond()
        {
            DateTime theDate = DateTime.Now;
            DateTime d1 = new DateTime(1970, 1, 1);
            DateTime d2 = theDate.ToUniversalTime();
            TimeSpan ts = new TimeSpan(d2.Ticks - d1.Ticks);
            return (long)ts.TotalSeconds;
        }

        /// <summary>
        /// 转换utc时间戳
        /// </summary>
        /// <param name="timestamp">毫秒数</param>
        /// <returns></returns>
        public static DateTime TimeStampToDateTime(long timestamp)
        {
            //DateTime date = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1)); // 当地时区
            var date = TimeZoneInfo.ConvertTimeFromUtc(new System.DateTime(1970, 1, 1), TimeZoneInfo.Local);
            return date.AddMilliseconds(timestamp);
        }

        public static string ToLongString(DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string ToShortString(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }

        public static long GetCurrentTimeMillis()
        {
            return GetTimeStamp();
        }


        /// <summary>
        /// 获取当前周几   1,2,3,4,5,6,7
        /// </summary>
        /// <returns></returns>
        public static int GetCurrentWeek()
        {
            int currentWeek = DateTime.Now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)DateTime.Now.DayOfWeek;

            return currentWeek;
        }

    }
}
