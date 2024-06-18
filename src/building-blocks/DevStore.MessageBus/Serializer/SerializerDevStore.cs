using Confluent.Kafka;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace DevStore.MessageBus.Serializer
{
    public class SerializerDevStore<T> : ISerializer<T>
    {
        public byte[] Serialize(T data, SerializationContext context)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(data);

            using var memoryStream = new MemoryStream();
            using var zipStream = new GZipStream(memoryStream, CompressionMode.Compress, true);

            zipStream.Write(bytes, 0, bytes.Length);
            zipStream.Close();

            var buffer = memoryStream.ToArray();

            return buffer;
        }
    }
}