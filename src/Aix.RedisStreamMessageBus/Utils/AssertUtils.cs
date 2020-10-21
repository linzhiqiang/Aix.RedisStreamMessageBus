using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RedisStreamMessageBus.Utils
{
    internal static class AssertUtils
    {
        public static void IsTrue(bool condition, string errorText)
        {
            if (!condition)
            {
                throw new Exception(errorText ?? "异常");
            }
        }

        public static void IsNotNull(object obj, string errorText)
        {
            IsTrue(obj != null, errorText);
        }

        public static void IsNotEmpty(string obj, string errorText)
        {
            IsTrue(!string.IsNullOrEmpty(obj), errorText);
        }

        public static void ThrowException(string errorText)
        {
            IsTrue(false, errorText);
        }

    }
}
