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
        /// 기본적으로 BCC를 사용할 것인지 여부, 기본값은 true
        /// </summary>
        public bool DefaultUseBCC { get; set; } = true;

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
        public CnetResponse Request(CnetRequest request) => Request(DefaultUseBCC, Timeout, request);

        /// <summary>
        /// Cnet 요청하기
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="request">Cnet 요청</param>
        /// <returns>Cnet 응답</returns>
        public CnetResponse Request(bool useBCC, CnetRequest request) => Request(useBCC, Timeout, request);

        /// <summary>
        /// Cnet 요청하기
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="request">Cnet 요청</param>
        /// <returns>Cnet 응답</returns>
        public CnetResponse Request(int timeout, CnetRequest request) => Request(DefaultUseBCC, timeout, request);

        /// <summary>
        /// Cnet 요청하기
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="request">Cnet 요청</param>
        /// <returns>Cnet 응답</returns>
        public CnetResponse Request(bool useBCC, int timeout, CnetRequest request)
        {
            request = (CnetRequest)request.Clone();

            Channel channel = (Channel as Channel) ?? (Channel as ChannelProvider)?.PrimaryChannel;

            if (channel == null)
                throw new ArgumentNullException(nameof(Channel));

            CnetRequestLog requestLog;
            CnetResponse result;
            List<byte> buffer = new List<byte>();
            List<byte> errorBuffer = new List<byte>();

            try
            {
                var requestMessage = request.Serialize(useBCC).ToArray();

                channel.Write(requestMessage);
                requestLog = new CnetRequestLog(channel, request, requestMessage);
                channel?.Logger?.Log(requestLog);

                result = DeserializeResponse(channel, buffer, request, useBCC, timeout);

                while (result is CnetCommErrorResponse responseCommErrorMessage
                    && responseCommErrorMessage.ErrorCode != CnetCommErrorCode.ResponseTimeout)
                {
                    errorBuffer.Add(buffer[0]);
                    buffer.RemoveAt(0);
                    result = DeserializeResponse(channel, buffer, request, useBCC, timeout);
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
                    throw new CnetNAKException(exceptionResponse.NAKCode, exceptionResponse.NAKCodeValue);
            }
            else
                channel?.Logger?.Log(new CnetResponseLog(channel, result, result is CnetNAKResponse ? null : buffer.ToArray(), requestLog));

            return result;
        }



        /// <summary>
        /// 개별 디바이스 변수 읽기
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="moreDeviceVariables">추가 디바이스 변수 목록</param>
        /// <returns>읽은 디바이스 변수/값 Dictionary</returns>
        public IReadOnlyDictionary<DeviceVariable, DeviceValue> Read(byte stationNumber, DeviceVariable deviceVariable, params DeviceVariable[] moreDeviceVariables)
            => Read(DefaultUseBCC, Timeout, stationNumber, deviceVariable, moreDeviceVariables);
        /// <summary>
        /// 개별 디바이스 변수 읽기
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="moreDeviceVariables">추가 디바이스 변수 목록</param>
        /// <returns>읽은 디바이스 변수/값 Dictionary</returns>
        public IReadOnlyDictionary<DeviceVariable, DeviceValue> Read(bool useBCC, byte stationNumber, DeviceVariable deviceVariable, params DeviceVariable[] moreDeviceVariables)
            => Read(useBCC, Timeout, stationNumber, deviceVariable, moreDeviceVariables);
        /// <summary>
        /// 개별 디바이스 변수 읽기
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="moreDeviceVariables">추가 디바이스 변수 목록</param>
        /// <returns>읽은 디바이스 변수/값 Dictionary</returns>
        public IReadOnlyDictionary<DeviceVariable, DeviceValue> Read(int timeout, byte stationNumber, DeviceVariable deviceVariable, params DeviceVariable[] moreDeviceVariables)
            => Read(DefaultUseBCC, timeout, stationNumber, deviceVariable, moreDeviceVariables);
        /// <summary>
        /// 개별 디바이스 변수 읽기
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="moreDeviceVariables">추가 디바이스 변수 목록</param>
        /// <returns>읽은 디바이스 변수/값 Dictionary</returns>
        public IReadOnlyDictionary<DeviceVariable, DeviceValue> Read(bool useBCC, int timeout, byte stationNumber, DeviceVariable deviceVariable, params DeviceVariable[] moreDeviceVariables)
        {
            var response = Request(useBCC, timeout, new CnetReadIndividualRequest(stationNumber, new DeviceVariable[] { deviceVariable }.Concat(moreDeviceVariables)));
            if (response is CnetReadResponse readResponse)
                return readResponse;
            else if (response is CnetNAKResponse exceptionResponse)
                throw new CnetNAKException(exceptionResponse.NAKCode, exceptionResponse.NAKCodeValue);
            return null;
        }


        /// <summary>
        /// 연속 디바이스 변수 읽기
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="count">읽을 개수</param>
        /// <returns>읽은 디바이스 변수/값 Dictionary</returns>
        public IReadOnlyDictionary<DeviceVariable, DeviceValue> Read(byte stationNumber, DeviceVariable startDeviceVariable, int count)
            => Read(DefaultUseBCC, Timeout, stationNumber, startDeviceVariable, count);
        /// <summary>
        /// 연속 디바이스 변수 읽기
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="count">읽을 개수</param>
        /// <returns>읽은 디바이스 변수/값 Dictionary</returns>
        public IReadOnlyDictionary<DeviceVariable, DeviceValue> Read(bool useBCC, byte stationNumber, DeviceVariable startDeviceVariable, int count)
            => Read(useBCC, Timeout, stationNumber, startDeviceVariable, count);
        /// <summary>
        /// 연속 디바이스 변수 읽기
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="count">읽을 개수</param>
        /// <returns>읽은 디바이스 변수/값 Dictionary</returns>
        public IReadOnlyDictionary<DeviceVariable, DeviceValue> Read(int timeout, byte stationNumber, DeviceVariable startDeviceVariable, int count)
            => Read(DefaultUseBCC, timeout, stationNumber, startDeviceVariable, count);
        /// <summary>
        /// 연속 디바이스 변수 읽기
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="count">읽을 개수</param>
        /// <returns>읽은 디바이스 변수/값 Dictionary</returns>
        public IReadOnlyDictionary<DeviceVariable, DeviceValue> Read(bool useBCC, int timeout, byte stationNumber, DeviceVariable startDeviceVariable, int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

            var response = Request(useBCC, timeout, new CnetReadContinuousRequest(stationNumber, startDeviceVariable, count));
            if (response is CnetReadResponse readResponse)
                return readResponse;
            else if (response is CnetNAKResponse exceptionResponse)
                throw new CnetNAKException(exceptionResponse.NAKCode, exceptionResponse.NAKCodeValue);
            return null;
        }


        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="deviceValue">디바이스 변수에 쓸 값</param>
        public void Write(byte stationNumber, DeviceVariable deviceVariable, DeviceValue deviceValue)
            => Write(DefaultUseBCC, Timeout, stationNumber, deviceVariable, deviceValue);
        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="deviceValue">디바이스 변수에 쓸 값</param>
        public void Write(bool useBCC, byte stationNumber, DeviceVariable deviceVariable, DeviceValue deviceValue)
            => Write(useBCC, Timeout, stationNumber, deviceVariable, deviceValue);
        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="deviceValue">디바이스 변수에 쓸 값</param>
        public void Write(int timeout, byte stationNumber, DeviceVariable deviceVariable, DeviceValue deviceValue)
            => Write(DefaultUseBCC, timeout, stationNumber, deviceVariable, deviceValue);
        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="deviceValue">디바이스 변수에 쓸 값</param>
        public void Write(bool useBCC, int timeout, byte stationNumber, DeviceVariable deviceVariable, DeviceValue deviceValue)
            => Write(useBCC, timeout, stationNumber, new KeyValuePair<DeviceVariable, DeviceValue>[] { new KeyValuePair<DeviceVariable, DeviceValue>(deviceVariable, deviceValue) });


        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="valueTuple">디바이스 변수에 쓸 값</param>
        /// <param name="moreValueTuples">추가 디바이스 변수에 쓸 값들</param>
        public void Write(byte stationNumber, (DeviceVariable, DeviceValue) valueTuple, params (DeviceVariable, DeviceValue)[] moreValueTuples)
            => Write(DefaultUseBCC, Timeout, stationNumber, valueTuple, moreValueTuples);
        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="valueTuple">디바이스 변수에 쓸 값</param>
        /// <param name="moreValueTuples">추가 디바이스 변수에 쓸 값들</param>
        public void Write(bool useBCC, byte stationNumber, (DeviceVariable, DeviceValue) valueTuple, params (DeviceVariable, DeviceValue)[] moreValueTuples)
            => Write(useBCC, Timeout, stationNumber, valueTuple, moreValueTuples);
        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="valueTuple">디바이스 변수에 쓸 값</param>
        /// <param name="moreValueTuples">추가 디바이스 변수에 쓸 값들</param>
        public void Write(int timeout, byte stationNumber, (DeviceVariable, DeviceValue) valueTuple, params (DeviceVariable, DeviceValue)[] moreValueTuples)
            => Write(DefaultUseBCC, timeout, stationNumber, valueTuple, moreValueTuples);
        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="valueTuple">디바이스 변수에 쓸 값</param>
        /// <param name="moreValueTuples">추가 디바이스 변수에 쓸 값들</param>
        public void Write(bool useBCC, int timeout, byte stationNumber, (DeviceVariable, DeviceValue) valueTuple, params (DeviceVariable, DeviceValue)[] moreValueTuples)
            => Write(useBCC, timeout, stationNumber, new (DeviceVariable, DeviceValue)[] { valueTuple }.Concat(moreValueTuples).Select(item => new KeyValuePair<DeviceVariable, DeviceValue>(item.Item1, item.Item2)));

        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="values">디바이스 변수에 쓸 값들</param>
        public void Write(byte stationNumber, IEnumerable<KeyValuePair<DeviceVariable, DeviceValue>> values)
            => Write(DefaultUseBCC, Timeout, stationNumber, values);
        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="values">디바이스 변수에 쓸 값들</param>
        public void Write(bool useBCC, byte stationNumber, IEnumerable<KeyValuePair<DeviceVariable, DeviceValue>> values)
            => Write(useBCC, Timeout, stationNumber, values);
        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="values">디바이스 변수에 쓸 값들</param>
        public void Write(int timeout, byte stationNumber, IEnumerable<KeyValuePair<DeviceVariable, DeviceValue>> values)
            => Write(DefaultUseBCC, timeout, stationNumber, values);
        /// <summary>
        /// 개별 디바이스 변수 쓰기
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="values">디바이스 변수에 쓸 값들</param>
        public void Write(bool useBCC, int timeout, byte stationNumber, IEnumerable<KeyValuePair<DeviceVariable, DeviceValue>> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (values.Count() == 0) throw new ArgumentOutOfRangeException(nameof(values));

            var response = Request(useBCC, timeout, new CnetWriteIndividualRequest(stationNumber, values));
            if (response is CnetNAKResponse exceptionResponse)
                throw new CnetNAKException(exceptionResponse.NAKCode, exceptionResponse.NAKCodeValue);
        }


        /// <summary>
        /// 연속 디바이스 변수 쓰기
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="deviceValue">쓰기 요청할 디바이스 값</param>
        /// <param name="moreDeviceValues">추가로 쓸 디바이스 값들</param>
        public void Write(byte stationNumber, DeviceVariable startDeviceVariable, DeviceValue deviceValue, params DeviceValue[] moreDeviceValues)
            => Write(DefaultUseBCC, Timeout, stationNumber, startDeviceVariable, deviceValue, moreDeviceValues);
        /// <summary>
        /// 연속 디바이스 변수 쓰기
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="deviceValue">쓰기 요청할 디바이스 값</param>
        /// <param name="moreDeviceValues">추가로 쓸 디바이스 값들</param>
        public void Write(bool useBCC, byte stationNumber, DeviceVariable startDeviceVariable, DeviceValue deviceValue, params DeviceValue[] moreDeviceValues)
            => Write(useBCC, Timeout, stationNumber, startDeviceVariable, deviceValue, moreDeviceValues);
        /// <summary>
        /// 연속 디바이스 변수 쓰기
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="deviceValue">쓰기 요청할 디바이스 값</param>
        /// <param name="moreDeviceValues">추가로 쓸 디바이스 값들</param>
        public void Write(int timeout, byte stationNumber, DeviceVariable startDeviceVariable, DeviceValue deviceValue, params DeviceValue[] moreDeviceValues)
            => Write(DefaultUseBCC, timeout, stationNumber, startDeviceVariable, deviceValue, moreDeviceValues);
        /// <summary>
        /// 연속 디바이스 변수 쓰기
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="deviceValue">쓰기 요청할 디바이스 값</param>
        /// <param name="moreDeviceValues">추가로 쓸 디바이스 값들</param>
        public void Write(bool useBCC, int timeout, byte stationNumber, DeviceVariable startDeviceVariable, DeviceValue deviceValue, params DeviceValue[] moreDeviceValues)
        {
            var response = Request(useBCC, timeout, new CnetWriteContinuousRequest(stationNumber, startDeviceVariable, new DeviceValue[] { deviceValue }.Concat(moreDeviceValues)));
            if (response is CnetNAKResponse exceptionResponse)
                throw new CnetNAKException(exceptionResponse.NAKCode, exceptionResponse.NAKCodeValue);
        }



        /// <summary>
        /// 직접변수 개별 읽기 모니터 등록
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="moreDeviceVariables">추가로 읽을 디바이스 변수 목록</param>
        /// <returns>모니터 실행 요청</returns>
        public CnetExecuteMonitorRequest RegisterMonitor(byte stationNumber, byte monitorNumber, DeviceVariable deviceVariable, params DeviceVariable[] moreDeviceVariables)
            => RegisterMonitor(DefaultUseBCC, Timeout, stationNumber, monitorNumber, deviceVariable, moreDeviceVariables);
        /// <summary>
        /// 직접변수 개별 읽기 모니터 등록
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="moreDeviceVariables">추가로 읽을 디바이스 변수 목록</param>
        /// <returns>모니터 실행 요청</returns>
        public CnetExecuteMonitorRequest RegisterMonitor(bool useBCC, byte stationNumber, byte monitorNumber, DeviceVariable deviceVariable, params DeviceVariable[] moreDeviceVariables)
            => RegisterMonitor(useBCC, Timeout, stationNumber, monitorNumber, deviceVariable, moreDeviceVariables);
        /// <summary>
        /// 직접변수 개별 읽기 모니터 등록
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="moreDeviceVariables">추가로 읽을 디바이스 변수 목록</param>
        /// <returns>모니터 실행 요청</returns>
        public CnetExecuteMonitorRequest RegisterMonitor(int timeout, byte stationNumber, byte monitorNumber, DeviceVariable deviceVariable, params DeviceVariable[] moreDeviceVariables)
            => RegisterMonitor(DefaultUseBCC, timeout, stationNumber, monitorNumber, deviceVariable, moreDeviceVariables);
        /// <summary>
        /// 직접변수 개별 읽기 모니터 등록
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="moreDeviceVariables">추가로 읽을 디바이스 변수 목록</param>
        /// <returns>모니터 실행 요청</returns>
        public CnetExecuteMonitorRequest RegisterMonitor(bool useBCC, int timeout, byte stationNumber, byte monitorNumber, DeviceVariable deviceVariable, params DeviceVariable[] moreDeviceVariables)
        {
            var monitor = new CnetMonitorByIndividualAccess(stationNumber, monitorNumber, deviceVariable, moreDeviceVariables);

            var response = Request(useBCC, timeout, monitor.CreateRegisterRequest());
            if (response is CnetNAKResponse exceptionResponse)
                throw new CnetNAKException(exceptionResponse.NAKCode, exceptionResponse.NAKCodeValue);

            return monitor.CreateExecuteRequest();
        }


        /// <summary>
        /// 직접변수 연속 읽기 모니터 등록
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="count">읽을 개수</param>
        /// <returns>모니터 실행 요청</returns>
        public CnetExecuteMonitorRequest RegisterMonitor(byte stationNumber, byte monitorNumber, DeviceVariable startDeviceVariable, int count)
            => RegisterMonitor(DefaultUseBCC, Timeout, stationNumber, monitorNumber, startDeviceVariable, count);
        /// <summary>
        /// 직접변수 연속 읽기 모니터 등록
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="count">읽을 개수</param>
        /// <returns>모니터 실행 요청</returns>
        public CnetExecuteMonitorRequest RegisterMonitor(bool useBCC, byte stationNumber, byte monitorNumber, DeviceVariable startDeviceVariable, int count)
            => RegisterMonitor(useBCC, Timeout, stationNumber, monitorNumber, startDeviceVariable, count);
        /// <summary>
        /// 직접변수 연속 읽기 모니터 등록
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="count">읽을 개수</param>
        /// <returns>모니터 실행 요청</returns>
        public CnetExecuteMonitorRequest RegisterMonitor(int timeout, byte stationNumber, byte monitorNumber, DeviceVariable startDeviceVariable, int count)
            => RegisterMonitor(DefaultUseBCC, timeout, stationNumber, monitorNumber, startDeviceVariable, count);
        /// <summary>
        /// 직접변수 연속 읽기 모니터 등록
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="count">읽을 개수</param>
        /// <returns>모니터 실행 요청</returns>
        public CnetExecuteMonitorRequest RegisterMonitor(bool useBCC, int timeout, byte stationNumber, byte monitorNumber, DeviceVariable startDeviceVariable, int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

            var monitor = new CnetMonitorByContinuousAccess(stationNumber, monitorNumber, startDeviceVariable, count);

            var response = Request(useBCC, timeout, monitor.CreateRegisterRequest());
            if (response is CnetNAKResponse exceptionResponse)
                throw new CnetNAKException(exceptionResponse.NAKCode, exceptionResponse.NAKCodeValue);

            return monitor.CreateExecuteRequest();
        }


        /// <summary>
        /// 모니터 변수 읽기
        /// </summary>
        /// <param name="executeMonitorRequest">모니터 실행 요청</param>
        /// <returns>읽은 디바이스 변수/값 Dictionary</returns>
        public IReadOnlyDictionary<DeviceVariable, DeviceValue> Read(CnetExecuteMonitorRequest executeMonitorRequest)
            => Read(DefaultUseBCC, Timeout, executeMonitorRequest);
        /// <summary>
        /// 모니터 변수 읽기
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="executeMonitorRequest">모니터 실행 요청</param>
        /// <returns>읽은 디바이스 변수/값 Dictionary</returns>
        public IReadOnlyDictionary<DeviceVariable, DeviceValue> Read(bool useBCC, CnetExecuteMonitorRequest executeMonitorRequest)
            => Read(useBCC, Timeout, executeMonitorRequest);
        /// <summary>
        /// 모니터 변수 읽기
        /// </summary>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="executeMonitorRequest">모니터 실행 요청</param>
        /// <returns>읽은 디바이스 변수/값 Dictionary</returns>
        public IReadOnlyDictionary<DeviceVariable, DeviceValue> Read(int timeout, CnetExecuteMonitorRequest executeMonitorRequest)
            => Read(DefaultUseBCC, timeout, executeMonitorRequest);
        /// <summary>
        /// 모니터 변수 읽기
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <param name="executeMonitorRequest">모니터 실행 요청</param>
        /// <returns>읽은 디바이스 변수/값 Dictionary</returns>
        public IReadOnlyDictionary<DeviceVariable, DeviceValue> Read(bool useBCC, int timeout, CnetExecuteMonitorRequest executeMonitorRequest)
        {
            if (executeMonitorRequest == null) throw new ArgumentOutOfRangeException(nameof(executeMonitorRequest));

            var response = Request(useBCC, timeout, executeMonitorRequest);
            if (response is CnetReadResponse readResponse)
                return readResponse;
            else if (response is CnetNAKResponse exceptionResponse)
                throw new CnetNAKException(exceptionResponse.NAKCode, exceptionResponse.NAKCodeValue);
            return null;
        }



        private CnetResponse DeserializeResponse(Channel channel, List<byte> buffer, CnetRequest request, bool useBCC, int timeout)
        {
            buffer.Add(channel.Read(timeout));
            if (buffer[0] != CnetMessage.ACK && buffer[0] != CnetMessage.NAK)
                return new CnetCommErrorResponse(CnetCommErrorCode.ResponseHeaderError, buffer, request);

            var requestMessage = request.Serialize(useBCC).ToArray();

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

                var tailErrorResponse = DeserializeTail(channel, buffer, request, useBCC, timeout);
                if (tailErrorResponse != null) return tailErrorResponse;

                if (!CnetMessage.TryParseWord(buffer, 6, out var errorCode))
                    return new CnetCommErrorResponse(CnetCommErrorCode.ResponseParseHexError, buffer, request);

                return new CnetNAKResponse(errorCode, request);
            }

            if (request.Command == CnetCommand.Write
                || request.Command == CnetCommand.RegisterMonitor)
                return DeserializeTail(channel, buffer, request, useBCC, timeout) ?? new CnetACKResponse(request);

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
                                return DeserializeIndividualDataResponse(channel, buffer, request, useBCC, timeout, out deviceValues) ?? new CnetReadResponse(deviceValues, request as CnetReadIndividualRequest);
                            case CnetCommand.ExecuteMonitor:
                                return DeserializeIndividualDataResponse(channel, buffer, request, useBCC, timeout, out deviceValues) ?? new CnetReadResponse(deviceValues, request as CnetExecuteMonitorRequest);
                        }
                        break;
                    case CnetCommandType.Continuous:
                        switch (request.Command)
                        {
                            case CnetCommand.Read:
                                return DeserializeContinuousDataResponse(channel, buffer, request, useBCC, timeout, out bytes) ?? new CnetReadResponse(bytes, request as CnetReadContinuousRequest);
                            case CnetCommand.ExecuteMonitor:
                                return DeserializeContinuousDataResponse(channel, buffer, request, useBCC, timeout, out bytes) ?? new CnetReadResponse(bytes, request as CnetExecuteMonitorContinuousRequest);
                        }
                        break;
                }
            }

            throw new NotImplementedException();
        }

        private CnetResponse DeserializeIndividualDataResponse(Channel channel, List<byte> buffer, CnetRequest request, bool useBCC, int timeout, out List<DeviceValue> deviceValues)
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

                if (dataCount > 8)
                    return new CnetCommErrorResponse(CnetCommErrorCode.ErrorDataCount, buffer, request);

                ulong value = 0;
                for (int j = dataCount - 1; j >= 0; j--)
                {
                    buffer.AddRange(channel.Read(2, timeout));
                    if (!CnetMessage.TryParseByte(buffer, buffer.Count - 2, out byte b))
                        return new CnetCommErrorResponse(CnetCommErrorCode.ResponseParseHexError, buffer, request);

                    value |= (ulong)b << (8 * j);
                }

                deviceValues.Add(new DeviceValue(value));
            }

            return DeserializeTail(channel, buffer, request, useBCC, timeout);
        }

        private CnetResponse DeserializeContinuousDataResponse(Channel channel, List<byte> buffer, CnetRequest request, bool useBCC, int timeout, out List<byte> bytes)
        {
            var continuousAccessRequest = (IContinuousAccessRequest)request;
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
                return new CnetCommErrorResponse(CnetCommErrorCode.ResponseDataCountNotMatch, buffer, request);

            buffer.AddRange(channel.Read((uint)dataCount * 2, timeout));

            var tailErrorResponse = DeserializeTail(channel, buffer, request, useBCC, timeout);
            if (tailErrorResponse != null) return tailErrorResponse;

            for (int i = 0; i < dataCount; i++)
            {
                if (!CnetMessage.TryParseByte(buffer, 8 + i * 2, out var value))
                    return new CnetCommErrorResponse(CnetCommErrorCode.ResponseParseHexError, buffer, request);
                bytes.Add(value);
            }

            return null;
        }

        private CnetResponse DeserializeTail(Channel channel, List<byte> buffer, CnetRequest request, bool useBCC, int timeout)
        {
            buffer.Add(channel.Read(timeout));
            if (buffer[buffer.Count - 1] != CnetMessage.ETX)
                return new CnetCommErrorResponse(CnetCommErrorCode.ResponseTailError, buffer, request);

            if (useBCC)
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

            public override IEnumerable<byte> Serialize(bool useBCC)
            {
                return ReceivedBytes;
            }

            protected override void OnCreateFrame(List<byte> byteList, bool useBCC)
            {
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