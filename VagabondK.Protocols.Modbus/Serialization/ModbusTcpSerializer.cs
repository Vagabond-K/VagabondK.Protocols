using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VagabondK.Protocols.Channels;

namespace VagabondK.Protocols.Modbus.Serialization
{
    /// <summary>
    /// Modbus TCP Serializer
    /// </summary>
    public sealed class ModbusTcpSerializer : ModbusSerializer
    {
        private ushort transactionID = 0;
        private readonly Dictionary<ushort, ResponseWaitHandle> responseWaitHandles = new Dictionary<ushort, ResponseWaitHandle>();
        private bool isReceiving = false;
        private readonly List<byte> errorBuffer = new List<byte>();

        class ResponseWaitHandle : EventWaitHandle
        {
            public ResponseWaitHandle(ResponseBuffer buffer, ModbusRequest request, int timeout) : base(false, EventResetMode.ManualReset)
            {
                ResponseBuffer = buffer;
                Request = request;
                Timeout = timeout;
            }

            public ResponseBuffer ResponseBuffer { get; }
            public ModbusRequest Request { get; }
            public int Timeout { get; }
            public ModbusResponse Response { get; set; }
        }


        internal override IEnumerable<byte> OnSerialize(IModbusMessage message)
        {
            if (message is ModbusRequest request && request.TransactionID == null)
                request.TransactionID = transactionID++;

            var messageArray = message.Serialize().ToArray();
            int messageLength = messageArray.Length;

            yield return (byte)((message.TransactionID >> 8) & 0xff);
            yield return (byte)(message.TransactionID & 0xff);
            yield return 0;
            yield return 0;
            yield return (byte)((messageLength >> 8) & 0xff);
            yield return (byte)(messageLength & 0xff);

            foreach (var b in messageArray)
            {
                yield return b;
            }
        }

        private bool IsException(ResponseBuffer buffer, ModbusRequest request, int timeout, out ModbusResponse responseMessage)
        {
            if ((Read(buffer, 7, timeout) & 0x80) == 0x80)
            {
                var codeValue = Read(buffer, 8, timeout);

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


        private void RunReceive(Channel channel)
        {
            lock (responseWaitHandles)
            {
                if (!isReceiving)
                {
                    isReceiving = true;
                    try
                    {
                        var readBuffer = new ResponseBuffer(channel);

                        while (responseWaitHandles.Count > 0)
                        {
                            if (errorBuffer.Count >= 256)
                            {
                                RaiseUnrecognized(channel, errorBuffer.ToArray());
                                errorBuffer.Clear();
                            }

                            readBuffer.Read(2, 0);

                            ushort transactionID = (ushort)((readBuffer[0] << 8) | readBuffer[1]);
                            if (!responseWaitHandles.TryGetValue(transactionID, out var responseWaitHandle))
                            {
                                errorBuffer.AddRange(readBuffer);
                                readBuffer.Clear();
                                continue;
                            }

                            readBuffer.Read(2, responseWaitHandle.Timeout);
                            if (readBuffer[2] != 0
                                || readBuffer[3] != 0)
                            {
                                errorBuffer.AddRange(readBuffer);
                                readBuffer.Clear();

                                responseWaitHandle.Response = new ModbusCommErrorResponse(ModbusCommErrorCode.ModbusTcpSymbolError, errorBuffer, responseWaitHandle.Request);
                                continue;
                            }

                            var result = base.DeserializeResponse(readBuffer, responseWaitHandle.Request, responseWaitHandle.Timeout);

                            if (result is ModbusCommErrorResponse responseCommErrorMessage)
                            {
                                errorBuffer.AddRange(readBuffer.Take(4));
                                readBuffer.RemoveRange(0, 4);

                                responseWaitHandle.Response = result;
                                continue;
                            }

                            if (readBuffer.Count - 6 != (ushort)((readBuffer[4] << 8) | readBuffer[5]))
                            {
                                errorBuffer.AddRange(readBuffer.Take(4));
                                readBuffer.RemoveRange(0, 4);

                                responseWaitHandle.Response = new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseTcpLengthDoNotMatch, readBuffer, responseWaitHandle.Request);
                                continue;
                            }


                            if (errorBuffer.Count > 0)
                            {
                                RaiseUnrecognized(channel, errorBuffer.ToArray());
                                errorBuffer.Clear();
                            }

                            responseWaitHandle.ResponseBuffer.AddRange(readBuffer);

                            readBuffer.Clear();
                            responseWaitHandle.Response = result;
                            responseWaitHandles.Remove(transactionID);
                            responseWaitHandle.Set();
                        }
                    }
                    catch
                    {
                    }
                    isReceiving = false;
                }
            }
        }

        internal override ModbusResponse DeserializeResponse(ResponseBuffer buffer, ModbusRequest request, int timeout)
        {
            if (responseWaitHandles.TryGetValue(request.TransactionID.Value, out var oldHandle))
                oldHandle.WaitOne(timeout);

            var responseWaitHandle = responseWaitHandles[request.TransactionID.Value] = new ResponseWaitHandle(buffer, request, timeout);

            Task.Run(() => RunReceive(buffer.Channel));

            responseWaitHandle.WaitOne(timeout);

            var result = responseWaitHandle.Response;
            if (result == null)
            {
                responseWaitHandles.Remove(request.TransactionID.Value);
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseTimeout, new byte[0], request);
            }

            return result;
        }

        internal override ModbusResponse DeserializeReadBooleanResponse(ResponseBuffer buffer, ModbusReadRequest request, int timeout)
        {
            if (IsException(buffer, request, timeout, out var responseMessage))
                return responseMessage;

            byte byteLength = Read(buffer, 8, timeout);

            if (Read(buffer, 6, timeout) != request.SlaveAddress)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseSlaveAddressDoNotMatch, buffer, request);
            if ((Read(buffer, 7, timeout) & 0x7f) != (byte)request.Function)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseFunctionDoNotMatch, buffer, request);
            if (byteLength != (byte)Math.Ceiling(request.Length / 8d))
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseLengthDoNotMatch, buffer, request);

            return new ModbusReadBooleanResponse(Read(buffer, 9, byteLength, timeout).SelectMany(b => ByteToBooleanArray(b)).Take(request.Length).ToArray(), request);
        }

        internal override ModbusResponse DeserializeReadRegisterResponse(ResponseBuffer buffer, ModbusReadRequest request, int timeout)
        {
            if (IsException(buffer, request, timeout, out var responseMessage))
                return responseMessage;

            byte byteLength = Read(buffer, 8, timeout);

            if (Read(buffer, 6, timeout) != request.SlaveAddress)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseSlaveAddressDoNotMatch, buffer, request);
            if ((Read(buffer, 7, timeout) & 0x7f) != (byte)request.Function)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseFunctionDoNotMatch, buffer, request);
            if (byteLength != (byte)(request.Length * 2))
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseLengthDoNotMatch, buffer, request);

            return new ModbusReadRegisterResponse(Read(buffer, 9, byteLength, timeout).ToArray(), request);
        }

        internal override ModbusResponse DeserializeWriteResponse(ResponseBuffer buffer, ModbusWriteCoilRequest request, int timeout)
        {
            if (IsException(buffer, request, timeout, out var responseMessage))
                return responseMessage;

            if (Read(buffer, 6, timeout) != request.SlaveAddress)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseSlaveAddressDoNotMatch, buffer, request);
            if ((Read(buffer, 7, timeout) & 0x7f) != (byte)request.Function)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseFunctionDoNotMatch, buffer, request);

            Read(buffer, 11, timeout);

            if (ToUInt16(buffer, 8) != request.Address)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseAddressDoNotMatch, buffer, request);

            switch (request.Function)
            {
                case ModbusFunction.WriteSingleCoil:
                    if (Read(buffer, 10, timeout) != (request.SingleBooleanValue ? 0xff : 0x00)
                        || Read(buffer, 11, timeout) != 0x00)
                        return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseWritedValueDoNotMatch, buffer, request);
                    break;
                case ModbusFunction.WriteMultipleCoils:
                    if (ToUInt16(buffer, 10) != request.Length)
                        return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseWritedLengthDoNotMatch, buffer, request);
                    break;
            }

            return new ModbusWriteResponse(request);
        }

        internal override ModbusResponse DeserializeWriteResponse(ResponseBuffer buffer, ModbusWriteHoldingRegisterRequest request, int timeout)
        {
            if (IsException(buffer, request, timeout, out var responseMessage))
                return responseMessage;

            if (Read(buffer, 6, timeout) != request.SlaveAddress)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseSlaveAddressDoNotMatch, buffer, request);
            if ((Read(buffer, 7, timeout) & 0x7f) != (byte)request.Function)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseFunctionDoNotMatch, buffer, request);

            Read(buffer, 11, timeout);

            if (ToUInt16(buffer, 8) != request.Address)
                return new ModbusCommErrorResponse(ModbusCommErrorCode.ResponseAddressDoNotMatch, buffer, request);

            ushort value = ToUInt16(buffer, 10);

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


        internal override ModbusRequest DeserializeRequest(RequestBuffer buffer)
        {
            ModbusRequest result = null;
            while (!buffer.Channel.IsDisposed)
            {
                if (errorBuffer.Count >= 256)
                {
                    RaiseUnrecognized(buffer.Channel, errorBuffer.ToArray());
                    errorBuffer.Clear();
                }

                while (buffer.Count < 12 && !buffer.Channel.IsDisposed)
                    buffer.Read();

                if (buffer.Channel.IsDisposed) break;

                var messageLength = ToUInt16(buffer, 4);
                ushort transactionID = 0;
                if (ToUInt16(buffer, 2) == 0)
                {
                    transactionID = ToUInt16(buffer, 0);
                    var slaveAddress = buffer[6];

                    if (messageLength >= 6
                        && buffer.ModbusSlave.IsValidSlaveAddress(slaveAddress, buffer.Channel)
                        && Enum.IsDefined(typeof(ModbusFunction), buffer[7]))
                    {
                        ModbusFunction function = (ModbusFunction)buffer[7];
                        var address = ToUInt16(buffer, 8);
                        var valueOrLength = ToUInt16(buffer, 10);

                        switch (function)
                        {
                            case ModbusFunction.ReadCoils:
                            case ModbusFunction.ReadDiscreteInputs:
                            case ModbusFunction.ReadHoldingRegisters:
                            case ModbusFunction.ReadInputRegisters:
                            case ModbusFunction.WriteSingleCoil:
                            case ModbusFunction.WriteSingleHoldingRegister:
                                if (messageLength == 6)
                                {
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
                                if (buffer.Count < 13 && !buffer.Channel.IsDisposed)
                                    buffer.Read();

                                if (buffer.Channel.IsDisposed) break;

                                var byteLength = buffer[12];

                                if (messageLength - 7 == byteLength
                                    && (function == ModbusFunction.WriteMultipleCoils && byteLength == Math.Ceiling(valueOrLength / 8d)
                                    || function == ModbusFunction.WriteMultipleHoldingRegisters && byteLength == valueOrLength * 2))
                                {
                                    while (buffer.Count < 6 + messageLength && !buffer.Channel.IsDisposed)
                                        buffer.Read();

                                    if (buffer.Channel.IsDisposed) break;

                                    switch (function)
                                    {
                                        case ModbusFunction.WriteMultipleCoils:
                                            result = new ModbusWriteCoilRequest(slaveAddress, address, buffer.Skip(13).Take(byteLength).SelectMany(b => ByteToBooleanArray(b)).Take(valueOrLength).ToArray());
                                            break;
                                        case ModbusFunction.WriteMultipleHoldingRegisters:
                                            result = new ModbusWriteHoldingRegisterRequest(slaveAddress, address, buffer.Skip(13).Take(byteLength).ToArray());
                                            break;
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
                        RaiseUnrecognized(buffer.Channel, errorBuffer.ToArray());
                        errorBuffer.Clear();
                    }
                    result.TransactionID = transactionID;
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
    }
}
