using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RedisStreamMessageBus.Model
{
    public class JobData
    {
        public static JobData CreateJobData(string topic, byte[] data,string routeKey)
        {
            return new JobData
            {
                Data = data,
                Topic = topic,
                RouteKey = routeKey
            };
        }

        public JobData()
        {
            JobId = Guid.NewGuid().ToString().Replace("-", "");
            ErrorGroup = "";
        }
        public string JobId { get; set; }

        /// <summary>
        /// 业务数据
        /// </summary>
        public byte[] Data { get; set; }

        public string Topic { get; set; }

        /// <summary>
        /// 路由key
        /// </summary>
        public string RouteKey { get; set; }

        public int ErrorCount { get; set; }

        /// <summary>
        /// 当前消费失败所在组 消费是如果是失败的企鹅该值有值，只允许组一样的消费组消费
        /// </summary>
        public string ErrorGroup { get; set; }

        public NameValueEntry[] ToNameValueEntries()
        {
            //var jobData = this;
            //var nameValues = new NameValueEntry[] {
            //     new NameValueEntry(nameof( JobData.JobId),jobData.JobId?? ""),
            //     new NameValueEntry(nameof( JobData.Topic),jobData.Topic ?? ""),
            //new NameValueEntry(nameof(JobData.RouteKey), jobData.RouteKey ?? ""),
            //     new NameValueEntry(nameof( JobData.Data),jobData.Data ?? new byte[0]),
            //     new NameValueEntry(nameof( JobData.ErrorCount),jobData.ErrorCount),
            //     new NameValueEntry(nameof( JobData.ErrorGroup),jobData.ErrorGroup ?? ""),
            //    };

            var list = new List<NameValueEntry>();
            foreach (var item in GetType().GetProperties())
            {
                var value = item.GetValue(this);
                if (value == null)
                {
                    if (item.PropertyType == typeof(string))
                    {
                        value = RedisValue.EmptyString;
                    }
                    else if (item.PropertyType == typeof(byte[]))
                    {
                        value = new byte[0];
                    }
                }
                list.Add(new NameValueEntry(item.Name, value != null ? RedisValue.Unbox(value) : RedisValue.Null));
            }

            return list.ToArray();
        }

        public HashEntry[] ToHashEntries()
        {
            //var jobData = this;
            //var nameValues = new HashEntry[] {
            //     new HashEntry(nameof( JobData.JobId),jobData.JobId),
            //     new HashEntry(nameof( JobData.Topic),jobData.Topic),
            //     new HashEntry(nameof(JobData.RouteKey), jobData.RouteKey),
            //     new HashEntry(nameof( JobData.Data),jobData.Data),
            //     new HashEntry(nameof( JobData.ErrorCount),jobData.ErrorCount),
            //    };

            //return nameValues;

            var list = new List<HashEntry>();
            foreach (var item in GetType().GetProperties())
            {
                var value = item.GetValue(this);
                list.Add(new HashEntry(item.Name, value != null ? RedisValue.Unbox(value) : RedisValue.EmptyString));
            }

            return list.ToArray();
        }

        public static JobData ToJobData(NameValueEntry[] nameValues)
        {
            var dict = ToDictionary(nameValues);
            return ToJobData(dict);
        }

        public static JobData ToJobData(HashEntry[] hashEntries)
        {
            var dict = hashEntries.ToDictionary();
            return ToJobData(dict);
        }

        public static JobData ToJobData(Dictionary<RedisValue, RedisValue> keyValues)
        {
            //var jobData = new JobData
            //{
            //    //JobId = dict.ContainsKey(nameof(JobData.JobId)) ? dict[nameof(JobData.JobId)] : RedisValue.EmptyString,
            //    //Topic = dict.ContainsKey(nameof(JobData.Topic)) ? dict[nameof(JobData.Topic)] : RedisValue.EmptyString,
            //    //Data = dict.ContainsKey(nameof(JobData.Data)) ? dict[nameof(JobData.Data)] : RedisValue.Null,
            //    JobId = keyValues.GetValue(nameof(JobData.JobId)),
            //    Topic = keyValues.GetValue(nameof(JobData.Topic)),
            //    RouteKey = keyValues.GetValue(nameof(JobData.RouteKey)),
            //    Data = keyValues.GetValue(nameof(JobData.Data)),
            //    ErrorCount = NumberUtils.ToInt(keyValues.GetValue(nameof(JobData.ErrorCount))),
            //    ErrorGroup = keyValues.GetValue(nameof(JobData.ErrorGroup)),
            //};

            //return jobData;

            var result = new JobData();
            foreach (var item in keyValues)
            {
                var property = typeof(JobData).GetProperty(item.Key);
                if (property != null)
                {
                    //if (property.PropertyType == typeof(string))
                    //{
                    //    property.SetValue(result, item.Value.HasValue ? item.Value.ToString() : "");
                    //}
                    //else if (property.PropertyType == typeof(byte[]))
                    //{
                    //   var v= Convert.ChangeType(item.Value, property.PropertyType);
                    //    property.SetValue(result, v);
                    //}
                    //else
                    //{
                    //    property.SetValue(result, item.Value);
                    //}

                    property.SetValue(result, Convert.ChangeType(item.Value, property.PropertyType));

                }
            }
            return result;
        }

        private static Dictionary<RedisValue, RedisValue> ToDictionary(NameValueEntry[] nameValueEntries)
        {
            var dict = new Dictionary<RedisValue, RedisValue>();
            if (nameValueEntries != null && nameValueEntries.Length > 0)
            {
                foreach (var item in nameValueEntries)
                {
                    dict.Add(item.Name, item.Value);
                }
            }
            return dict;

        }
    }
}
