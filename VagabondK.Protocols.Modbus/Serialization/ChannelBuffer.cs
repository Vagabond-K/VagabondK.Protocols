using System.Collections.Generic;
using System.Linq;
using VagabondK.Protocols.Channels;

namespace VagabondK.Protocols.Modbus.Serialization
{
    class ChannelBuffer : List<byte>
    {
        internal ChannelBuffer(Channel channel)
        {
            Channel = channel;
        }

        public Channel Channel { get; }

        public byte Read(int timeout)
        {
            var result = Channel.Read(timeout);
            Add(result);
            return result;
        }

        public byte Read() => Read(0);

        public byte[] Read(uint count, int timeout)
        {
            var result = Channel.Read(count, timeout).ToArray();
            AddRange(result);
            return result;
        }
    }

    class RequestBuffer : ChannelBuffer
    {
        internal RequestBuffer(ModbusSlaveService modbusSlave, Channel channel) : base(channel)
        {
            ModbusSlave = modbusSlave;
        }

        public ModbusSlaveService ModbusSlave { get; }
    }

    class ResponseBuffer : ChannelBuffer
    {
        internal ResponseBuffer(Channel channel) : base(channel)
        {
        }
    }

}
