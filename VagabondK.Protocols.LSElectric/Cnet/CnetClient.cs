using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;

namespace VagabondK.Protocols.LSElectric.Cnet
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 기반 클라이언트입니다.
    /// XGT 시리즈 제품의 Cnet I/F 모듈과 통신 가능합니다.
    /// </summary>
    public class CnetClient : IDisposable
    {
        /// <summary>
        /// 생성자
        /// </summary>
        public CnetClient() { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        public CnetClient(IChannel channel)
        {
            this.channel = channel;
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            channel?.Dispose();
        }


        private IChannel channel;


        /// <summary>
        /// 통신 채널
        /// </summary>
        public IChannel Channel
        {
            get => channel;
            set
            {
                if (channel != value)
                {
                    channel = value;
                }
            }
        }


        /// <summary>
        /// 응답 제한시간(밀리초)
        /// </summary>
        public int Timeout { get; set; } = 1000;

        /// <summary>
        /// NAK에 대한 예외 발생 여부
        /// </summary>
        public bool ThrowsExceptionFromNAK { get; set; } = true;


        /// <summary>
        /// Cnet 요청하기
        /// </summary>
        /// <param name="request">Cnet 요청</param>
        /// <returns>Cnet 응답</returns>
        public CnetResponse Request(CnetRequest request) => Request(request, Timeout);

        /// <summary>
        /// Cnet 요청하기
        /// </summary>
        /// <param name="request">Cnet 요청</param>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <returns>Cnet 응답</returns>
        public CnetResponse Request(CnetRequest request, int timeout)
        {
            request = (CnetRequest)request.Clone();

            Channel channel = (Channel as Channel) ?? (Channel as ChannelProvider)?.PrimaryChannel;

            if (channel == null)
                throw new ArgumentNullException(nameof(Channel));


            var requestMessage = request.Serialize().ToArray();

            channel.Write(requestMessage);
            var requestLog = new CnetRequestLog(channel, request, requestMessage);
            channel?.Logger?.Log(requestLog);

            CnetResponse result;
            List<byte> buffer = new List<byte>();
            List<byte> errorBuffer = new List<byte>();

            try
            {
                result = DeserializeResponse(channel, buffer, request, timeout);

                while (result is CnetCommErrorResponse responseCommErrorMessage
                    && responseCommErrorMessage.ErrorCode != CnetCommErrorCode.ResponseTimeout)
                {
                    errorBuffer.Add(buffer[0]);
                    buffer.RemoveAt(0);
                    result = DeserializeResponse(channel, buffer, request, timeout);
                }

                if (result is CnetCommErrorResponse responseCommError)
                {
                    result = new CnetCommErrorResponse(responseCommError.ErrorCode, errorBuffer.Concat(responseCommError.ReceivedBytes), request);
                }
                else if (errorBuffer.Count > 0)
                {
                    channel?.Logger?.Log(new UnrecognizedErrorLog(channel, errorBuffer.ToArray()));
                    errorBuffer.Clear();
                }
            }
            catch (Exception ex)
            {
                channel?.Logger?.Log(new ChannelErrorLog(channel, ex));
                throw ex;
            }

            if (result is CnetCommErrorResponse commErrorResponse)
            {
                var ex = new RequestException<CnetCommErrorCode>(commErrorResponse.ErrorCode, commErrorResponse.ReceivedBytes, commErrorResponse.Request);
                channel?.Logger?.Log(new ChannelErrorLog(channel, ex));
                throw ex;
            }

            if (result is CnetNAKResponse exceptionResponse)
            {
                channel?.Logger?.Log(new CnetNAKLog(channel, exceptionResponse, buffer.ToArray(), requestLog));
                if (ThrowsExceptionFromNAK)
                    throw new ErrorCodeException<CnetNAKCode>(exceptionResponse.NAKCode);
            }
            else
                channel?.Logger?.Log(new CnetResponseLog(channel, result, result is CnetNAKResponse ? null : buffer.ToArray(), requestLog));

            return result;
        }

        private CnetResponse DeserializeResponse(Channel channel, List<byte> buffer, CnetRequest request, int timeout)
        {
            buffer.Add(channel.Read(timeout));
            if (buffer[0] != CnetMessage.ACK && buffer[0] != CnetMessage.NAK)
                return new CnetCommErrorResponse(CnetCommErrorCode.ResponseHeaderError, buffer, request);

            var requestMessage = request.Serialize().ToArray();

            buffer.AddRange(channel.Read(2, timeout));
            if (buffer[1] != requestMessage[1] || buffer[2] != requestMessage[2])
                return new CnetCommErrorResponse(CnetCommErrorCode.ResponseStationNumberDoNotMatch, buffer, request);

            buffer.Add(channel.Read(timeout));
            if (buffer[3] != requestMessage[3])
                return new CnetCommErrorResponse(CnetCommErrorCode.ResponseCommandDoNotMatch, buffer, request);

            buffer.AddRange(channel.Read(2, timeout));
            if (buffer[4] != requestMessage[4] || buffer[5] != requestMessage[5])
                switch (request.Command)
                {
                    case CnetCommand.Read:
                    case CnetCommand.Write:
                        return new CnetCommErrorResponse(CnetCommErrorCode.ResponseCommandTypeDoNotMatch, buffer, request);
                    default:
                        return new CnetCommErrorResponse(CnetCommErrorCode.ResponseMonitorNumberDoNotMatch, buffer, request);
                }

            if (buffer[0] == CnetMessage.NAK)
            {
                buffer.AddRange(channel.Read(4, timeout));

                var tailErrorResponse = DeserializeTail(buffer, request, timeout);
                if (tailErrorResponse != null) return tailErrorResponse;

                if (!CnetMessage.TryParseUint16(buffer, 6, out var errorCode))
                    return new CnetCommErrorResponse(CnetCommErrorCode.ResponseParseHexError, buffer, request);

                return new CnetNAKResponse(errorCode, request);
            }

            if (request.Command == CnetCommand.Write
                || request.Command == CnetCommand.RegisterMonitor)
                return DeserializeTail(buffer, request, timeout) ?? new CnetACKResponse(request);

            List<DeviceValue> deviceValues;
            List<byte> bytes;

            if (request is CnetIncludeCommandTypeRequest commandTypeRequest)
            {
                switch (commandTypeRequest.CommandType)
                {
                    case CnetCommandType.Individual:
                        switch (request.Command)
                        {
                            case CnetCommand.Read:
                                return DeserializeIndividualDataResponse(buffer, request, timeout, out deviceValues) ?? new CnetReadResponse(deviceValues, request as CnetReadIndividualRequest);
                            case CnetCommand.ExecuteMonitor:
                                return DeserializeIndividualDataResponse(buffer, request, timeout, out deviceValues) ?? new CnetReadResponse(deviceValues, request as CnetExecuteMonitorRequest);
                        }
                        break;
                    case CnetCommandType.Continuous:
                        switch (request.Command)
                        {
                            case CnetCommand.Read:
                                return DeserializeContinuousDataResponse(buffer, request, timeout, out bytes) ?? new CnetReadResponse(bytes, request as CnetReadContinuousRequest);
                            case CnetCommand.ExecuteMonitor:
                                return DeserializeContinuousDataResponse(buffer, request, timeout, out bytes) ?? new CnetReadResponse(bytes, request as CnetExecuteMonitorContinuousRequest);
                        }
                        break;
                }
            }

            throw new NotImplementedException();
        }

        private CnetResponse DeserializeIndividualDataResponse(List<byte> buffer, CnetRequest request, int timeout, out List<DeviceValue> deviceValues)
        {
            var deviceVariables = ((IEnumerable<DeviceVariable>)request).ToArray();
            deviceValues = new List<DeviceValue>();

            buffer.AddRange(channel.Read(2, timeout));
            if (!CnetMessage.TryParseByte(buffer, 6, out var blockCount))
                return new CnetCommErrorResponse(CnetCommErrorCode.ResponseParseHexError, buffer, request);
            if (blockCount != deviceVariables.Length)
                return new CnetCommErrorResponse(CnetCommErrorCode.ResponseDataBlockCountDoNotMatch, buffer, request);

            for (int i = 0; i < blockCount; i++)
            {
                buffer.AddRange(channel.Read(2, timeout));
                if (!CnetMessage.TryParseByte(buffer, buffer.Count - 2, out byte dataCount))
                    return new CnetCommErrorResponse(CnetCommErrorCode.ResponseParseHexError, buffer, request);

                buffer.AddRange(channel.Read((uint)dataCount * 2, timeout));
                
                switch (deviceVariables[i].DataType)
                {
                    case DataType.Bit:
                        if (dataCount != 1) return new CnetCommErrorResponse(CnetCommErrorCode.ResponseDataCountDoNotMatch, buffer, request);
                        else if (CnetMessage.TryParseByte(buffer, buffer.Count - dataCount * 2, out var value)) deviceValues.Add(new DeviceValue(value != 0));
                        break;
                    case DataType.Byte:
                        if (dataCount != 1) return new CnetCommErrorResponse(CnetCommErrorCode.ResponseDataCountDoNotMatch, buffer, request);
                        else if (CnetMessage.TryParseByte(buffer, buffer.Count - dataCount * 2, out var value)) deviceValues.Add(new DeviceValue(value));
                        break;
                    case DataType.Word:
                        if (dataCount != 2) return new CnetCommErrorResponse(CnetCommErrorCode.ResponseDataCountDoNotMatch, buffer, request);
                        else if (CnetMessage.TryParseUint16(buffer, buffer.Count - dataCount * 2, out var value)) deviceValues.Add(new DeviceValue(value));
                        break;
                    case DataType.DoubleWord:
                        if (dataCount != 4) return new CnetCommErrorResponse(CnetCommErrorCode.ResponseDataCountDoNotMatch, buffer, request);
                        else if (CnetMessage.TryParseUint32(buffer, buffer.Count - dataCount * 2, out var value)) deviceValues.Add(new DeviceValue(value));
                        break;
                    case DataType.LongWord:
                        if (dataCount != 8) return new CnetCommErrorResponse(CnetCommErrorCode.ResponseDataCountDoNotMatch, buffer, request);
                        else if (CnetMessage.TryParseUint64(buffer, buffer.Count - dataCount * 2, out var value)) deviceValues.Add(new DeviceValue(value));
                        break;
                    default:
                        deviceValues.Add(new DeviceValue());
                        break;
                }
            }

            return DeserializeTail(buffer, request, timeout);
        }

        private CnetResponse DeserializeContinuousDataResponse(List<byte> buffer, CnetRequest request, int timeout, out List<byte> bytes)
        {
            ICnetContinuousAccessRequest continuousAccessRequest = (ICnetContinuousAccessRequest)request;
            bytes = new List<byte>();

            int dataUnit;
            switch (continuousAccessRequest.StartDeviceVariable.DataType)
            {
                case DataType.Word:
                    dataUnit = 2;
                    break;
                case DataType.DoubleWord:
                    dataUnit = 4;
                    break;
                case DataType.LongWord:
                    dataUnit = 8;
                    break;
                default:
                    dataUnit = 1;
                    break;
            }

            buffer.AddRange(channel.Read(2, timeout));
            if (!CnetMessage.TryParseByte(buffer, 6, out var dataCount))
                return new CnetCommErrorResponse(CnetCommErrorCode.ResponseParseHexError, buffer, request);
            if (dataCount != dataUnit * continuousAccessRequest.Count)
                return new CnetCommErrorResponse(CnetCommErrorCode.ResponseDataCountDoNotMatch, buffer, request);

            buffer.AddRange(channel.Read((uint)dataCount * 2, timeout));

            var tailErrorResponse = DeserializeTail(buffer, request, timeout);
            if (tailErrorResponse != null) return tailErrorResponse;

            for (int i = 0; i < dataCount; i++)
            {
                if (!CnetMessage.TryParseByte(buffer, 8 + i * 2, out var value))
                    return new CnetCommErrorResponse(CnetCommErrorCode.ResponseParseHexError, buffer, request);
                bytes.Add(value);
            }

            return null;
        }

        private CnetResponse DeserializeTail(List<byte> buffer, CnetRequest request, int timeout)
        {
            buffer.Add(channel.Read(timeout));
            if (buffer[buffer.Count - 1] != CnetMessage.ETX)
                return new CnetCommErrorResponse(CnetCommErrorCode.ResponseTailError, buffer, request);

            if (request.UseBCC)
            {
                buffer.AddRange(channel.Read(2, timeout));
                if (!buffer.Skip(buffer.Count - 2).SequenceEqual(Encoding.ASCII.GetBytes((buffer.Take(buffer.Count - 2).Sum(b => b) % 256).ToString("X2"))))
                    return new CnetCommErrorResponse(CnetCommErrorCode.ErrorBCC, buffer, request);
            }

            return null;
        }


        class CnetCommErrorResponse : CnetResponse
        {
            public CnetCommErrorResponse(CnetCommErrorCode errorCode, IEnumerable<byte> receivedMessage, CnetRequest request) : base(request)
            {
                ErrorCode = errorCode;
                ReceivedBytes = receivedMessage?.ToArray() ?? new byte[0];
            }

            public CnetCommErrorCode ErrorCode { get; }
            public IReadOnlyList<byte> ReceivedBytes { get; }

            public override byte Header => throw new NotImplementedException();

            public override IEnumerable<byte> Serialize()
            {
                return ReceivedBytes;
            }

            protected override void OnCreateFrame(List<byte> byteList, out bool useBCC)
            {
                useBCC = Request.UseBCC;
            }

            protected override void OnCreateFrameData(List<byte> byteList) { }

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
}