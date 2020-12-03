using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aix.RedisStreamMessageBus.Serializer
{
    
    public class MessagePackSerializerImpl : ISerializer
    {
        public static MessagePackSerializerImpl Serializer = new MessagePackSerializerImpl();

        private readonly IFormatterResolver _formatterResolver;
        private readonly bool _useCompression;

        private MessagePackSerializerOptions _compressionOptions;
        private MessagePackSerializerOptions _unCompressionOptions;

        public MessagePackSerializerImpl(IFormatterResolver resolver = null, bool useCompression = false)
        {
            _useCompression = useCompression;
            _formatterResolver = resolver ?? ContractlessStandardResolver.Instance;

            _compressionOptions = MessagePackSerializerOptions.Standard.WithResolver(_formatterResolver).WithCompression(MessagePackCompression.Lz4BlockArray);
            _unCompressionOptions = MessagePackSerializerOptions.Standard.WithResolver(_formatterResolver);
        }

        public T Deserialize<T>(byte[] bytes)
        {
            if (_useCompression)
            {
                return MessagePack.MessagePackSerializer.Deserialize<T>(bytes, _compressionOptions);
            }
            else
            {
                return MessagePack.MessagePackSerializer.Deserialize<T>(bytes, _unCompressionOptions);
            }
        }

        public byte[] Serialize<T>(T data)
        {
            if (_useCompression)
            {
                return MessagePack.MessagePackSerializer.Serialize(data, _compressionOptions);
            }
            else
            {
                return MessagePack.MessagePackSerializer.Serialize(data, _unCompressionOptions);
            }
        }
    }

    
}
