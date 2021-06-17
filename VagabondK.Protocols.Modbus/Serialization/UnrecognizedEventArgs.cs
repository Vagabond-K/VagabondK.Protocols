using System;
using System.Collections.Generic;
using VagabondK.Protocols.Channels;

namespace VagabondK.Protocols.Modbus.Serialization
{
    class UnrecognizedEventArgs : EventArgs
    {
        public UnrecognizedEventArgs(IChannel channel, IReadOnlyList<byte> unrecognizedMessage)
        {
            Channel = channel;
            UnrecognizedMessage = unrecognizedMessage;
        }

        public IChannel Channel { get; }
        public IReadOnlyList<byte> UnrecognizedMessage { get; }
    }
}
