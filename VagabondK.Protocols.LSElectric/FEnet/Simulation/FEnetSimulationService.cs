using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;

namespace VagabondK.Protocols.LSElectric.FEnet.Simulation
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) FEnet 프로토콜 시뮬레이터 서비스입니다.
    /// FEnet 클라이언트를 테스트하는 용도로 사용 가능합니다.
    /// </summary>
    public class FEnetSimulationService : IDisposable
    {
        /// <summary>
        /// 생성자
        /// </summary>
        public FEnetSimulationService() { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        public FEnetSimulationService(IChannel channel)
        {
            Channel = channel;
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            lock (channelTasks)
            {
                if (channel is ChannelProvider channelProvider)
                    channelProvider.Created -= OnChannelCreated;

                foreach (var task in channelTasks.Values)
                {
                    task.Stop();
                }
                channel.Dispose();
            }
        }


        private IChannel channel;
        private readonly Dictionary<Channel, ChannelTask> channelTasks = new Dictionary<Channel, ChannelTask>();

        /// <summary>
        /// 개별 읽기 요청 이벤트
        /// </summary>
        public event EventHandler<FEnetRequestedReadIndividualEventArgs> RequestedReadIndividual;

        /// <summary>
        /// 연속 읽기 요청 이벤트
        /// </summary>
        public event EventHandler<FEnetRequestedReadContinuousEventArgs> RequestedReadContinuous;

        /// <summary>
        /// 개별 쓰기 요청 이벤트
        /// </summary>
        public event EventHandler<FEnetRequestedWriteIndividualEventArgs> RequestedWriteIndividual;

        /// <summary>
        /// 연속 쓰기 요청 이벤트
        /// </summary>
        public event EventHandler<FEnetRequestedWriteContinuousEventArgs> RequestedWriteContinuous;

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
                    lock (channelTasks)
                    {
                        if (channel is Channel oldChannel)
                        {
                            if (channelTasks.TryGetValue(oldChannel, out var channelTask))
                            {
                                channelTask.Stop();
                                channelTasks.Remove(oldChannel);
                            }
                        }
                        else if (channel is ChannelProvider channelProvider)
                        {
                            channelProvider.Created -= OnChannelCreated;
                        }

                        channel = value;

                        if (channel is Channel newChannel)
                        {
                            var channelTask = new ChannelTask(this, newChannel, false);
                            channelTasks[newChannel] = channelTask;
                            channelTask.Start();
                        }
                        else if (channel is ChannelProvider channelProvider)
                        {
                            channelProvider.Created += OnChannelCreated;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 채널 유지 제한시간(밀리세컨드 단위). 이 시간 동안 요청이 발생하지 않으면 채널을 닫습니다. 기본값은 10000(10초)이고, 0이면 채널을 항상 유지합니다.
        /// </summary>
        public int ChannelTimeout { get; set; } = 10000;

        /// <summary>
        /// 메시지 헤더의 CompanyID. 기본값은 LSIS-XGT. 단종된 모델에서는 LGIS-GLOFA를 사용할 수도 있음.
        /// </summary>
        public string CompanyID { get; set; } = "LSIS-XGT";

        /// <summary>
        /// 기본적으로 체크섬을 사용할 것인지 여부, 기본값은 true
        /// </summary>
        public bool UseChecksum { get; set; } = true;

        /// <summary>
        /// PLC의 CPU 타입 및 상태 정보. 자세한 내용은 매뉴얼 참조 바람.
        /// </summary>
        public ushort PlcInfo { get; set; }

        /// <summary>
        /// 이더넷 모듈의 슬롯(Slot) 번호.
        /// </summary>
        public byte EthernetModuleSlot { get; set; } = 1;
        /// <summary>
        /// 이더넷 모듈의 베이스(Base) 번호.
        /// </summary>
        public byte EthernetModuleBase { get; set; } = 1;

        /// <summary>
        /// 비트 변수의 인덱스를 16진수로 통신할지 여부를 결정합니다.
        /// P, M, L, K, F 이면서 Bit일 경우 문자열 오른쪽 끝을 16진수로 인식하고 그 이외의 자리수는 워드 단위 인덱스로 해석합니다.
        /// 그 외에는 인덱스가 .으로 나누어져있고 Bit일 경우 마지막 자리만 16진수로 인식합니다.
        /// </summary>
        public bool UseHexBitIndex { get; set; }

        private byte EthernetModuleInfo => (byte)((EthernetModuleBase << 4) | (EthernetModuleSlot & 0xF));

        private void OnChannelCreated(object sender, ChannelCreatedEventArgs e)
        {
            lock (channelTasks)
            {
                var channelTask = new ChannelTask(this, e.Channel, true);
                channelTasks[e.Channel] = channelTask;
                channelTask.Start();
            }
        }

        private FEnetResponse OnRequestedRead(FEnetReadRequest request, Channel channel)
        {
            FEnetResponse response = null;

            switch (request.DataType)
            {
                case FEnetDataType.Continuous:
                    var eventArgsContinuous = new FEnetRequestedReadContinuousEventArgs((FEnetReadContinuousRequest)request, channel);
                    RequestedReadContinuous?.Invoke(this, eventArgsContinuous);
                    response = eventArgsContinuous.NAKCode != FEnetNAKCode.Unknown
                        ? (FEnetResponse)new FEnetNAKResponse(eventArgsContinuous.NAKCode, request, PlcInfo, EthernetModuleInfo)
                        : new FEnetReadContinuousResponse(eventArgsContinuous.ResponseValues, (FEnetReadContinuousRequest)request, PlcInfo, EthernetModuleInfo);
                    break;
                default:
                    var eventArgsIndividual = new FEnetRequestedReadIndividualEventArgs((FEnetReadIndividualRequest)request, channel);
                    RequestedReadIndividual?.Invoke(this, eventArgsIndividual);
                    response = eventArgsIndividual.NAKCode != FEnetNAKCode.Unknown
                        ? (FEnetResponse)new FEnetNAKResponse(eventArgsIndividual.NAKCode, request, PlcInfo, EthernetModuleInfo)
                        : new FEnetReadIndividualResponse(eventArgsIndividual.ResponseValues.Select(v => v.DeviceValue), (FEnetReadIndividualRequest)request, PlcInfo, EthernetModuleInfo);
                    break;
            }

            return response;
        }

        private FEnetResponse OnRequestedWrite(FEnetWriteRequest request, Channel channel)
        {
            FEnetResponse response;

            switch (request.DataType)
            {
                case FEnetDataType.Continuous:
                    var eventArgsContinuous = new FEnetRequestedWriteContinuousEventArgs((FEnetWriteContinuousRequest)request, channel);
                    RequestedWriteContinuous?.Invoke(this, eventArgsContinuous);
                    response = eventArgsContinuous.NAKCode != FEnetNAKCode.Unknown
                        ? (FEnetResponse)new FEnetNAKResponse(eventArgsContinuous.NAKCode, request, PlcInfo, EthernetModuleInfo)
                        : new FEnetWriteResponse(request, PlcInfo, EthernetModuleInfo);
                    break;
                default:
                    var eventArgsIndividual = new FEnetRequestedWriteIndividualEventArgs((FEnetWriteIndividualRequest)request, channel);
                    RequestedWriteIndividual?.Invoke(this, eventArgsIndividual);
                    response = eventArgsIndividual.NAKCode != FEnetNAKCode.Unknown
                        ? (FEnetResponse)new FEnetNAKResponse(eventArgsIndividual.NAKCode, request, PlcInfo, EthernetModuleInfo)
                        : new FEnetWriteResponse(request, PlcInfo, EthernetModuleInfo);
                    break;
            }

            return response;
        }

        private void DeserializeRequest(Channel channel, List<byte> buffer, List<byte> errorBuffer)
        {
            FEnetRequest request = null;

            if (errorBuffer.Count >= 256)
            {
                channel?.Logger?.Log(new UnrecognizedErrorLog(channel, errorBuffer.ToArray()));
                errorBuffer.Clear();
            }

            if (buffer.Count < 28)
                buffer.AddRange(channel.Read((uint)(28 - buffer.Count), 0));

            if (Encoding.ASCII.GetString(buffer.Take(10).ToArray()).TrimEnd('\0') != CompanyID?.TrimEnd('\0'))
            {
                errorBuffer.Add(buffer[0]);
                buffer.RemoveAt(0);
                return;
            }

            if (buffer[13] != 0x33)
            {
                errorBuffer.AddRange(buffer.Take(14));
                buffer.RemoveRange(0, 14);
                return;
            }

            var commandValue = (ushort)(buffer[20] | (buffer[21] << 8));
            if (!Enum.IsDefined(typeof(FEnetCommand), commandValue))
            {
                errorBuffer.AddRange(buffer.Take(21));
                buffer.RemoveRange(0, 21);
                return;
            }

            var dataTypeValue = (ushort)(buffer[22] | (buffer[23] << 8));
            if (!Enum.IsDefined(typeof(FEnetDataType), dataTypeValue))
            {
                errorBuffer.AddRange(buffer.Take(23));
                buffer.RemoveRange(0, 23);
                return;
            }

            ushort invokeID = (ushort)(buffer[14] | (buffer[15] << 8));
            var command = (FEnetCommand)commandValue;
            var dataType = (FEnetDataType)dataTypeValue;
            var blockCount = (ushort)(buffer[26] | (buffer[27] << 8));

            try
            {
                if (UseChecksum && buffer[19] != 0 && buffer[19] != buffer.Take(19).Sum(b => b) % 256)
                    throw new FEnetNAKException(FEnetNAKCode.ErrorChacksum);

                if (blockCount > 16)
                    throw new FEnetNAKException(FEnetNAKCode.OverRequestReadBlockCount);

                switch (command)
                {
                    case FEnetCommand.Read:
                        request = DeserializeReadRequest(channel, FEnetMessage.ToDataType(dataType), buffer, blockCount);
                        break;
                    case FEnetCommand.Write:
                        request = DeserializeWriteRequest(channel, FEnetMessage.ToDataType(dataType), buffer, blockCount);
                        break;
                }
            }
            catch (FEnetNAKException ex)
            {
                channel.Logger.Log(new UnrecognizedErrorLog(channel, buffer.ToArray()));

                var response = new FEnetNAKResponse(ex.Code, command, dataType, PlcInfo, EthernetModuleInfo) { InvokeID = invokeID };
                var message = response.Serialize(CompanyID, UseChecksum).ToArray();
                channel.Write(message);
                channel.Logger.Log(new FEnetNAKLog(channel, response, message, null));
            }

            if (request != null)
            {
                var requestLog = new FEnetRequestLog(channel, request, buffer.ToArray());
                channel.Logger?.Log(requestLog);
                buffer.Clear();

                FEnetResponse response = null;
                try
                {
                    switch (request.Command)
                    {
                        case FEnetCommand.Read:
                            response = OnRequestedRead((FEnetReadRequest)request, channel);
                            break;
                        case FEnetCommand.Write:
                            response = OnRequestedWrite((FEnetWriteRequest)request, channel);
                            break;
                    }

                    if (response != null)
                    {
                        response.InvokeID = invokeID;
                        var responseMessage = response.Serialize(CompanyID, UseChecksum).ToArray();
                        channel.Logger.Log(new FEnetResponseLog(channel, response, responseMessage, requestLog));
                        channel.Write(responseMessage);
                    }
                }
                catch (FEnetNAKException ex)
                {
                    var nakResponse = new FEnetNAKResponse(ex.Code, request, PlcInfo, EthernetModuleInfo) { InvokeID = invokeID };
                    var message = nakResponse.Serialize(CompanyID, UseChecksum).ToArray();
                    channel.Write(message);
                    channel.Logger.Log(new FEnetNAKLog(channel, nakResponse, message, requestLog));
                }
            }
            else
            {
                channel.Logger.Log(new UnrecognizedErrorLog(channel, buffer.ToArray()));
            }
            buffer.Clear();
            errorBuffer.Clear();
        }

        private DeviceVariable? DeserializeDeviceVariable(Channel channel, List<byte> buffer)
        {
            var byteCount = FEnetMessage.ReadWord(channel, buffer);

            var bytes = channel.Read(byteCount, 0).ToArray();
            buffer.AddRange(bytes);

            var s = Encoding.ASCII.GetString(bytes);

            if (!Enum.IsDefined(typeof(DeviceType), (byte)s[1]))
                throw new FEnetNAKException(FEnetNAKCode.IlegalDeviceMemory);
            if (!Enum.IsDefined(typeof(DataType), (byte)s[2]))
                throw new FEnetNAKException(FEnetNAKCode.DeviceVariableTypeError);

            return DeviceVariable.TryParse(s, UseHexBitIndex, out var deviceVariable)? deviceVariable : throw new FEnetNAKException(FEnetNAKCode.IlegalDeviceMemory);
        }

        private FEnetRequest DeserializeReadRequest(Channel channel, DataType dataType, List<byte> buffer, ushort blockCount)
        {
            if (dataType == DataType.Unknown)
            {
                var deviceVariable = DeserializeDeviceVariable(channel, buffer);
                if (deviceVariable != null)
                {
                    if (deviceVariable.Value.DataType != DataType.Byte)
                        throw new FEnetNAKException(FEnetNAKCode.DeviceVariableTypeError);

                    var count = FEnetMessage.ReadWord(channel, buffer);
                    return new FEnetReadContinuousRequest(deviceVariable.Value.DeviceType, deviceVariable.Value.Index, count) { UseHexBitIndex = UseHexBitIndex };
                }
            }
            else
            {
                List<DeviceVariable> deviceVariables = new List<DeviceVariable>();
                for (int i = 0; i < blockCount; i++)
                {
                    var deviceVariable = DeserializeDeviceVariable(channel, buffer);
                    if (deviceVariable == null)
                        return null;

                    deviceVariables.Add(deviceVariable.Value);
                }
                return new FEnetReadIndividualRequest(dataType, deviceVariables) { UseHexBitIndex = UseHexBitIndex };
            }
            return null;
        }

        private FEnetRequest DeserializeWriteRequest(Channel channel, DataType dataType, List<byte> buffer, ushort blockCount)
        {
            if (dataType == DataType.Unknown)
            {
                var deviceVariable = DeserializeDeviceVariable(channel, buffer);
                if (deviceVariable != null)
                {
                    if (deviceVariable.Value.DataType != DataType.Byte)
                        throw new FEnetNAKException(FEnetNAKCode.DeviceVariableTypeError);

                    var count = FEnetMessage.ReadWord(channel, buffer);

                    if (count > 1400)
                        throw new FEnetNAKException(FEnetNAKCode.OverDataLengthTotal);

                    return new FEnetWriteContinuousRequest(deviceVariable.Value.DeviceType, deviceVariable.Value.Index, channel.Read(count, 0).ToArray()) { UseHexBitIndex = UseHexBitIndex };
                }
            }
            else
            {
                DeviceVariable[] deviceVariables = new DeviceVariable[blockCount];
                DeviceValue[] deviceValues = new DeviceValue[blockCount];

                for (int i = 0; i < blockCount; i++)
                {
                    var deviceVariable = DeserializeDeviceVariable(channel, buffer);
                    if (deviceVariable != null)
                        deviceVariables[i] = deviceVariable.Value;
                }

                for (int i = 0; i < blockCount; i++)
                {
                    var dataCount = FEnetMessage.ReadWord(channel, buffer);

                    if (dataCount > 8)
                        throw new FEnetNAKException(FEnetNAKCode.OverDataLengthIndividual);

                    ulong value = 0;
                    for (int j = 0; j < dataCount; j++)
                    {
                        byte b = channel.Read(0);
                        buffer.Add(b);

                        value |= (ulong)b << (8 * j);
                    }
                    deviceValues[i] = new DeviceValue(value);
                }

                return new FEnetWriteIndividualRequest(dataType, deviceVariables.Zip(deviceValues, (variable, value) => new KeyValuePair<DeviceVariable, DeviceValue>(variable, value))) { UseHexBitIndex = UseHexBitIndex };
            }
            return null;
        }



        class ChannelTask
        {
            public ChannelTask(FEnetSimulationService simulationService, Channel channel, bool createdFromProvider)
            {
                this.simulationService = simulationService;
                this.channel = channel;
                this.createdFromProvider = createdFromProvider;
            }

            private readonly FEnetSimulationService simulationService;
            private readonly Channel channel;
            private readonly bool createdFromProvider;
            private bool isRunning = false;

            public void Start()
            {
                if (!channel.IsDisposed)
                {
                    isRunning = true;
                    Task.Factory.StartNew(() =>
                    {
                        List<byte> buffer = new List<byte>();
                        List<byte> errorBuffer = new List<byte>();
                        while (isRunning && !channel.IsDisposed)
                        {
                            try
                            {
                                var channelTimeout = simulationService.ChannelTimeout;
                                if (!createdFromProvider || channelTimeout == 0)
                                {
                                    simulationService.DeserializeRequest(channel, buffer, errorBuffer);
                                }
                                else if (!Task.Run(() => simulationService.DeserializeRequest(channel, buffer, errorBuffer)).Wait(channelTimeout))
                                {
                                    simulationService.channelTasks.Remove(channel);
                                    channel.Dispose();
                                }
                            }
                            catch
                            {
                                if (createdFromProvider)
                                {
                                    simulationService.channelTasks.Remove(channel);
                                    channel.Dispose();
                                }
                            }
                        }
                        if (!channel.IsDisposed)
                            channel.Dispose();
                    }, TaskCreationOptions.LongRunning);
                }
            }

            public void Stop()
            {
                isRunning = false;
            }
        }

    }


    /// <summary>
    /// FEnet 요청 발생 이벤트 매개변수
    /// </summary>
    public abstract class FEnetRequestedEventArgs : EventArgs
    {
        internal FEnetRequestedEventArgs(Channel channel)
        {
            Channel = channel;
        }

        /// <summary>
        /// 통신 채널
        /// </summary>
        public Channel Channel { get; }

        /// <summary>
        /// NAK 에러 코드
        /// </summary>
        public FEnetNAKCode NAKCode { get; set; }
    }

    /// <summary>
    /// 개별 읽기 요청 발생 이벤트 매개변수
    /// </summary>
    public sealed class FEnetRequestedReadIndividualEventArgs : FEnetRequestedEventArgs
    {
        internal FEnetRequestedReadIndividualEventArgs(FEnetReadIndividualRequest request, Channel channel) : base(channel)
        {
            ResponseValues = request.Select(deviceVariable => new DeviceVariableValue(deviceVariable)).ToArray();
        }

        /// <summary>
        /// 응답할 값 목록
        /// </summary>
        public IReadOnlyList<DeviceVariableValue> ResponseValues { get; }
    }

    /// <summary>
    /// 연속 읽기 요청 발생 이벤트 매개변수
    /// </summary>
    public sealed class FEnetRequestedReadContinuousEventArgs : FEnetRequestedEventArgs
    {
        internal FEnetRequestedReadContinuousEventArgs(FEnetReadContinuousRequest request, Channel channel) : base(channel)
        {
            StartDeviceVariable = request.StartDeviceVariable;
            Count = request.Count;
        }

        /// <summary>
        /// 읽기 요청 시작 디바이스 변수
        /// </summary>
        public DeviceVariable StartDeviceVariable { get; }

        /// <summary>
        /// 읽을 개수
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// 응답할 값 목록
        /// </summary>
        public IEnumerable<byte> ResponseValues { get; set; }
    }

    /// <summary>
    /// 개별 쓰기 요청 발생 이벤트 매개변수
    /// </summary>
    public sealed class FEnetRequestedWriteIndividualEventArgs : FEnetRequestedEventArgs
    {
        internal FEnetRequestedWriteIndividualEventArgs(FEnetWriteIndividualRequest request, Channel channel) : base(channel)
        {
            Values = new ReadOnlyDictionary<DeviceVariable, DeviceValue>(request);
        }

        /// <summary>
        /// 쓰기 위해 받은 값 목록
        /// </summary>
        public IReadOnlyDictionary<DeviceVariable, DeviceValue> Values { get; }
    }

    /// <summary>
    /// 연속 쓰기 요청 발생 이벤트 매개변수
    /// </summary>
    public sealed class FEnetRequestedWriteContinuousEventArgs : FEnetRequestedEventArgs
    {
        internal FEnetRequestedWriteContinuousEventArgs(FEnetWriteContinuousRequest request, Channel channel) : base(channel)
        {
            StartDeviceVariable = request.StartDeviceVariable;
            Values = request.ToArray();
        }

        /// <summary>
        /// 쓰기 요청 시작 디바이스 변수
        /// </summary>
        public DeviceVariable StartDeviceVariable { get; }

        /// <summary>
        /// 쓰기 위해 받은 값 목록
        /// </summary>
        public IReadOnlyList<byte> Values { get; }
    }
}
