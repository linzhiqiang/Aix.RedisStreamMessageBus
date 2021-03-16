using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;

namespace Aix.RedisStreamMessageBus.Serializer
{
    public class SystemTextJsonSerializerImpl : ISerializer
    {
        static JsonSerializerOptions Options = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
            //WriteIndented=true //格式化的
        };
        public T Deserialize<T>(byte[] bytes)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(bytes, Options);
        }

        public byte[] Serialize<T>(T data)
        {
            if (data == null) return null;
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data, Options);
        }
    }
}
