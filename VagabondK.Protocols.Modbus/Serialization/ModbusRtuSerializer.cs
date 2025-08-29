using System;
using System.Collections.Generic;
using System.Linq;
using VagabondK.Protocols.Logging;

namespace VagabondK.Protocols.Modbus.Serialization
{
    /// <summary>
    /// Modbus RTU Serializer
    /// </summary>
    public sealed class ModbusRtuSerializer : ModbusSerializer
    {
        private readonly List<byte> errorBuffer = new List<byte>();

        private static readonly ushort[] crcTable = {
            0x0000, 0xc0c1, 0xc181, 0x0140, 0xc301, 0x03c0, 0x0280, 0xc241,
            0xc601, 0x06c0, 0x0780, 0xc741, 0x0500, 0xc5c1, 0xc481, 0x0440,
            0xcc01, 0x0cc0, 0x0d80, 0xcd41, 0x0f00, 0xcfc1, 0xce81, 0x0e40,
            0x0a00, 0xcac1, 0xcb81, 0x0b40, 0xc901, 0x09c0, 0x0880, 0xc841,
            0xd801, 0x18c0, 0x1980, 0xd941, 0x1b00, 0xdbc1, 0xda81, 0x1a40,
            0x1e00, 0xdec1, 0xdf81, 0x1f40, 0xdd01, 0x1dc0, 0x1c80, 0xdc41,
            0x1400, 0xd4c1, 0xd581, 0x1540, 0xd701, 0x17c0, 0x1680, 0xd641,
            0xd201, 0x12c0, 0x1380, 0xd341, 0x1100, 0xd1c1, 0xd081, 0x1040,
            0xf001, 0x30c0, 0x3180, 0xf141, 0x3300, 0xf3c1, 0xf281, 0x3240,
            0x3600, 0xf6c1, 0xf781, 0x3740, 0xf501, 0x35c0, 0x3480, 0xf441,
            0x3c00, 0xfcc1, 0xfd81, 0x3d40, 0xff01, 0x3fc0, 0x3e80, 0xfe41,
            0xfa01, 0x3ac0, 0x3b80, 0xfb41, 0x3900, 0xf9c1, 0xf881, 0x3840,
            0x2800, 0xe8c1, 0xe981, 0x2940, 0xeb01, 0x2bc0, 0x2a80, 0xea41,
            0xee01, 0x2ec0, 0x2f80, 0xef41, 0x2d00, 0xedc1, 0xec81, 0x2c40,
            0xe401, 0x24c0, 0x2580, 0xe541, 0x2700, 0xe7c1, 0xe681, 0x2640,
            0x2200, 0xe2c1, 0xe381, 0x2340, 0xe101, 0x21c0, 0x2080, 0xe041,
            0xa001, 0x60c0, 0x6180, 0xa141, 0x6300, 0xa3c1, 0xa281, 0x6240,
            0x6600, 0xa6c1, 0xa781, 0x6740, 0xa501, 0x65c0, 0x6480, 0xa441,
            0x6c00, 0xacc1, 0xad81, 0x6d40, 0xaf01, 0x6fc0, 0x6e80, 0xae41,
            0xaa01, 0x6ac0, 0x6b80, 0xab41, 0x6900, 0xa9c1, 0xa881, 0x6840,
            0x7800, 0xb8c1, 0xb981, 0x7940, 0xbb01, 0x7bc0, 0x7a80, 0xba41,
            0xbe01, 0x7ec0, 0x7f80, 0xbf41, 0x7d00, 0xbdc1, 0xbc81, 0x7c40,
            0xb401, 0x74c0, 0x7580, 0xb541, 0x7700, 0xb7c1, 0xb681, 0x7640,
            0x7200, 0xb2c1, 0xb381, 0x7340, 0xb101, 0x71c0, 0x7080, 0xb041,
            0x5000, 0x90c1, 0x9181, 0x5140, 0x9301, 0x53c0, 0x5280, 0x9241,
            0x9601, 0x56c0, 0x5780, 0x9741, 0x5500, 0x95c1, 0x9481, 0x5440,
            0x9c01, 0x5cc0, 0x5d80, 0x9d41, 0x5f00, 0x9fc1, 0x9e81, 0x5e40,
            0x5a00, 0x9ac1, 0x9b81, 0x5b40, 0x9901, 0x59c0, 0x5880, 0x9841,
            0x8801, 0x48c0, 0x4980, 0x8941, 0x4b00, 0x8bc1, 0x8a81, 0x4a40,
            0x4e00, 0x8ec1, 0x8f81, 0x4f40, 0x8d01, 0x4dc0, 0x4c80, 0x8c41,
            0x4400, 0x84c1, 0x8581, 0x4540, 0x8701, 0x47c0, 0x4680, 0x8641,
            0x8201, 0x42c0, 0x4380, 0x8341, 0x4100, 0x81c1, 0x8081, 0x4040
        };

        private bool swapCRC;

        /// <summary>
        /// CRC 바이트 순서를 바꿉니다. 이 옵션은 기본적으로는 사용하면 안 됩니다. CRC는 Modbus 프로토콜에서 기본적으로 리틀 엔디안(하위 바이트 우선) 으로 전송됩니다. 특정 장비나 예외적인 상황에서 빅 엔디안(상위 바이트 우선) 전송이 필요한 경우에만 사용하세요.
        /// </summary>
        [Obsolete("In Modbus protocol, CRC is transmitted in little-endian format (low byte first) by default. Use this option only if your device requires big-endian CRC (high byte first) due to specific implementation needs.")]
        public bool SwapCRC { get => swapCRC; set => swapCRC = value; }

        private IEnumerable<byte> GetCrcBytes(ushort crc)
        {
            if (swapCRC)
            {
                yield return (byte)(crc >> 8);
                yield return (byte)(crc & 0xff);
            }
            else
            {
                yield return (byte)(crc & 0xff);
                yield return (byte)(crc >> 8);
            }
        }

        internal override IEnumerable<byte> OnSerialize(IModbusMessage message)
        {
            ushort crc = ushort.MaxValue;

            foreach (var b in message.Serialize())
            {
                byte tableIndex = (byte)(crc ^ b);
                crc >>= 8;
                crc ^= crcTable[tableIndex];
                yield return b;
            }

            foreach (var b in GetCrcBytes(crc))
                yield return b;
        }

        private bool IsException(ResponseBuffer buffer, ModbusRequest request, int timeout, out ModbusResponse responseMessage)
        {
            if ((Read(buffer, 1, timeout) & 0x80) == 0x80)
            {
                var codeValue = Read(buffer, 2, timeout);

                if (IsErrorCRC(buffer, 3, timeout))
                    throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ErrorCRC, buffer, request);

                ModbusExceptionCode exceptionCode = ModbusExceptionCode.NotDefined;
                if (Enum.IsDefined(typeof(ModbusExceptionCode), codeValue))
                    exceptionCode = (ModbusExceptionCode)codeValue;

                responseMessage = new ModbusExceptionResponse(exceptionCode, request);
                return true;
            }
            else
            {
                responseMessage = null;
                return false;
            }
        }

        private bool IsErrorCRC(ResponseBuffer buffer, int messageLength, int timeout)
        {
            var crc = Read(buffer, messageLength, 2, timeout).ToArray();

            return !CalculateCrc(buffer.Take(messageLength)).SequenceEqual(crc);
        }


        internal override ModbusResponse DeserializeResponse(ResponseBuffer buffer, ModbusRequest request, int timeout)
        {
            lock (buffer.Channel)
            {
                var remainMessage = buffer.Channel.ReadAllRemain().ToArray();
                if (remainMessage != null && remainMessage.Length > 0)
                    RaiseUnrecognized(buffer.Channel, remainMessage);

                var requestMessage = Serialize(request).ToArray();
                buffer.Channel.Write(requestMessage);
                buffer.RequestLog = new ModbusRequestLog(buffer.Channel, request, requestMessage, this);
                buffer.Channel?.Logger?.Log(buffer.RequestLog);

                ModbusResponse result = base.DeserializeResponse(buffer, request, timeout);

                while (result is ModbusCommErrorResponse responseCommErrorMessage
                    && responseCommErrorMessage.ErrorCode != ModbusCommErrorCode.ResponseTimeout)
                {
                    errorBuffer.Add(buffer[0]);
                    buffer.RemoveAt(0);
                    result = base.DeserializeResponse(buffer, request, timeout);
                }

                if (result is ModbusCommErrorResponse responseCommError)
                {
                    result = new ModbusCommErrorResponse(responseCommError.ErrorCode, errorBuffer.Concat(responseCommError.ReceivedBytes), request);
                }
                else if (errorBuffer.Count > 0)
                {
                    RaiseUnrecognized(buffer.Channel, errorBuffer.ToArray());
                    errorBuffer.Clear();
                }

                return result;
            }
        }

        internal override ModbusResponse DeserializeReadBitResponse(ResponseBuffer buffer, ModbusReadRequest request, int timeout)
        {
            if (IsException(buffer, request, timeout, out var responseMessage))
                return responseMessage;

            byte byteLength = Read(buffer, 2, timeout);

            if (IsErrorCRC(buffer, 3 + byteLength, timeout))
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ErrorCRC, buffer, request);

            if (Read(buffer, 0, timeout) != request.SlaveAddress)
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ResponseSlaveAddressDoNotMatch, buffer, request);
            if ((Read(buffer, 1, timeout) & 0x7f) != (byte)request.Function)
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ResponseFunctionDoNotMatch, buffer, request);
            if (byteLength != (byte)Math.Ceiling(request.Length / 8d))
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ResponseLengthDoNotMatch, buffer, request);

            return new ModbusReadBitResponse(Read(buffer, 3, byteLength, timeout).SelectMany(b => ByteToBitArray(b)).Take(request.Length).ToArray(), request);
        }

        internal override ModbusResponse DeserializeReadWordResponse(ResponseBuffer buffer, ModbusReadRequest request, int timeout)
        {
            if (IsException(buffer, request, timeout, out var responseMessage))
                return responseMessage;

            byte byteLength = Read(buffer, 2, timeout);

            if (IsErrorCRC(buffer, 3 + byteLength, timeout))
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ErrorCRC, buffer, request);

            if (Read(buffer, 0, timeout) != request.SlaveAddress)
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ResponseSlaveAddressDoNotMatch, buffer, request);
            if ((Read(buffer, 1, timeout) & 0x7f) != (byte)request.Function)
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ResponseFunctionDoNotMatch, buffer, request);
            if (byteLength != (byte)(request.Length * 2))
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ResponseLengthDoNotMatch, buffer, request);

            return new ModbusReadWordResponse(Read(buffer, 3, byteLength, timeout).ToArray(), request);
        }

        internal override ModbusResponse DeserializeWriteResponse(ResponseBuffer buffer, ModbusWriteCoilRequest request, int timeout)
        {
            if (IsException(buffer, request, timeout, out var responseMessage))
                return responseMessage;

            if (IsErrorCRC(buffer, 6, timeout))
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ErrorCRC, buffer, request);

            if (Read(buffer, 0, timeout) != request.SlaveAddress)
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ResponseSlaveAddressDoNotMatch, buffer, request);
            if ((Read(buffer, 1, timeout) & 0x7f) != (byte)request.Function)
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ResponseFunctionDoNotMatch, buffer, request);
            if (ToUInt16(buffer, 2) != request.Address)
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ResponseAddressDoNotMatch, buffer, request);

            switch (request.Function)
            {
                case ModbusFunction.WriteSingleCoil:
                    if (Read(buffer, 4, timeout) != (request.SingleBitValue ? 0xff : 0x00)
                        || Read(buffer, 5, timeout) != 0x00)
                        throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ResponseWritedValueDoNotMatch, buffer, request);
                    break;
                case ModbusFunction.WriteMultipleCoils:
                    if (ToUInt16(buffer, 4) != request.Length)
                        throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ResponseWritedLengthDoNotMatch, buffer, request);
                    break;
            }

            return new ModbusWriteResponse(request);
        }

        internal override ModbusResponse DeserializeWriteResponse(ResponseBuffer buffer, ModbusWriteHoldingRegisterRequest request, int timeout)
        {
            if (IsException(buffer, request, timeout, out var responseMessage))
                return responseMessage;

            if (IsErrorCRC(buffer, 6, timeout))
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ErrorCRC, buffer, request);

            if (Read(buffer, 0, timeout) != request.SlaveAddress)
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ResponseSlaveAddressDoNotMatch, buffer, request);
            if ((Read(buffer, 1, timeout) & 0x7f) != (byte)request.Function)
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ResponseFunctionDoNotMatch, buffer, request);
            if (ToUInt16(buffer, 2) != request.Address)
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ResponseAddressDoNotMatch, buffer, request);

            ushort value = ToUInt16(buffer, 4);

            switch (request.Function)
            {
                case ModbusFunction.WriteSingleHoldingRegister:
                    if (value != request.SingleWordValue)
                        throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ResponseWritedValueDoNotMatch, buffer, request);
                    break;
                case ModbusFunction.WriteMultipleHoldingRegisters:
                    if (value != request.Length)
                        throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.ResponseWritedLengthDoNotMatch, buffer, request);
                    break;
            }

            return new ModbusWriteResponse(request);
        }


        internal override ModbusRequest DeserializeRequest(RequestBuffer buffer, int timeout)
        {
            ModbusRequest result = null;
            while (!buffer.Channel.IsDisposed)
            {
                if (errorBuffer.Count >= 256)
                {
                    RaiseUnrecognized(buffer.Channel, errorBuffer.ToArray());
                    errorBuffer.Clear();
                }

                while (buffer.Count < 8 && !buffer.Channel.IsDisposed)
                    buffer.Read(1, timeout);

                if (buffer.Channel.IsDisposed) break;

                var slaveAddress = buffer[0];
                int messageLength = 0;

                if (buffer.ModbusSlave.IsValidSlaveAddress(slaveAddress, buffer.Channel)
                    && Enum.IsDefined(typeof(ModbusFunction), buffer[1]))
                {
                    ModbusFunction function = (ModbusFunction)buffer[1];
                    var address = ToUInt16(buffer, 2);
                    var valueOrLength = ToUInt16(buffer, 4);

                    switch (function)
                    {
                        case ModbusFunction.ReadCoils:
                        case ModbusFunction.ReadDiscreteInputs:
                        case ModbusFunction.ReadHoldingRegisters:
                        case ModbusFunction.ReadInputRegisters:
                        case ModbusFunction.WriteSingleCoil:
                        case ModbusFunction.WriteSingleHoldingRegister:
                            if (CalculateCrc(buffer.Take(6)).SequenceEqual(buffer.Skip(6).Take(2)))
                            {
                                messageLength = 8;
                                switch (function)
                                {
                                    case ModbusFunction.ReadCoils:
                                    case ModbusFunction.ReadDiscreteInputs:
                                    case ModbusFunction.ReadHoldingRegisters:
                                    case ModbusFunction.ReadInputRegisters:
                                        result = new ModbusReadRequest(slaveAddress, (ModbusObjectType)(byte)function, address, valueOrLength);
                                        break;
                                    case ModbusFunction.WriteSingleCoil:
                                        if (valueOrLength != 0xff00 && valueOrLength != 0)
                                            result = new ModbusWriteCoilRequest(slaveAddress, address);
                                        else
                                            result = new ModbusWriteCoilRequest(slaveAddress, address, valueOrLength == 0xff00);
                                        break;
                                    case ModbusFunction.WriteSingleHoldingRegister:
                                        result = new ModbusWriteHoldingRegisterRequest(slaveAddress, address, valueOrLength);
                                        break;
                                }
                            }
                            break;
                        case ModbusFunction.WriteMultipleCoils:
                        case ModbusFunction.WriteMultipleHoldingRegisters:
                            if (buffer.Count < 7 && !buffer.Channel.IsDisposed)
                                buffer.Read(1, timeout);

                            if (buffer.Channel.IsDisposed) break;

                            var byteLength = buffer[6];
                            messageLength = byteLength + 9;

                            if (function == ModbusFunction.WriteMultipleCoils && byteLength == Math.Ceiling(valueOrLength / 8d)
                                || function == ModbusFunction.WriteMultipleHoldingRegisters && byteLength == valueOrLength * 2)
                            {
                                while (buffer.Count < messageLength && !buffer.Channel.IsDisposed)
                                    buffer.Read(1, timeout);

                                if (buffer.Channel.IsDisposed) break;

                                if (CalculateCrc(buffer.Take(byteLength + 7)).SequenceEqual(buffer.Skip(byteLength + 7).Take(2)))
                                {
                                    switch (function)
                                    {
                                        case ModbusFunction.WriteMultipleCoils:
                                            result = new ModbusWriteCoilRequest(slaveAddress, address, buffer.Skip(7).Take(byteLength).SelectMany(b => ByteToBitArray(b)).Take(valueOrLength).ToArray());
                                            break;
                                        case ModbusFunction.WriteMultipleHoldingRegisters:
                                            result = new ModbusWriteHoldingRegisterRequest(slaveAddress, address, buffer.Skip(7).Take(byteLength).ToArray());
                                            break;
                                    }
                                }
                            }
                            break;
                    }
                }

                if (result != null)
                {
                    if (errorBuffer.Count > 0)
                    {
                        RaiseUnrecognized(buffer.Channel, errorBuffer.ToArray());
                        errorBuffer.Clear();
                    }
                    return result;
                }
                else
                {
                    errorBuffer.Add(buffer[0]);
                    buffer.RemoveAt(0);
                    continue;
                }
            }
            return null;
        }


        private IEnumerable<byte> CalculateCrc(IEnumerable<byte> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            ushort crc = ushort.MaxValue;

            foreach (byte b in data)
            {
                byte tableIndex = (byte)(crc ^ b);
                crc >>= 8;
                crc ^= crcTable[tableIndex];
            }

            return GetCrcBytes(crc);
        }
    }
}
