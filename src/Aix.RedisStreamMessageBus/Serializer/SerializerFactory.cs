using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RedisStreamMessageBus.Serializer
{
   public class SerializerFactory
    {
        public static SerializerFactory Instance = new SerializerFactory();

        private SerializerFactory() { }

        private ISerializer SerializerImpl = new SystemTextJsonSerializerImpl();
        public ISerializer GetSerializer()
        {
            return SerializerImpl;
        }
    }
}
