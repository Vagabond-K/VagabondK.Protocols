using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.LSElectric.Cnet.Logging;

namespace VagabondK.Protocols.LSElectric.Cnet
{
    /// <summary>
    /// LS ELECTRIC Cnet 프로토콜 기반 클라이언트입니다.
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
        /// Error Code에 대한 예외 발생 여부
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
            channel.ReadAllRemain().ToArray();
            channel.Write(requestMessage);
            channel?.Logger?.Log(new CnetMessageLog(channel, request, requestMessage));

            CnetResponse result;
            List<byte> buffer = new List<byte>();

            try
            {
                result = Deserialize(channel, buffer, request, timeout);
            }
            catch (TimeoutException ex)
            {
                throw new RequestException<CnetCommErrorCode>(CnetCommErrorCode.ResponseTimeout, buffer, ex, request);
            }
            catch (RequestException<CnetCommErrorCode> ex)
            {
                channel?.Logger?.Log(new ChannelErrorLog(channel, ex));
                throw ex;
            }
            catch (Exception ex)
            {
                throw new RequestException<CnetCommErrorCode>(buffer, ex, request);
            }

            if (result is CnetNAKResponse nakResponse)
            {
                channel?.Logger?.Log(new CnetNAKLog(channel, nakResponse.NAKCode, buffer.ToArray()));
                //if (ThrowsExceptionFromNAK)
                //    throw new CnetException(nakResponse.ExceptionCode);
            }
            else
                channel?.Logger?.Log(new CnetMessageLog(channel, result, result is CnetCommErrorResponse ? null : buffer.ToArray()));


            return result;
        }

        private CnetResponse Deserialize(Channel channel, List<byte> buffer, CnetRequest request, int timeout)
        {
            buffer.Add(channel.Read(timeout));
            if (buffer[0] != CnetMessage.ACK || buffer[0] != CnetMessage.NAK)
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

                if (!TryParseUint16(buffer, 6, out var errorCode))
                    return new CnetCommErrorResponse(CnetCommErrorCode.ResponseParseHexError, buffer, request);

                return new CnetNAKResponse(errorCode, request);
            }

            if (request.Command == CnetCommand.Write
                || request.Command == CnetCommand.RegisterMonitor)
                return DeserializeTail(buffer, request, timeout) ?? new CnetACKResponse(request);

            List<DeviceValue> values;
            List<byte> bytes;

            if (request is CnetIncludeCommandTypeRequest commandTypeRequest)
            {
                switch (commandTypeRequest.CommandType)
                {
                    case CnetCommandType.Each:
                        switch (request.Command)
                        {
                            case CnetCommand.Read:
                                return DeserializeEachAddressDataResponse(buffer, request, timeout, out values) ?? new CnetReadResponse(values, request as CnetReadEachAddressRequest);
                            case CnetCommand.ExecuteMonitor:
                                return DeserializeEachAddressDataResponse(buffer, request, timeout, out values) ?? new CnetReadResponse(values, request as CnetExecuteMonitorEachAddressRequest);
                        }
                        break;
                    case CnetCommandType.Block:
                        switch (request.Command)
                        {
                            case CnetCommand.Read:
                                return DeserializeAddressBlockDataResponse(buffer, request, timeout, out bytes) ?? new CnetReadResponse(bytes, request as CnetReadAddressBlockRequest);
                            case CnetCommand.ExecuteMonitor:
                                return DeserializeAddressBlockDataResponse(buffer, request, timeout, out bytes) ?? new CnetReadResponse(bytes, request as CnetExecuteMonitorAddressBlockRequest);
                        }
                        break;
                }
            }

            throw new NotImplementedException();
        }

        private CnetResponse DeserializeEachAddressDataResponse(List<byte> buffer, CnetRequest request, int timeout, out List<DeviceValue> values)
        {
            var addresses = ((IEnumerable<DeviceAddress>)request).ToArray();
            values = new List<DeviceValue>();

            buffer.AddRange(channel.Read(2, timeout));
            if (!TryParseByte(buffer, 6, out var blockCount))
                return new CnetCommErrorResponse(CnetCommErrorCode.ResponseParseHexError, buffer, request);
            if (blockCount != addresses.Length)
                return new CnetCommErrorResponse(CnetCommErrorCode.ResponseDataBlockCountDoNotMatch, buffer, request);

            for (int i = 0; i < blockCount; i++)
            {
                buffer.AddRange(channel.Read(2, timeout));
                if (!TryParseByte(buffer, buffer.Count - 2, out byte dataCount))
                    return new CnetCommErrorResponse(CnetCommErrorCode.ResponseParseHexError, buffer, request);

                buffer.AddRange(channel.Read((uint)dataCount * 2, timeout));
                
                switch (addresses[i].DataType)
                {
                    case DataType.Bit:
                        if (dataCount != 1) return new CnetCommErrorResponse(CnetCommErrorCode.ResponseDataCountDoNotMatch, buffer, request);
                        else if (TryParseByte(buffer, buffer.Count - dataCount * 2, out var value)) values.Add(new DeviceValue(value != 0));
                        break;
                    case DataType.Byte:
                        if (dataCount != 1) return new CnetCommErrorResponse(CnetCommErrorCode.ResponseDataCountDoNotMatch, buffer, request);
                        else if (TryParseByte(buffer, buffer.Count - dataCount * 2, out var value)) values.Add(new DeviceValue(value));
                        break;
                    case DataType.Word:
                        if (dataCount != 2) return new CnetCommErrorResponse(CnetCommErrorCode.ResponseDataCountDoNotMatch, buffer, request);
                        else if (TryParseUint16(buffer, buffer.Count - dataCount * 2, out var value)) values.Add(new DeviceValue(value));
                        break;
                    case DataType.DoubleWord:
                        if (dataCount != 4) return new CnetCommErrorResponse(CnetCommErrorCode.ResponseDataCountDoNotMatch, buffer, request);
                        else if (TryParseUint32(buffer, buffer.Count - dataCount * 2, out var value)) values.Add(new DeviceValue(value));
                        break;
                    case DataType.LongWord:
                        if (dataCount != 8) return new CnetCommErrorResponse(CnetCommErrorCode.ResponseDataCountDoNotMatch, buffer, request);
                        else if (TryParseUint64(buffer, buffer.Count - dataCount * 2, out var value)) values.Add(new DeviceValue(value));
                        break;
                    default:
                        values.Add(new DeviceValue());
                        break;
                }
            }

            return DeserializeTail(buffer, request, timeout);
        }

        private CnetResponse DeserializeAddressBlockDataResponse(List<byte> buffer, CnetRequest request, int timeout, out List<byte> bytes)
        {
            ICnetAddressBlockRequest addressBlockRequest = (ICnetAddressBlockRequest)request;
            bytes = new List<byte>();

            int dataUnit;
            switch (addressBlockRequest.DeviceAddress.DataType)
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
            if (!TryParseByte(buffer, 6, out var dataCount))
                return new CnetCommErrorResponse(CnetCommErrorCode.ResponseParseHexError, buffer, request);
            if (dataCount != dataUnit * addressBlockRequest.Count)
                return new CnetCommErrorResponse(CnetCommErrorCode.ResponseDataCountDoNotMatch, buffer, request);

            buffer.AddRange(channel.Read((uint)dataUnit * (uint)addressBlockRequest.Count * 2, timeout));

            var tailErrorResponse = DeserializeTail(buffer, request, timeout);
            if (tailErrorResponse != null) return tailErrorResponse;

            for (int i = 0; i < dataCount; i++)
            {
                if (!TryParseByte(buffer, 7 + i * 2, out var value))
                    return new CnetCommErrorResponse(CnetCommErrorCode.ResponseParseHexError, buffer, request);
                bytes.Add(value);
            }

            return null;
        }

        private CnetResponse DeserializeTail(List<byte> buffer, CnetRequest request, int timeout)
        {
            buffer.Add(channel.Read(timeout));
            if (buffer[10] != CnetMessage.ETX)
                return new CnetCommErrorResponse(CnetCommErrorCode.ResponseTailError, buffer, request);

            if (request.UseBCC)
            {
                buffer.AddRange(channel.Read(2, timeout));
                if (!buffer.Skip(buffer.Count - 2).SequenceEqual(Encoding.ASCII.GetBytes((buffer.Take(buffer.Count - 2).Sum(b => b) % 256).ToString("X2"))))
                    return new CnetCommErrorResponse(CnetCommErrorCode.ErrorBCC, buffer, request);
            }

            return null;
        }

        private static bool TryParseByte(IList<byte> bytes, int index, out byte value)
            => byte.TryParse($"{(char)bytes[index]}{(char)bytes[index + 1]}", System.Globalization.NumberStyles.HexNumber, null, out value);
        private static bool TryParseUint16(IList<byte> bytes, int index, out ushort value)
            => ushort.TryParse($"{(char)bytes[index]}{(char)bytes[index + 1]}{(char)bytes[index + 2]}{(char)bytes[index + 3]}", System.Globalization.NumberStyles.HexNumber, null, out value);
        private static bool TryParseUint32(IList<byte> bytes, int index, out uint value)
            => uint.TryParse($"{(char)bytes[index]}{(char)bytes[index + 1]}{(char)bytes[index + 2]}{(char)bytes[index + 3]}{(char)bytes[index + 4]}{(char)bytes[index + 5]}{(char)bytes[index + 6]}{(char)bytes[index + 7]}", System.Globalization.NumberStyles.HexNumber, null, out value);
        private static bool TryParseUint64(IList<byte> bytes, int index, out ulong value)
            => ulong.TryParse($"{(char)bytes[index]}{(char)bytes[index + 1]}{(char)bytes[index + 2]}{(char)bytes[index + 3]}{(char)bytes[index + 4]}{(char)bytes[index + 5]}{(char)bytes[index + 6]}{(char)bytes[index + 7]}{(char)bytes[index + 8]}{(char)bytes[index + 9]}{(char)bytes[index + 10]}{(char)bytes[index + 11]}{(char)bytes[index + 12]}{(char)bytes[index + 13]}{(char)bytes[index + 14]}{(char)bytes[index + 15]}", System.Globalization.NumberStyles.HexNumber, null, out value);

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