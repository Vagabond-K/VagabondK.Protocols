using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;

namespace VagabondK.Protocols.LSElectric.FEnet
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) FEnet 프로토콜 기반 클라이언트입니다.
    /// XGT 시리즈 제품의 FEnet I/F 모듈과 통신 가능합니다.
    /// </summary>
    public class FEnetClient : IDisposable
    {
        private ushort invokeID = 0;
        private readonly Dictionary<ushort, ResponseWaitHandle> responseWaitHandles = new Dictionary<ushort, ResponseWaitHandle>();
        private bool isReceiving = false;
        private readonly List<byte> errorBuffer = new List<byte>();

        class ResponseWaitHandle : EventWaitHandle
        {
            public ResponseWaitHandle(List<byte> buffer, FEnetRequest request, int timeout) : base(false, EventResetMode.ManualReset)
            {
                ResponseBuffer = buffer;
                Request = request;
                Timeout = timeout;
            }

            public List<byte> ResponseBuffer { get; }
            public FEnetRequest Request { get; }
            public int Timeout { get; }
            public FEnetResponse Response { get; set; }
        }


        /// <summary>
        /// 생성자
        /// </summary>
        public FEnetClient() { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        public FEnetClient(IChannel channel)
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
        /// 메시지 헤더의 CompanyID. 기본값은 LSIS-XGT. 단종된 모델에서는 LGIS-GLOFA를 사용할 수도 있음.
        /// </summary>
        public string CompanyID { get; set; } = "LSIS-XGT";

        /// <summary>
        /// 기본적으로 체크섬을 사용할 것인지 여부, 기본값은 true
        /// </summary>
        public bool UseChecksum { get; set; } = true;

        /// <summary>
        /// 응답 제한시간(밀리초)
        /// </summary>
        public int Timeout { get; set; } = 1000;

        /// <summary>
        /// NAK에 대한 예외 발생 여부
        /// </summary>
        public bool ThrowsExceptionFromNAK { get; set; } = true;


        /// <summary>
        /// FEnet 요청하기
        /// </summary>
        /// <param name="request">FEnet 요청</param>
        /// <returns>FEnet 응답</returns>
        public FEnetResponse Request(FEnetRequest request) => Request(Timeout, request);

        /// <summary>
        /// FEnet 요청하기
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="request">FEnet 요청</param>
        /// <returns>FEnet 응답</returns>
        public FEnetResponse Request(int timeout, FEnetRequest request)
        {
            if (request.InvokeID == null)
                request.InvokeID = invokeID++;

            request = (FEnetRequest)request.Clone();

            Channel channel = (Channel as Channel) ?? (Channel as ChannelProvider)?.PrimaryChannel;

            if (channel == null)
                throw new ArgumentNullException(nameof(Channel));

            var requestMessage = request.Serialize(CompanyID, UseChecksum).ToArray();

            channel.Write(requestMessage);
            var requestLog = new FEnetRequestLog(channel, request, requestMessage);
            channel?.Logger?.Log(requestLog);

            FEnetResponse result;
            List<byte> buffer = new List<byte>();

            try
            {
                if (responseWaitHandles.TryGetValue(request.InvokeID.Value, out var oldHandle))
                    oldHandle.WaitOne(timeout);

                var responseWaitHandle = responseWaitHandles[request.InvokeID.Value] = new ResponseWaitHandle(buffer, request, timeout);

                Task.Run(() => RunReceive(channel));

                responseWaitHandle.WaitOne(timeout);

                result = responseWaitHandle.Response;
                if (result == null)
                {
                    responseWaitHandles.Remove(request.InvokeID.Value);
                    return new FEnetCommErrorResponse(FEnetCommErrorCode.ResponseTimeout, new byte[0], request, 0, 0);
                }
            }
            catch (Exception ex)
            {
                channel?.Logger?.Log(new ChannelErrorLog(channel, ex));
                throw ex;
            }

            if (result is FEnetCommErrorResponse commErrorResponse)
            {
                var ex = new RequestException<FEnetCommErrorCode>(commErrorResponse.ErrorCode, commErrorResponse.ReceivedBytes, commErrorResponse.Request);
                channel?.Logger?.Log(new ChannelErrorLog(channel, ex));
                throw ex;
            }

            if (result is FEnetNAKResponse exceptionResponse)
            {
                channel?.Logger?.Log(new FEnetNAKLog(channel, exceptionResponse, buffer.ToArray(), requestLog));
                if (ThrowsExceptionFromNAK)
                    throw new FEnetNAKException(exceptionResponse.NAKCode, exceptionResponse.NAKCodeValue);
            }
            else
                channel?.Logger?.Log(new FEnetResponseLog(channel, result, result is FEnetNAKResponse ? null : buffer.ToArray(), requestLog));

            return result;
        }







        /// <summary>
        /// 개별 디바이스 변수 읽기
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="moreDeviceVariables">추가 디바이스 변수 목록</param>
        /// <returns>읽은 디바이스 변수/값 Dictionary</returns>
        public IReadOnlyDictionary<DeviceVariable, DeviceValue> Read(DeviceVariable deviceVariable, params DeviceVariable[] moreDeviceVariables)
            => Read(Timeout, deviceVariable, moreDeviceVariables);
        /// <summary>
        /// 개별 디바이스 변수 읽기
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="moreDeviceVariables">추가 디바이스 변수 목록</param>
        /// <returns>읽은 디바이스 변수/값 Dictionary</returns>
        public IReadOnlyDictionary<DeviceVariable, DeviceValue> Read(int timeout, DeviceVariable deviceVariable, params DeviceVariable[] moreDeviceVariables)
        {
            var response = Request(timeout, new FEnetReadIndividualRequest(deviceVariable.DataType, new DeviceVariable[] { deviceVariable }.Concat(moreDeviceVariables)));
            if (response is FEnetReadIndividualResponse readResponse)
                return readResponse;
            else if (response is FEnetNAKResponse exceptionResponse)
                throw new FEnetNAKException(exceptionResponse.NAKCode, exceptionResponse.NAKCodeValue);
            return null;
        }



        /// <summary>
        /// 연속 디바이스 변수 읽기
        /// </summary>
        /// <param name="deviceType">읽기 요청할 디바이스 영역</param>
        /// <param name="index">읽기 요청 시작 디바이스 인덱스</param>
        /// <param name="count">읽을 개수</param>
        /// <returns>읽은 디바이스 변수/값 Dictionary</returns>
        public IReadOnlyList<byte> Read(DeviceType deviceType, uint index, int count) => Read(Timeout, deviceType, index, count);
        /// <summary>
        /// 연속 디바이스 변수 읽기
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="deviceType">읽기 요청할 디바이스 영역</param>
        /// <param name="index">읽기 요청 시작 디바이스 인덱스</param>
        /// <param name="count">읽을 개수</param>
        /// <returns>읽은 디바이스 변수/값 Dictionary</returns>
        public IReadOnlyList<byte> Read(int timeout, DeviceType deviceType, uint index, int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

            var response = Request(timeout, new FEnetReadContinuousRequest(deviceType, index, count));
            if (response is FEnetReadContinuousResponse readResponse)
                return readResponse;
            else if (response is FEnetNAKResponse exceptionResponse)
                throw new FEnetNAKException(exceptionResponse.NAKCode, exceptionResponse.NAKCodeValue);
            return null;
        }



        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="deviceValue">디바이스 변수에 쓸 값</param>
        public void Write(DeviceVariable deviceVariable, DeviceValue deviceValue) => Write(Timeout, deviceVariable, deviceValue);
        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="deviceValue">디바이스 변수에 쓸 값</param>
        public void Write(int timeout, DeviceVariable deviceVariable, DeviceValue deviceValue)
            => Write(timeout, new KeyValuePair<DeviceVariable, DeviceValue>[] { new KeyValuePair<DeviceVariable, DeviceValue>(deviceVariable, deviceValue) });


        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="valueTuple">디바이스 변수에 쓸 값</param>
        /// <param name="moreValueTuples">추가 디바이스 변수에 쓸 값들</param>
        public void Write((DeviceVariable, DeviceValue) valueTuple, params (DeviceVariable, DeviceValue)[] moreValueTuples)
            => Write(Timeout, new (DeviceVariable, DeviceValue)[] { valueTuple }.Concat(moreValueTuples).Select(item => new KeyValuePair<DeviceVariable, DeviceValue>(item.Item1, item.Item2)));
        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="valueTuple">디바이스 변수에 쓸 값</param>
        /// <param name="moreValueTuples">추가 디바이스 변수에 쓸 값들</param>
        public void Write(int timeout, (DeviceVariable, DeviceValue) valueTuple, params (DeviceVariable, DeviceValue)[] moreValueTuples)
            => Write(timeout, new (DeviceVariable, DeviceValue)[] { valueTuple }.Concat(moreValueTuples).Select(item => new KeyValuePair<DeviceVariable, DeviceValue>(item.Item1, item.Item2)));

        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="values">디바이스 변수에 쓸 값들</param>
        public void Write(IEnumerable<KeyValuePair<DeviceVariable, DeviceValue>> values) => Write(Timeout, values);
        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="values">디바이스 변수에 쓸 값들</param>
        public void Write(int timeout, IEnumerable<KeyValuePair<DeviceVariable, DeviceValue>> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (values.Count() == 0) throw new ArgumentOutOfRangeException(nameof(values));

            var response = Request(timeout, new FEnetWriteIndividualRequest(values.First().Key.DataType, values));
            if (response is FEnetNAKResponse exceptionResponse)
                throw new FEnetNAKException(exceptionResponse.NAKCode, exceptionResponse.NAKCodeValue);
        }



        /// <summary>
        /// 연속 디바이스 변수 쓰기
        /// </summary>
        /// <param name="deviceType">읽기 요청 시작 디바이스</param>
        /// <param name="index">읽기 요청 시작 디바이스 인덱스</param>
        /// <param name="deviceValue">쓰기 요청할 디바이스 값</param>
        /// <param name="moreDeviceValues">추가로 쓸 디바이스 값들</param>
        public void Write(DeviceType deviceType, uint index, byte deviceValue, params byte[] moreDeviceValues)
            => Write(Timeout, deviceType, index, deviceValue, moreDeviceValues);
        /// <summary>
        /// 연속 디바이스 변수 쓰기
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="deviceType">읽기 요청 시작 디바이스</param>
        /// <param name="index">읽기 요청 시작 디바이스 인덱스</param>
        /// <param name="deviceValue">쓰기 요청할 디바이스 값</param>
        /// <param name="moreDeviceValues">추가로 쓸 디바이스 값들</param>
        public void Write(int timeout, DeviceType deviceType, uint index, byte deviceValue, params byte[] moreDeviceValues)
            => Write(timeout, deviceType, index, new byte[] { deviceValue }.Concat(moreDeviceValues));
        /// <summary>
        /// 연속 디바이스 변수 쓰기
        /// </summary>
        /// <param name="deviceType">읽기 요청 시작 디바이스</param>
        /// <param name="index">읽기 요청 시작 디바이스 인덱스</param>
        /// <param name="deviceValues">쓰기 요청할 디바이스 값들</param>
        public void Write(DeviceType deviceType, uint index, IEnumerable<byte> deviceValues)
            => Write(Timeout, deviceType, index, deviceValues);
        /// <summary>
        /// 연속 디바이스 변수 쓰기
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="deviceType">읽기 요청 시작 디바이스</param>
        /// <param name="index">읽기 요청 시작 디바이스 인덱스</param>
        /// <param name="deviceValues">쓰기 요청할 디바이스 값들</param>
        public void Write(int timeout, DeviceType deviceType, uint index, IEnumerable<byte> deviceValues)
        {
            var response = Request(timeout, new FEnetWriteContinuousRequest(deviceType, index, deviceValues));
            if (response is FEnetNAKResponse exceptionResponse)
                throw new FEnetNAKException(exceptionResponse.NAKCode, exceptionResponse.NAKCodeValue);
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
                        var buffer = new List<byte>();

                        while (responseWaitHandles.Count > 0)
                        {
                            if (errorBuffer.Count >= 256)
                            {
                                channel?.Logger?.Log(new UnrecognizedErrorLog(channel, errorBuffer.ToArray()));
                                errorBuffer.Clear();
                            }

                            if (buffer.Count < 20)
                                buffer.AddRange(channel.Read((uint)(20 - buffer.Count), 0));

                            if (Encoding.ASCII.GetString(buffer.Take(10).ToArray()).TrimEnd('\0') != CompanyID?.TrimEnd('\0'))
                            {
                                errorBuffer.Add(buffer[0]);
                                buffer.RemoveAt(0);
                                continue;
                            }

                            if (buffer[13] != 0x11)
                            {
                                errorBuffer.AddRange(buffer.Take(14));
                                buffer.RemoveRange(0, 14);
                                continue;
                            }

                            ushort invokeID = (ushort)(buffer[14]| (buffer[15] << 8));
                            if (!responseWaitHandles.TryGetValue(invokeID, out var responseWaitHandle))
                            {
                                errorBuffer.AddRange(buffer.Take(15));
                                buffer.RemoveRange(0, 15);
                                continue;
                            }

                            var plcInfo = (ushort)(buffer[10] | (buffer[11] << 8));
                            var ethernetModuleInfo = buffer[18];

                            var request = responseWaitHandle.Request;

                            if (UseChecksum && buffer[19] != 0 && buffer[19] != buffer.Take(19).Sum(b => b) % 256)
                            {
                                responseWaitHandle.Response = new FEnetCommErrorResponse(FEnetCommErrorCode.ErrorChecksum, buffer, request, plcInfo, ethernetModuleInfo);
                                buffer.Clear();
                                continue;
                            }


                            FEnetResponse result;

                            buffer.AddRange(channel.Read(2, 0));

                            var commandValue = (ushort)((buffer[20] | (buffer[21] << 8)) - 1);
                            if (!Enum.IsDefined(typeof(FEnetCommand), commandValue))
                            {
                                responseWaitHandle.Response = new FEnetCommErrorResponse(FEnetCommErrorCode.ResponseCommandDoNotMatch, buffer, request, plcInfo, ethernetModuleInfo);
                                buffer.Clear();
                                continue;
                            }
                            var command = (FEnetCommand)commandValue;
                            if (request.Command != command)
                            {
                                responseWaitHandle.Response = new FEnetCommErrorResponse(FEnetCommErrorCode.ResponseCommandDoNotMatch, buffer, request, plcInfo, ethernetModuleInfo);
                                buffer.Clear();
                                continue;
                            }

                            buffer.AddRange(channel.Read(2, 0));

                            var dataTypeValue = (ushort)(buffer[22] | (buffer[23] << 8));
                            if (!Enum.IsDefined(typeof(FEnetDataType), dataTypeValue))
                            {
                                responseWaitHandle.Response = new FEnetCommErrorResponse(FEnetCommErrorCode.ResponseDataTypeDoNotMatch, buffer, request, plcInfo, ethernetModuleInfo);
                                buffer.Clear();
                                continue;
                            }
                            var dataType = (FEnetDataType)dataTypeValue;
                            if (request.DataType != dataType)
                            {
                                responseWaitHandle.Response = new FEnetCommErrorResponse(FEnetCommErrorCode.ResponseDataTypeDoNotMatch, buffer, request, plcInfo, ethernetModuleInfo);
                                buffer.Clear();
                                continue;
                            }

                            buffer.AddRange(channel.Read(4, 0));

                            if (buffer[26] != 0 || buffer[27] != 0)
                            {
                                buffer.AddRange(channel.Read(2, 0));
                                result = new FEnetNAKResponse((ushort)(buffer[28] | (buffer[29] << 8)), request, plcInfo, ethernetModuleInfo);
                            }
                            else
                            {
                                buffer.AddRange(channel.Read(2, 0));
                                if (request.BlockCount != (buffer[28] | (buffer[29] << 8)))
                                    result = new FEnetCommErrorResponse(FEnetCommErrorCode.ResponseDataBlockCountDoNotMatch, buffer, request, plcInfo, ethernetModuleInfo);
                                else if (request.Command == FEnetCommand.Write)
                                {
                                    result = new FEnetWriteResponse(request as FEnetWriteRequest, plcInfo, ethernetModuleInfo);
                                }
                                else
                                {
                                    switch (request.DataType)
                                    {
                                        case FEnetDataType.Continuous:
                                            result = DeserializeContinuousDataResponse(channel, buffer, request, plcInfo, ethernetModuleInfo, out var bytes) ?? new FEnetReadContinuousResponse(bytes, request as FEnetReadContinuousRequest, plcInfo, ethernetModuleInfo);
                                            break;
                                        default:
                                            result = DeserializeIndividualDataResponse(channel, buffer, request, plcInfo, ethernetModuleInfo, out var deviceValues) ?? new FEnetReadIndividualResponse(deviceValues, request as FEnetReadIndividualRequest, plcInfo, ethernetModuleInfo);
                                            break;
                                    }
                                }
                            }

                            if (result is FEnetCommErrorResponse responseCommErrorMessage)
                            {
                                buffer.Clear();
                                responseWaitHandle.Response = result;
                                continue;
                            }

                            if (buffer.Count - 20 != (buffer[16] | (buffer[17] << 8)))
                            {
                                responseWaitHandle.Response = new FEnetCommErrorResponse(FEnetCommErrorCode.ResponseLengthDoNotMatch, buffer, request, plcInfo, ethernetModuleInfo);
                                buffer.Clear();
                                continue;
                            }

                            if (errorBuffer.Count > 0)
                            {
                                channel?.Logger?.Log(new UnrecognizedErrorLog(channel, errorBuffer.ToArray()));
                                errorBuffer.Clear();
                            }

                            responseWaitHandle.ResponseBuffer.AddRange(buffer);

                            buffer.Clear();
                            responseWaitHandle.Response = result;
                            responseWaitHandles.Remove(invokeID);
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


        private FEnetResponse DeserializeIndividualDataResponse(Channel channel, List<byte> buffer, FEnetRequest request, ushort plcInfo, byte ethernetModuleInfo, out DeviceValue[] deviceValues)
        {
            var blockCount = request.BlockCount;
            deviceValues = new DeviceValue[blockCount];

            for (int i = 0; i < blockCount; i++)
            {
                var dataCount = FEnetMessage.ReadWord(channel, buffer);

                if (dataCount > 8)
                    return new FEnetCommErrorResponse(FEnetCommErrorCode.ErrorDataCount, buffer, request, plcInfo, ethernetModuleInfo);

                ulong value = 0;
                for (int j = 0; j < dataCount; j++)
                {
                    byte b = channel.Read(0);
                    buffer.Add(b);

                    value |= (ulong)b << (8 * j);
                }

                deviceValues[i] = new DeviceValue(value);
            }

            return null;
        }

        private FEnetResponse DeserializeContinuousDataResponse(Channel channel, List<byte> buffer, FEnetRequest request, ushort plcInfo, byte ethernetModuleInfo, out byte[] bytes)
        {
            var continuousAccessRequest = (IContinuousAccessRequest)request;
            bytes = null;

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

            var dataCount = FEnetMessage.ReadWord(channel, buffer);
            if (dataCount != dataUnit * continuousAccessRequest.Count)
                return new FEnetCommErrorResponse(FEnetCommErrorCode.ResponseDataCountNotMatch, buffer, request, plcInfo, ethernetModuleInfo);

            bytes = channel.Read(dataCount, 0).ToArray();
            buffer.AddRange(bytes);

            return null;
        }


        class FEnetCommErrorResponse : FEnetResponse
        {
            public FEnetCommErrorResponse(FEnetCommErrorCode errorCode, IEnumerable<byte> receivedMessage, FEnetRequest request, ushort plcInfo, byte ethernetModuleInfo) : base(request, plcInfo, ethernetModuleInfo)
            {
                ErrorCode = errorCode;
                ReceivedBytes = receivedMessage?.ToArray() ?? new byte[0];
            }

            public FEnetCommErrorCode ErrorCode { get; }
            public IReadOnlyList<byte> ReceivedBytes { get; }

            public override IEnumerable<byte> Serialize(string companyID, bool useChecksum)
            {
                return ReceivedBytes;
            }

            protected override IEnumerable<byte> OnCreateDataFrame()
            {
                yield break;
            }

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
