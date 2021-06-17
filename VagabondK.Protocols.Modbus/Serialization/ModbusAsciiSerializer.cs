using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VagabondK.Protocols.Modbus.Serialization
{
    /// <summary>
    /// Modbus ASCII Serializer
    /// </summary>
    public sealed class ModbusAsciiSerializer : ModbusSerializer
    {
        private readonly List<byte> errorBuffer = new List<byte>();

        internal override IEnumerable<byte> OnSerialize(IModbusMessage message)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(":");

            byte lrc = 0;

            foreach (var b in message.Serialize())
            {
                stringBuilder.AppendFormat("{0:X2}", b);
                lrc += b;
            }

            lrc = (byte)(-lrc & 0xff);
            stringBuilder.AppendFormat("{0:X2}", lrc);
            stringBuilder.Append("\r\n");

            return Encoding.ASCII.GetBytes(stringBuilder.ToString());
        }

        internal override byte Read(ResponseBuffer buffer, int index, int timeout)
        {
            if (index * 2 >= buffer.Count - 2)
                buffer.Read((uint)(index * 2 - buffer.Count + 3), timeout);

            return byte.Parse(Encoding.ASCII.GetString(buffer.Skip(index * 2 + 1).Take(2).ToArray()), System.Globalization.NumberStyles.HexNumber, null);
        }

        internal override IEnumerable<byte> Read(ResponseBuffer buffer, int index, int count, int timeout)
        {
            if ((index + count) * 2 > buffer.Count - 1)
                buffer.Read((uint)((index + count) * 2 - buffer.Count + 1), timeout);

            var bytes = buffer.Skip(index * 2 + 1).Take(count * 2).ToArray();
            for (int i = 0; i < count; i++)
                yield return byte.Parse(Encoding.ASCII.GetString(bytes, i * 2, 2), System.Globalization.NumberStyles.HexNumber, null);
        }

        internal ushort ToUInt16(ResponseBuffer buffer, int index, int timeout)
        {
            return ToUInt16(Read(buffer, index, 2, timeout).ToArray(), 0);
        }

        internal override ModbusResponse DeserializeResponse(ResponseBuffer buffer, ModbusRequest request, int timeout)
        {
            ModbusResponse result = null;

            while (result == null ||
                result is ModbusCommErrorResponse responseCommErrorMessage
                && responseCommErrorMessage.ErrorCode != ModbusCommErrorCode.ResponseTimeout)
            {
                if (buffer.Count > 0)
                {
                    errorBuffer.Add(buffer[0]);
                    buffer.RemoveAt(0);
                }

                buffer.Read(timeout);

                try
                {
                    while (buffer[0] != 0x3a)
                    {
                        errorBuffer.Add(buffer[0]);
                        buffer.RemoveAt(0);
                        buffer.Read(timeout);
                    }
                }
                catch
                {
                    return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseAsciiStartError, errorBuffer.Concat(buffer), request);
                }

                result = base.DeserializeResponse(buffer, request, timeout);
            }

            if (result is ModbusCommErrorResponse responseCommError)
            {
                result = new ModbusCommErrorResponse(responseCommError.ErrorCode, errorBuffer.Concat(responseCommError.ReceivedBytes), request);
            }
            else
            {
                var asciiEnd = buffer.Read(2, timeout);
                if (!asciiEnd.SequenceEqual(new byte[] { 13, 10 }))
                    return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseAsciiEndError, buffer, request);

                if (errorBuffer.Count > 0)
                {
                    RaiseUnrecognized(buffer.Channel, errorBuffer.ToArray());
                    errorBuffer.Clear();
                }
            }

            return result;
        }

        private bool IsException(ResponseBuffer buffer, ModbusRequest request, int timeout, out ModbusResponse responseMessage)
        {
            if ((Read(buffer, 1, timeout) & 0x80) == 0x80)
            {
                var codeValue = Read(buffer, 2, timeout);

                if (IsErrorLRC(buffer, 3, request, timeout, out responseMessage))
                    return true;

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

        private bool IsErrorLRC(ResponseBuffer buffer, int messageLength, ModbusRequest request, int timeout, out ModbusResponse responseMessage)
        {
            
            byte lrc = 0;

            foreach (var b in Read(buffer, 0, messageLength, timeout))
            {
                lrc += b;
            }

            lrc = (byte)(-lrc & 0xff);

            if (lrc != Read(buffer, messageLength, timeout))
            {
                responseMessage = new ModbusCommErrorResponse(ModbusCommErrorCode.ErrorLRC, buffer, request);
                return true;
            }
            else
            {
                responseMessage = null;
                return false;
            }
        }

        internal override ModbusResponse DeserializeReadBooleanResponse(ResponseBuffer buffer, ModbusReadRequest request, int timeout)
        {
            if (IsException(buffer, request, timeout, out var responseMessage))
                return responseMessage;

            byte byteLength = Read(buffer, 2, timeout);

            if (IsErrorLRC(buffer, 3 + byteLength, request, timeout, out responseMessage))
                return responseMessage;

            if (Read(buffer, 0, timeout) != request.SlaveAddress)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseSlaveAddressDoNotMatch, buffer, request);
            if ((Read(buffer, 1, timeout) & 0x7f) != (byte)request.Function)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseFunctionDoNotMatch, buffer, request);
            if (byteLength != (byte)Math.Ceiling(request.Length / 8d))
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseLengthDoNotMatch, buffer, request);

            return new ModbusReadBooleanResponse(Read(buffer, 3, byteLength, timeout).SelectMany(b => ByteToBooleanArray(b)).Take(request.Length).ToArray(), request);
        }

        internal override ModbusResponse DeserializeReadRegisterResponse(ResponseBuffer buffer, ModbusReadRequest request, int timeout)
        {
            if (IsException(buffer, request, timeout, out var responseMessage))
                return responseMessage;

            byte byteLength = Read(buffer, 2, timeout);

            if (IsErrorLRC(buffer, 3 + byteLength, request, timeout, out responseMessage))
                return responseMessage;

            if (Read(buffer, 0, timeout) != request.SlaveAddress)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseSlaveAddressDoNotMatch, buffer, request);
            if ((Read(buffer, 1, timeout) & 0x7f) != (byte)request.Function)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseFunctionDoNotMatch, buffer, request);
            if (byteLength != (byte)(request.Length * 2))
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseLengthDoNotMatch, buffer, request);

            return new ModbusReadRegisterResponse(Read(buffer, 3, byteLength, timeout).ToArray(), request);
        }

        internal override ModbusResponse DeserializeWriteResponse(ResponseBuffer buffer, ModbusWriteCoilRequest request, int timeout)
        {
            if (IsException(buffer, request, timeout, out var responseMessage))
                return responseMessage;

            if (IsErrorLRC(buffer, 6, request, timeout, out responseMessage))
                return responseMessage;

            if (Read(buffer, 0, timeout) != request.SlaveAddress)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseSlaveAddressDoNotMatch, buffer, request);
            if ((Read(buffer, 1, timeout) & 0x7f) != (byte)request.Function)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseFunctionDoNotMatch, buffer, request);
            if (ToUInt16(buffer, 2, timeout) != request.Address)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseAddressDoNotMatch, buffer, request);

            switch (request.Function)
            {
                case ModbusFunction.WriteSingleCoil:
                    if (Read(buffer, 4, timeout) != (request.SingleBooleanValue ? 0xff : 0x00)
                        || Read(buffer, 5, timeout) != 0x00)
                        return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseWritedValueDoNotMatch, buffer, request);
                    break;
                case ModbusFunction.WriteMultipleCoils:
                    if (ToUInt16(buffer, 4, timeout) != request.Length)
                        return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseWritedLengthDoNotMatch, buffer, request);
                    break;
            }

            return new ModbusWriteResponse(request);
        }

        internal override ModbusResponse DeserializeWriteResponse(ResponseBuffer buffer, ModbusWriteHoldingRegisterRequest request, int timeout)
        {
            if (IsException(buffer, request, timeout, out var responseMessage))
                return responseMessage;

            if (IsErrorLRC(buffer, 6, request, timeout, out responseMessage))
                return responseMessage;

            if (Read(buffer, 0, timeout) != request.SlaveAddress)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseSlaveAddressDoNotMatch, buffer, request);
            if ((Read(buffer, 1, timeout) & 0x7f) != (byte)request.Function)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseFunctionDoNotMatch, buffer, request);
            if (ToUInt16(buffer, 2, timeout) != request.Address)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseAddressDoNotMatch, buffer, request);

            ushort value = ToUInt16(buffer, 4, timeout);

            switch (request.Function)
            {
                case ModbusFunction.WriteSingleHoldingRegister:
                    if (value != request.SingleRegisterValue)
                        return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseWritedValueDoNotMatch, buffer, request);
                    break;
                case ModbusFunction.WriteMultipleHoldingRegisters:
                    if (value != request.Length)
                        return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseWritedLengthDoNotMatch, buffer, request);
                    break;
            }

            return new ModbusWriteResponse(request);
        }

        private static bool TryParseFromHex(IReadOnlyList<byte> buffer, int index, out byte result)
        {
            if (!ValidByteValue(buffer[index]) || !ValidByteValue(buffer[index + 1]))
            {
                result = 0;
                return false;
            }
            
            return byte.TryParse(new string(new char[] { (char)buffer[index], (char)buffer[index + 1] }), System.Globalization.NumberStyles.HexNumber, null, out result);
        }

        private static bool ValidByteValue(byte value)
        {
            return value == 0x0d || value == 0x0a || value >= 0x30 && value <= 0x3a || value >= 0x41 && value <= 0x46 || value >= 0x61 && value <= 0x66;
        }

        internal override ModbusRequest DeserializeRequest(RequestBuffer rawBuffer)
        {
            ModbusRequest result = null;

            while (!rawBuffer.Channel.IsDisposed)
            {
                if (errorBuffer.Count >= 256)
                {
                    RaiseUnrecognized(rawBuffer.Channel, errorBuffer.ToArray());
                    errorBuffer.Clear();
                }

                while (rawBuffer.Count < 17 && !rawBuffer.Channel.IsDisposed)
                    rawBuffer.Read();

                if (rawBuffer.Channel.IsDisposed) break;

                int messageLength = 0;

                if (rawBuffer[0] == 0x3a)
                {
                    List<byte> buffer = new List<byte>();

                    for (int i = 0; i < 7; i++)
                    {
                        if (TryParseFromHex(rawBuffer, i * 2 + 1, out var byteValue))
                            buffer.Add(byteValue);
                        else
                            break;
                    }

                    if (buffer.Count < 7)
                    {
                        errorBuffer.AddRange(rawBuffer.Take((buffer.Count + 1) * 2 + 1));
                        rawBuffer.RemoveRange(0, (buffer.Count + 1) * 2 + 1);
                        continue;
                    }

                    var slaveAddress = buffer[0];

                    if (rawBuffer.ModbusSlave.IsValidSlaveAddress(slaveAddress, rawBuffer.Channel)
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
                                if ((-buffer.Take(6).Sum(b => b) & 0xff) == buffer[6]
                                    && rawBuffer[15] == '\r' && rawBuffer[16] == '\n')
                                {
                                    messageLength = 17;
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
                                var byteLength = buffer[6];
                                messageLength = byteLength * 2 + 19;

                                if (function == ModbusFunction.WriteMultipleCoils && byteLength == Math.Ceiling(valueOrLength / 8d)
                                    || function == ModbusFunction.WriteMultipleHoldingRegisters && byteLength == valueOrLength * 2)
                                {
                                    while (rawBuffer.Count < messageLength && !rawBuffer.Channel.IsDisposed)
                                        rawBuffer.Read();

                                    if (rawBuffer.Channel.IsDisposed) break;

                                    for (int i = 0; i < byteLength + 1; i++)
                                    {
                                        if (TryParseFromHex(rawBuffer, i * 2 + 15, out var byteValue))
                                            buffer.Add(byteValue);
                                        else
                                            break;
                                    }

                                    if (buffer.Count < 8 + byteLength)
                                    {
                                        errorBuffer.AddRange(rawBuffer.Take((buffer.Count + 1) * 2 + 1));
                                        rawBuffer.RemoveRange(0, (buffer.Count + 1) * 2 + 1);
                                        continue;
                                    }


                                    if ((-buffer.Take(7 + byteLength).Sum(b => b) & 0xff) == buffer[6]
                                        && rawBuffer[15] == '\r' && rawBuffer[16] == '\n')
                                    {
                                        switch (function)
                                        {
                                            case ModbusFunction.WriteMultipleCoils:
                                                result = new ModbusWriteCoilRequest(slaveAddress, address, buffer.Skip(7).Take(byteLength).SelectMany(b => ByteToBooleanArray(b)).Take(valueOrLength).ToArray());
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
                }

                if (result != null)
                {
                    if (errorBuffer.Count > 0)
                    {
                        RaiseUnrecognized(rawBuffer.Channel, errorBuffer.ToArray());
                        errorBuffer.Clear();
                    }
                    return result;
                }
                else
                {
                    errorBuffer.Add(rawBuffer[0]);
                    rawBuffer.RemoveAt(0);
                    continue;
                }
            }
            return null;
        }

    }
}
