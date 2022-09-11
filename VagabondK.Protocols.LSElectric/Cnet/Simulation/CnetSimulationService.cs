using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;

namespace VagabondK.Protocols.LSElectric.Cnet.Simulation
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 시뮬레이터 서비스입니다.
    /// Cnet 클라이언트를 테스트하는 용도로 사용 가능합니다.
    /// </summary>
    public class CnetSimulationService : IDisposable, IEnumerable<KeyValuePair<byte, CnetSimulationStation>>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        public CnetSimulationService() { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        public CnetSimulationService(IChannel channel)
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
        private readonly Dictionary<byte, CnetSimulationStation> simulationStations = new Dictionary<byte, CnetSimulationStation>();

        /// <summary>
        /// 시뮬레이션 스테이션 가져오기
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <returns>시뮬레이션 스테이션</returns>
        public CnetSimulationStation this[byte stationNumber]
        {
            get => simulationStations[stationNumber];
            set => simulationStations[stationNumber] = value;
        }

        /// <summary>
        /// 시뮬레이션 스테이션 국번 목록
        /// </summary>
        public ICollection<byte> StationsNumbers { get => simulationStations.Keys; }

        /// <summary>
        /// 시뮬레이션 스테이션 목록
        /// </summary>
        public ICollection<CnetSimulationStation> SimulationStations { get => simulationStations.Values; }

        /// <summary>
        /// 시뮬레이션 스테이션 포함 여부
        /// </summary>
        /// <param name="stationNumber">시뮬레이션 스테이션 국번</param>
        /// <returns>시뮬레이션 스테이션 포함 여부</returns>
        public bool ContainsStationsNumber(byte stationNumber) => simulationStations.ContainsKey(stationNumber);

        /// <summary>
        /// 시뮬레이션 스테이션 가져오기
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="simulationStation">시뮬레이션 스테이션</param>
        /// <returns>시뮬레이션 스테이션 포함 여부</returns>
        public bool TryGetValue(byte stationNumber, out CnetSimulationStation simulationStation) => simulationStations.TryGetValue(stationNumber, out simulationStation);

        /// <summary>
        /// 시뮬레이션 스테이션 열거
        /// </summary>
        /// <returns>시뮬레이션 스테이션 목록 열거</returns>
        public IEnumerator<KeyValuePair<byte, CnetSimulationStation>> GetEnumerator() => simulationStations.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


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

        private void OnChannelCreated(object sender, ChannelCreatedEventArgs e)
        {
            lock (channelTasks)
            {
                var channelTask = new ChannelTask(this, e.Channel, true);
                channelTasks[e.Channel] = channelTask;
                channelTask.Start();
            }
        }




        private CnetResponse OnRequestedRead(CnetSimulationStation simulationStation, CnetReadRequest request, Channel channel)
        {
            CnetResponse response = null;

            var eventArgs = new CnetRequestedReadEventArgs(request, channel);
            simulationStation.OnRequestedRead(eventArgs);
            if (eventArgs.NAKCode == CnetNAKCode.Unknown)
            {
                switch (request.CommandType)
                {
                    case CnetCommandType.Individual:
                        response = new CnetReadResponse(eventArgs.ResponseValues.Select(v => v.DeviceValue), (CnetReadIndividualRequest)request);
                        break;
                    case CnetCommandType.Continuous:
                        response = new CnetReadResponse(eventArgs.ResponseValues.Select(v => v.DeviceValue), (CnetReadContinuousRequest)request);
                        break;
                }
            }
            else
            {
                response = new CnetNAKResponse(eventArgs.NAKCode, request);
            }

            return response;
        }

        private CnetResponse OnRequestedWrite(CnetSimulationStation simulationStation, CnetWriteRequest request, Channel channel)
        {
            var eventArgs = new CnetRequestedWriteEventArgs(request, channel);
            simulationStation.OnRequestedWrite(eventArgs);

            CnetResponse response;
            if (eventArgs.NAKCode == CnetNAKCode.Unknown)
            {
                response = new CnetACKResponse(request);
            }
            else
            {
                response = new CnetNAKResponse(eventArgs.NAKCode, request);
            }

            return response;
        }

        private CnetResponse OnRequestedRegisterMonitor(CnetSimulationStation simulationStation, CnetRegisterMonitorRequest request, Channel channel)
        {
            CnetMonitor monitor = null;
            switch (request.CommandType)
            {
                case CnetCommandType.Individual:
                    monitor = new CnetMonitorByIndividualAccess(request.StationNumber, request.MonitorNumber, (CnetRegisterMonitorIndividualRequest)request);
                    break;
                case CnetCommandType.Continuous:
                    monitor = new CnetMonitorByContinuousAccess(request.StationNumber, request.MonitorNumber, ((CnetRegisterMonitorContinuousRequest)request).StartDeviceVariable, ((CnetRegisterMonitorContinuousRequest)request).Count);
                    break;
            }
            simulationStation.Monitors[request.MonitorNumber] = monitor;

            return new CnetACKResponse(request);
        }

        private CnetResponse OnRequestedExecuteMonitor(CnetSimulationStation simulationStation, CnetExecuteMonitorRequest request, Channel channel)
        {
            CnetResponse response = null;

            var eventArgs = new CnetRequestedReadEventArgs(request, channel);
            simulationStation.OnRequestedRead(eventArgs);
            if (eventArgs.NAKCode == CnetNAKCode.Unknown)
            {
                switch (request.CommandType)
                {
                    case CnetCommandType.Individual:
                        response = new CnetReadResponse(eventArgs.ResponseValues.Select(v => v.DeviceValue), (CnetExecuteMonitorIndividualRequest)request);
                        break;
                    case CnetCommandType.Continuous:
                        response = new CnetReadResponse(eventArgs.ResponseValues.Select(v => v.DeviceValue), (CnetExecuteMonitorContinuousRequest)request);
                        break;
                }
            }
            else
            {
                response = new CnetNAKResponse(eventArgs.NAKCode, request);
            }

            return response;
        }

        private void DeserializeRequest(Channel channel, List<byte> buffer)
        {
            CnetRequest request = null;

            List<byte> errorBuffer = new List<byte>();

            byte enq = 0;
            do
            {
                enq = channel.Read(0);
                if (enq == CnetMessage.ENQ)
                    buffer.Add(enq);
                else
                    errorBuffer.Add(enq);
            } while (enq != CnetMessage.ENQ);

            if (errorBuffer.Count > 0)
            {
                channel.Logger.Log(new UnrecognizedErrorLog(channel, errorBuffer.ToArray()));
                errorBuffer.Clear();
            }

            byte eot = 0;
            do
            {
                eot = channel.Read(0);
                buffer.Add(eot);
            } while (eot != CnetMessage.EOT);

            if (buffer.Count < 6)
                throw new Exception();

            bool useBCC = true;

            if (CnetMessage.TryParseByte(buffer, 1, out var stationNumber)
                && (Enum.IsDefined(typeof(CnetCommand), buffer[3]) || Enum.IsDefined(typeof(CnetCommand), (byte)(buffer[3] - 0x20))))
            {
                ushort commandTypeValue = (ushort)((buffer[4] << 8) | buffer[5]);
                useBCC = buffer[3] > 0x60;
                CnetCommand command = (CnetCommand)(useBCC ? buffer[3] - 0x20 : buffer[3]);
                byte monitorNumber;

                int frameLength = buffer.Count;

                if (useBCC)
                {
                    buffer.AddRange(channel.Read(2, 0));
                    if (!buffer.Skip(buffer.Count - 2).Take(2).SequenceEqual(Encoding.ASCII.GetBytes((buffer.Take(buffer.Count - 2).Sum(b => b) % 256).ToString("X2"))))
                    {
                        channel.Logger.Log(new UnrecognizedErrorLog(channel, buffer.ToArray()));
                        return;
                    }
                }

                int index = 6;

                try
                {
                    switch (command)
                    {
                        case CnetCommand.Read:
                            if (Enum.IsDefined(typeof(CnetCommandType), commandTypeValue))
                                request = DeserializeReadRequest(stationNumber, (CnetCommandType)commandTypeValue, buffer, useBCC, ref index);
                            break;
                        case CnetCommand.Write:
                            if (Enum.IsDefined(typeof(CnetCommandType), commandTypeValue))
                                request = DeserializeWriteRequest(stationNumber, (CnetCommandType)commandTypeValue, buffer, useBCC, ref index);
                            break;
                        case CnetCommand.RegisterMonitor:
                            if (CnetMessage.TryParseByte(buffer, 4, out monitorNumber))
                                request = DeserializeRegisterMonitorRequest(stationNumber, monitorNumber, buffer, useBCC, ref index);
                            break;
                        case CnetCommand.ExecuteMonitor:
                            if (CnetMessage.TryParseByte(buffer, 4, out monitorNumber))
                                request = DeserializeExecuteMonitorRequest(stationNumber, monitorNumber, buffer, useBCC, ref index);
                            break;
                    }

                    if (index < frameLength - 1)
                        throw new CnetNAKException(CnetNAKCode.UnnecessaryDataInFrame);
                }
                catch (CnetNAKException ex)
                {
                    channel.Logger.Log(new UnrecognizedErrorLog(channel, buffer.ToArray()));

                    var response = new CnetNAKResponse(ex.Code, stationNumber, command, commandTypeValue);
                    var message = response.Serialize(useBCC).ToArray();
                    channel.Write(message);
                    channel.Logger.Log(new CnetNAKLog(channel, response, message, null));
                }
            }

            if (request != null && simulationStations.TryGetValue(request.StationNumber, out var simulationStation))
            {
                var requestLog = new CnetRequestLog(channel, request, buffer.ToArray());
                channel.Logger?.Log(requestLog);
                CnetResponse response = null;
                try
                {
                    switch (request.Command)
                    {
                        case CnetCommand.Read:
                            response = OnRequestedRead(simulationStation, (CnetReadRequest)request, channel);
                            break;
                        case CnetCommand.Write:
                            response = OnRequestedWrite(simulationStation, (CnetWriteRequest)request, channel);
                            break;
                        case CnetCommand.RegisterMonitor:
                            response = OnRequestedRegisterMonitor(simulationStation, (CnetRegisterMonitorRequest)request, channel);
                            break;
                        case CnetCommand.ExecuteMonitor:
                            response = OnRequestedExecuteMonitor(simulationStation, (CnetExecuteMonitorRequest)request, channel);
                            break;
                    }

                    if (response != null)
                    {
                        var responseMessage = response.Serialize(useBCC).ToArray();
                        channel.Logger.Log(new CnetResponseLog(channel, response, responseMessage, requestLog));
                        channel.Write(responseMessage);
                    }
                }
                catch (CnetNAKException ex)
                {
                    var nakResponse = new CnetNAKResponse(ex.Code, request);
                    var message = nakResponse.Serialize(useBCC).ToArray();
                    channel.Write(message);
                    channel.Logger.Log(new CnetNAKLog(channel, nakResponse, message, requestLog));
                }
            }
            else
            {
                channel.Logger.Log(new UnrecognizedErrorLog(channel, buffer.ToArray()));
            }
        }

        private DeviceVariable? DeserializeDeviceVariable(List<byte> buffer, ref int index)
        {
            if (buffer.Count < index + 2)
                throw new Exception();

            if (CnetMessage.TryParseByte(buffer, index, out var byteCount))
            {
                index += 2;

                if (byteCount < 4 || byteCount > 12)
                    throw new CnetNAKException(CnetNAKCode.OverVariableLength);

                if (buffer.Count < index + byteCount)
                    throw new Exception();

                var s = Encoding.ASCII.GetString(buffer.Skip(index).Take(byteCount).ToArray());

                if (!Enum.IsDefined(typeof(DeviceType), (byte)s[1]))
                    throw new CnetNAKException(CnetNAKCode.IlegalDeviceMemory);
                if (!Enum.IsDefined(typeof(DataType), (byte)s[2]))
                    throw new CnetNAKException(CnetNAKCode.DeviceVariableTypeError);
                if (s[0] != '%' || !uint.TryParse(s.Remove(0, 3), out _))
                    throw new CnetNAKException(CnetNAKCode.DataError);

                if (DeviceVariable.TryParse(s, out var deviceVariable))
                {
                    index += byteCount;
                    return deviceVariable;
                }
            }
            return null;
        }

        private CnetRequest DeserializeReadRequest(byte stationNumber, CnetCommandType commandType, List<byte> buffer, bool useBCC, ref int index)
        {
            if (commandType == CnetCommandType.Individual)
            {
                if (buffer.Count < 8)
                    throw new Exception();
                if (CnetMessage.TryParseByte(buffer, 6, out var blockCount))
                {
                    index += 2;

                    if (blockCount > 16)
                        throw new CnetNAKException(CnetNAKCode.OverRequestReadBlockCount);

                    List<DeviceVariable> deviceVariables = new List<DeviceVariable>();
                    for (int i = 0; i < blockCount; i++)
                    {
                        var deviceVariable = DeserializeDeviceVariable(buffer, ref index);
                        if (deviceVariable == null)
                            return null;

                        if (deviceVariables.Count > 0
                            && (deviceVariables[0].DeviceType != deviceVariable.Value.DeviceType || deviceVariables[0].DataType != deviceVariable.Value.DataType))
                            throw new CnetNAKException(CnetNAKCode.DeviceVariableTypeIsDifferent);

                        deviceVariables.Add(deviceVariable.Value);
                    }
                    return new CnetReadIndividualRequest(stationNumber, deviceVariables);
                }
            }
            else
            {
                var deviceVariable = DeserializeDeviceVariable(buffer, ref index);
                if (deviceVariable != null)
                {
                    if (buffer.Count < index + 2)
                        throw new Exception();
                    if (CnetMessage.TryParseByte(buffer, index, out var count))
                    {
                        index += 2;

                        if (count > 60)
                            throw new CnetNAKException(CnetNAKCode.OverDataLength);

                        return new CnetReadContinuousRequest(stationNumber, deviceVariable.Value, count);
                    }
                    else throw new CnetNAKException(CnetNAKCode.DataError);
                }
            }
            return null;
        }

        private CnetRequest DeserializeWriteRequest(byte stationNumber, CnetCommandType commandType, List<byte> buffer, bool useBCC, ref int index)
        {
            if (commandType == CnetCommandType.Individual)
            {
                if (buffer.Count < 8)
                    throw new Exception();
                if (CnetMessage.TryParseByte(buffer, 6, out var blockCount))
                {
                    index += 2;

                    if (blockCount > 16)
                        throw new CnetNAKException(CnetNAKCode.OverRequestReadBlockCount);

                    List<KeyValuePair<DeviceVariable, DeviceValue>> deviceValues = new List<KeyValuePair<DeviceVariable, DeviceValue>>();

                    for (int i = 0; i < blockCount; i++)
                    {
                        var deviceVariable = DeserializeDeviceVariable(buffer, ref index);
                        if (deviceVariable != null)
                        {
                            if (deviceValues.Count > 0
                                && (deviceValues[0].Key.DeviceType != deviceVariable.Value.DeviceType || deviceValues[0].Key.DataType != deviceVariable.Value.DataType))
                                throw new CnetNAKException(CnetNAKCode.DeviceVariableTypeIsDifferent);

                            int dataUnit;
                            switch (deviceVariable.Value.DataType)
                            {
                                case DataType.Bit:
                                case DataType.Byte:
                                    dataUnit = 1;
                                    break;
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
                                    return null;
                            }

                            if (buffer.Count < index + dataUnit * 2)
                                throw new Exception();

                            switch (deviceVariable.Value.DataType)
                            {
                                case DataType.Bit:
                                    if (!CnetMessage.TryParseByte(buffer, index, out var bitValue) && bitValue <= 1) throw new CnetNAKException(CnetNAKCode.DataParsingError);
                                    else deviceValues.Add(new KeyValuePair<DeviceVariable, DeviceValue>(deviceVariable.Value, new DeviceValue(bitValue != 0)));
                                    break;
                                case DataType.Byte:
                                    if (!CnetMessage.TryParseByte(buffer, index, out var byteValue)) throw new CnetNAKException(CnetNAKCode.DataParsingError);
                                    else deviceValues.Add(new KeyValuePair<DeviceVariable, DeviceValue>(deviceVariable.Value, new DeviceValue(byteValue)));
                                    break;
                                case DataType.Word:
                                    if (!CnetMessage.TryParseWord(buffer, index, out var wordValue)) throw new CnetNAKException(CnetNAKCode.DataParsingError);
                                    else deviceValues.Add(new KeyValuePair<DeviceVariable, DeviceValue>(deviceVariable.Value, new DeviceValue(wordValue)));
                                    break;
                                case DataType.DoubleWord:
                                    if (!CnetMessage.TryParseDoubleWord(buffer, index, out var doubleWordValue)) throw new CnetNAKException(CnetNAKCode.DataParsingError);
                                    else deviceValues.Add(new KeyValuePair<DeviceVariable, DeviceValue>(deviceVariable.Value, new DeviceValue(doubleWordValue)));
                                    break;
                                case DataType.LongWord:
                                    if (!CnetMessage.TryParseLongWord(buffer, index, out var longWordValue)) throw new CnetNAKException(CnetNAKCode.DataParsingError);
                                    else deviceValues.Add(new KeyValuePair<DeviceVariable, DeviceValue>(deviceVariable.Value, new DeviceValue(longWordValue)));
                                    break;
                            }
                            index += dataUnit * 2;
                        }
                        else return null;
                    }

                    return new CnetWriteIndividualRequest(stationNumber, deviceValues);
                }
            }
            else
            {
                var deviceVariable = DeserializeDeviceVariable(buffer, ref index);
                if (deviceVariable != null)
                {
                    if (buffer.Count < index + 2)
                        throw new Exception();
                    if (CnetMessage.TryParseByte(buffer, index, out var count))
                    {
                        index += 2;

                        if (count > 60)
                            throw new CnetNAKException(CnetNAKCode.OverDataLength);

                        int dataUnit;
                        switch (deviceVariable.Value.DataType)
                        {
                            case DataType.Bit:
                            case DataType.Byte:
                                dataUnit = 1;
                                break;
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
                                return null;
                        }

                        List<DeviceValue> deviceValues = new List<DeviceValue>();

                        for (int i = 0; i < count; i++)
                        {
                            if (buffer.Count < index + dataUnit * 2)
                                throw new Exception();

                            switch (deviceVariable.Value.DataType)
                            {
                                case DataType.Bit:
                                    if (!CnetMessage.TryParseByte(buffer, index, out var bitValue) && bitValue <= 1) throw new CnetNAKException(CnetNAKCode.DataParsingError);
                                    else deviceValues.Add(new DeviceValue(bitValue != 0));
                                    break;
                                case DataType.Byte:
                                    if (CnetMessage.TryParseByte(buffer, index, out var byteValue)) deviceValues.Add(new DeviceValue(byteValue));
                                    else throw new CnetNAKException(CnetNAKCode.DataParsingError);
                                    break;
                                case DataType.Word:
                                    if (CnetMessage.TryParseWord(buffer, index, out var wordValue)) deviceValues.Add(new DeviceValue(wordValue));
                                    else throw new CnetNAKException(CnetNAKCode.DataParsingError);
                                    break;
                                case DataType.DoubleWord:
                                    if (CnetMessage.TryParseDoubleWord(buffer, index, out var doubleWordValue)) deviceValues.Add(new DeviceValue(doubleWordValue));
                                    else throw new CnetNAKException(CnetNAKCode.DataParsingError);
                                    break;
                                case DataType.LongWord:
                                    if (CnetMessage.TryParseLongWord(buffer, index, out var longWordValue)) deviceValues.Add(new DeviceValue(longWordValue));
                                    else throw new CnetNAKException(CnetNAKCode.DataParsingError);
                                    break;
                            }

                            index += dataUnit * 2;
                        }

                        return new CnetWriteContinuousRequest(stationNumber, deviceVariable.Value, deviceValues);
                    }
                    else throw new CnetNAKException(CnetNAKCode.DataError);
                }
            }
            return null;
        }

        private CnetRequest DeserializeRegisterMonitorRequest(byte stationNumber, byte monitorNumber, List<byte> buffer, bool useBCC, ref int index)
        {
            if (buffer.Count < 9)
                throw new Exception();

            if (buffer[6] == 'R' && Enum.IsDefined(typeof(CnetCommandType), (ushort)((buffer[7] << 8) | buffer[8])))
            {
                index += 3;

                var commandType = (CnetCommandType)(ushort)((buffer[7] << 8) | buffer[8]);

                if (commandType == CnetCommandType.Individual)
                {
                    if (buffer.Count < 11)
                        throw new Exception();
                    if (CnetMessage.TryParseByte(buffer, 9, out var blockCount))
                    {
                        index += 2;

                        List<DeviceVariable> deviceVariables = new List<DeviceVariable>();
                        for (int i = 0; i < blockCount; i++)
                        {
                            var deviceVariable = DeserializeDeviceVariable(buffer, ref index);
                            if (deviceVariable == null)
                                return null;
                            deviceVariables.Add(deviceVariable.Value);
                        }
                        return new CnetRegisterMonitorIndividualRequest(stationNumber, monitorNumber, deviceVariables);
                    }
                }
                else
                {
                    var deviceVariable = DeserializeDeviceVariable(buffer, ref index);
                    if (deviceVariable != null)
                    {
                        if (buffer.Count < index + 2)
                            throw new Exception();
                        if (CnetMessage.TryParseByte(buffer, index, out var count))
                        {
                            index += 2;
                            return new CnetRegisterMonitorContinuousRequest(stationNumber, monitorNumber, deviceVariable.Value, count);
                        }
                    }
                }
                return null;
            }
            return null;
        }

        private CnetRequest DeserializeExecuteMonitorRequest(byte stationNumber, byte monitorNumber, List<byte> buffer, bool useBCC, ref int index)
        {
            if (simulationStations.TryGetValue(stationNumber, out var simulationStation))
            {
                if (simulationStation.Monitors.TryGetValue(monitorNumber, out var monitor))
                {
                    return monitor.CreateExecuteRequest();
                }
                else throw new CnetNAKException(CnetNAKCode.NotExistsMonitorNumber);
            }
            return null;
        }


        class ChannelTask
        {
            public ChannelTask(CnetSimulationService simulationService, Channel channel, bool createdFromProvider)
            {
                this.simulationService = simulationService;
                this.channel = channel;
                this.createdFromProvider = createdFromProvider;
            }

            private readonly CnetSimulationService simulationService;
            private readonly Channel channel;
            private readonly bool createdFromProvider;
            private bool isRunning = false;

            public void Start()
            {
                if (!channel.IsDisposed)
                {
                    isRunning = true;
                    Task.Run(() =>
                    {
                        while (isRunning && !channel.IsDisposed)
                        {
                            try
                            {
                                var channelTimeout = simulationService.ChannelTimeout;
                                List<byte> buffer = new List<byte>();
                                if (!createdFromProvider || channelTimeout == 0)
                                {
                                    simulationService.DeserializeRequest(channel, buffer);
                                }
                                else if (!Task.Run(() => simulationService.DeserializeRequest(channel, buffer)).Wait(channelTimeout))
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
                    });
                }
            }

            public void Stop()
            {
                isRunning = false;
            }
        }

    }


    /// <summary>
    /// Cnet 요청 발생 이벤트 매개변수
    /// </summary>
    public abstract class CnetRequestedEventArgs : EventArgs
    {
        internal CnetRequestedEventArgs(Channel channel)
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
        public CnetNAKCode NAKCode { get; set; }
    }

    /// <summary>
    /// 읽기 요청 발생 이벤트 매개변수
    /// </summary>
    public sealed class CnetRequestedReadEventArgs : CnetRequestedEventArgs
    {
        internal CnetRequestedReadEventArgs(CnetReadRequest request, Channel channel) : base(channel)
        {
            switch (request.CommandType)
            {
                case CnetCommandType.Individual:
                    ResponseValues = ((CnetReadIndividualRequest)request).Select(deviceVariable => new DeviceVariableValue(deviceVariable)).ToArray();
                    break;
                case CnetCommandType.Continuous:
                    ResponseValues = ((CnetReadContinuousRequest)request).ToDeviceVariables().Select(deviceVariable => new DeviceVariableValue(deviceVariable)).ToArray();
                    break;
            }
        }
        internal CnetRequestedReadEventArgs(CnetExecuteMonitorRequest request, Channel channel) : base(channel)
        {
            switch (request.CommandType)
            {
                case CnetCommandType.Individual:
                    ResponseValues = ((CnetExecuteMonitorIndividualRequest)request).Select(deviceVariable => new DeviceVariableValue(deviceVariable)).ToArray();
                    break;
                case CnetCommandType.Continuous:
                    ResponseValues = ((CnetExecuteMonitorContinuousRequest)request).ToDeviceVariables().Select(deviceVariable => new DeviceVariableValue(deviceVariable)).ToArray();
                    break;
            }
        }

        /// <summary>
        /// 응답할 값 목록
        /// </summary>
        public IReadOnlyList<DeviceVariableValue> ResponseValues { get; }
    }

    /// <summary>
    /// 쓰기 요청 발생 이벤트 매개변수
    /// </summary>
    public sealed class CnetRequestedWriteEventArgs : CnetRequestedEventArgs
    {
        internal CnetRequestedWriteEventArgs(CnetWriteRequest request, Channel channel) : base(channel)
        {
            switch(request.CommandType)
            {
                case CnetCommandType.Individual:
                    Values = new ReadOnlyDictionary<DeviceVariable, DeviceValue>((CnetWriteIndividualRequest)request);
                    break;
                case CnetCommandType.Continuous:
                    CnetWriteContinuousRequest writeContinuousRequest = (CnetWriteContinuousRequest)request;
                    var values = new Dictionary<DeviceVariable, DeviceValue>();
                    foreach (var pair in writeContinuousRequest.ToDeviceVariables().Zip(writeContinuousRequest, (variable, value) => new { variable, value }))
                        values[pair.variable] = pair.value;
                    Values = new ReadOnlyDictionary<DeviceVariable, DeviceValue>(values);
                    break;
            }
        }

        /// <summary>
        /// 쓰기 위해 받은 값 목록
        /// </summary>
        public IReadOnlyDictionary<DeviceVariable, DeviceValue> Values { get; }
    }
}
