using System;
using System.Collections.Generic;
using System.Linq;
using VagabondK.Protocols.Modbus.Logging;

namespace VagabondK.Protocols.Modbus.Serialization
{
    class ModbusCommErrorResponse : ModbusResponse
    {
        public ModbusCommErrorResponse(ModbusCommErrorCode errorCode, IEnumerable<byte> receivedMessage, ModbusRequest request) : base(request)
        {
            ErrorCode = errorCode;
            ReceivedBytes = receivedMessage?.ToArray() ?? new byte[0];
        }

        public ModbusCommErrorCode ErrorCode { get; }
        public IReadOnlyList<byte> ReceivedBytes { get; }

        public override IEnumerable<byte> Serialize()
        {
            return ReceivedBytes;
        }

        public override ModbusLogCategory LogCategory { get => ModbusLogCategory.CommError; }

        public override string ToString()
        {
            string errorName = ErrorCode.ToString();

            if (ReceivedBytes != null && ReceivedBytes.Count > 0)
                return $"{errorName}: {BitConverter.ToString(ReceivedBytes as byte[])}";
            else
                return errorName;
        }
    }
}
