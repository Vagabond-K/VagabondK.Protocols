using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.LSElectric.Cnet.Logging;

namespace VagabondK.Protocols.LSElectric.Cnet.Simulation
{
    /// <summary>
    /// LS ELECTRIC Cnet 프로토콜 시뮬레이터입니다.
    /// Cnet 클라이언트를 테스트하는 용도로 사용 가능합니다.
    /// </summary>
    public class CnetSimulationService : IDisposable
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
            this.channel = channel;
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
        private readonly Dictionary<byte, Dictionary<byte, CnetMonitor>> monitors = new Dictionary<byte, Dictionary<byte, CnetMonitor>>();

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


        /// <summary>
        /// 슬레이브 주소 검증 이벤트
        /// </summary>
        public event EventHandler<ValidatingStationNumberEventArgs> ValidatingStationNumber;

        public event EventHandler<RequestedReadEventArgs> RequestedRead;

        public event EventHandler<RequestedWriteEventArgs> RequestedWrite;


        private void OnRequestedRead(CnetReadRequest request, Channel channel)
        {
            var eventArgs = new RequestedReadEventArgs(request, channel);
            RequestedRead?.Invoke(this, eventArgs);
            if (eventArgs.NAKCode == CnetNAKCode.Unknown)
            {
                switch (request.CommandType)
                {
                    case CnetCommandType.Each:
                        channel.Write(new CnetReadResponse(eventArgs.ResponseValues.Select(v => v.DeviceValue), (CnetReadEachAddressRequest)request).Serialize().ToArray());
                        break;
                    case CnetCommandType.Block:
                        channel.Write(new CnetReadResponse(eventArgs.ResponseValues.Select(v => v.DeviceValue), (CnetReadAddressBlockRequest)request).Serialize().ToArray());
                        break;
                }
            }
            else
            {
                channel.Write(new CnetNAKResponse(eventArgs.NAKCode, request).Serialize().ToArray());
            }
        }

        private void OnRequestedWrite(CnetWriteRequest request, Channel channel)
        {
            var eventArgs = new RequestedWriteEventArgs(request, channel);
            RequestedWrite?.Invoke(this, eventArgs);
            if (eventArgs.NAKCode == CnetNAKCode.Unknown)
            {
                channel.Write(new CnetACKResponse(request).Serialize().ToArray());
            }
            else
            {
                channel.Write(new CnetNAKResponse(eventArgs.NAKCode, request).Serialize().ToArray());
            }
        }

        private void OnRequestedRegisterMonitor(CnetRegisterMonitorRequest request, Channel channel)
        {
            CnetMonitor monitor = null;
            switch (request.CommandType)
            {
                case CnetCommandType.Each:
                    monitor = new CnetMonitorByEachAddress(request.StationNumber, request.MonitorNumber, (CnetRegisterMonitorEachAddressRequest)request);
                    break;
                case CnetCommandType.Block:
                    monitor = new CnetMonitorByAddressBlock(request.StationNumber, request.MonitorNumber)
                    {
                        DeviceAddress = ((CnetRegisterMonitorAddressBlockRequest)request).DeviceAddress,
                        Count = ((CnetRegisterMonitorAddressBlockRequest)request).Count,
                    };
                    break;
            }
            
            
        }

        private void OnRequestedExecuteMonitor(CnetExecuteMonitorRequest request, Channel channel)
        {
            var eventArgs = new RequestedReadEventArgs(request, channel);
            RequestedRead?.Invoke(this, eventArgs);
            if (eventArgs.NAKCode == CnetNAKCode.Unknown)
            {
                switch (request.CommandType)
                {
                    case CnetCommandType.Each:
                        channel.Write(new CnetReadResponse(eventArgs.ResponseValues.Select(v => v.DeviceValue), (CnetExecuteMonitorEachAddressRequest)request).Serialize().ToArray());
                        break;
                    case CnetCommandType.Block:
                        channel.Write(new CnetReadResponse(eventArgs.ResponseValues.Select(v => v.DeviceValue), (CnetExecuteMonitorAddressBlockRequest)request).Serialize().ToArray());
                        break;
                }
            }
            else
            {
                channel.Write(new CnetNAKResponse(eventArgs.NAKCode, request).Serialize().ToArray());
            }
        }

        private bool IsValidStationNumber(byte stationNumber, Channel channel)
        {
            var eventArgs = new ValidatingStationNumberEventArgs(stationNumber, channel);
            ValidatingStationNumber?.Invoke(this, eventArgs);
            return eventArgs.IsValid;
        }

        private CnetRequest DeserializeRequest(Channel channel, List<byte> buffer)
        {
            CnetRequest result = null;

            List<byte> errorBuffer = new List<byte>();
            while (!channel.IsDisposed)
            {
                if (errorBuffer.Count >= 256)
                {
                    channel.Logger.Log(new Protocols.Logging.UnrecognizedErrorLog(channel, errorBuffer.ToArray()));
                    errorBuffer.Clear();
                }

                if (buffer.Count < 6 && !channel.IsDisposed)
                    buffer.AddRange(channel.Read((uint)(6 - buffer.Count), 0));

                if (channel.IsDisposed) break;

                if (buffer[0] == CnetMessage.ENQ
                    && CnetMessage.TryParseByte(buffer, 1, out var stationNumber)
                    && IsValidStationNumber(stationNumber, channel)
                    && (Enum.IsDefined(typeof(CnetCommand), buffer[3]) || Enum.IsDefined(typeof(CnetCommand), (byte)(buffer[3] - 0x20))))
                {
                    ushort commandTypeValue = (ushort)((buffer[4] << 8) | buffer[5]);
                    bool useBCC = buffer[3] > 0x60;
                    CnetCommand command = (CnetCommand)(useBCC ? buffer[3] - 0x20 : buffer[3]);
                    byte monitorNumber;

                    switch (command)
                    {
                        case CnetCommand.Read:
                            if (Enum.IsDefined(typeof(CnetCommandType), commandTypeValue))
                                result = DeserializeReadRequest(channel, stationNumber, (CnetCommandType)commandTypeValue, buffer, useBCC);
                            break;
                        case CnetCommand.Write:
                            if (Enum.IsDefined(typeof(CnetCommandType), commandTypeValue))
                                result = DeserializeWriteRequest(channel, stationNumber, (CnetCommandType)commandTypeValue, buffer, useBCC);
                            break;
                        case CnetCommand.RegisterMonitor:
                            if (CnetMessage.TryParseByte(buffer, 4, out monitorNumber))
                                result = DeserializeRegisterMonitorRequest(channel, stationNumber, monitorNumber, buffer, useBCC);
                            break;
                        case CnetCommand.ExecuteMonitor:
                            if (CnetMessage.TryParseByte(buffer, 4, out monitorNumber))
                                result = DeserializeExecuteMonitorRequest(channel, stationNumber, monitorNumber, buffer, useBCC);
                            break;
                    }
                }

                if (result != null)
                {
                    if (errorBuffer.Count > 0)
                    {
                        channel.Logger.Log(new Protocols.Logging.UnrecognizedErrorLog(channel, errorBuffer.ToArray()));
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

        private DeviceAddress? DeserializeDeviceAddress(Channel channel, List<byte> buffer, ref int index)
        {
            if (buffer.Count < index + 2)
                buffer.AddRange(channel.Read((uint)(index + 2 - buffer.Count), 0));
            if (CnetMessage.TryParseByte(buffer, index, out var addressCount))
            {
                index += 2;

                if (buffer.Count < index + addressCount)
                    buffer.AddRange(channel.Read((uint)(index + addressCount - buffer.Count), 0));
                if (DeviceAddress.TryParse(Encoding.ASCII.GetString(buffer.Skip(index).Take(addressCount).ToArray()), out var deviceAddress))
                {
                    index += addressCount;
                    return deviceAddress;
                }
            }
            return null;
        }

        private bool DeserializeTail(Channel channel, List<byte> buffer, ref int index, bool useBCC)
        {
            if (buffer.Count < index + 1)
                buffer.AddRange(channel.Read((uint)(index + 1 - buffer.Count), 0));

            if (buffer[index] == CnetMessage.EOT)
            {
                index++;
                if (useBCC)
                {
                    if (buffer.Count < index + 2)
                        buffer.AddRange(channel.Read((uint)(index + 2 - buffer.Count), 0));
                    if (!buffer.Skip(index).Take(2).SequenceEqual(Encoding.ASCII.GetBytes((buffer.Take(index).Sum(b => b) % 256).ToString("X2"))))
                        return true;
                }
                return true;
            }
            return false;
        }

        private CnetRequest DeserializeReadRequest(Channel channel, byte stationNumber, CnetCommandType commandType, List<byte> buffer, bool useBCC)
        {
            if (commandType == CnetCommandType.Each)
            {
                if (buffer.Count < 8 && !channel.IsDisposed)
                    buffer.AddRange(channel.Read((uint)(8 - buffer.Count), 0));
                if (CnetMessage.TryParseByte(buffer, 6, out var blockCount))
                {
                    List<DeviceAddress> deviceAddresses = new List<DeviceAddress>();
                    int index = 8;
                    for (int i = 0; i < blockCount; i++)
                    {
                        var deviceAddress = DeserializeDeviceAddress(channel, buffer, ref index);
                        if (deviceAddress == null)
                            return null;
                        deviceAddresses.Add(deviceAddress.Value);
                    }
                    if (DeserializeTail(channel, buffer, ref index, useBCC))
                        return new CnetReadEachAddressRequest(stationNumber, deviceAddresses, useBCC);
                }
            }
            else
            {
                int index = 6;
                var deviceAddress = DeserializeDeviceAddress(channel, buffer, ref index);
                if (deviceAddress != null)
                {
                    if (buffer.Count < index + 2)
                        buffer.AddRange(channel.Read((uint)(index + 2 - buffer.Count), 0));
                    if (CnetMessage.TryParseByte(buffer, index, out var count))
                    {
                        index += 2;
                        if (DeserializeTail(channel, buffer, ref index, useBCC))
                            return new CnetReadAddressBlockRequest(stationNumber, deviceAddress.Value, count, useBCC);
                    }
                }
            }
            return null;
        }

        private CnetRequest DeserializeWriteRequest(Channel channel, byte stationNumber, CnetCommandType commandType, List<byte> buffer, bool useBCC)
        {
            if (commandType == CnetCommandType.Each)
            {
                if (buffer.Count < 8 && !channel.IsDisposed)
                    buffer.AddRange(channel.Read((uint)(8 - buffer.Count), 0));
                if (CnetMessage.TryParseByte(buffer, 6, out var blockCount))
                {
                    List<KeyValuePair<DeviceAddress, DeviceValue>> deviceValues = new List<KeyValuePair<DeviceAddress, DeviceValue>>();
                    int index = 8;

                    for (int i = 0; i < blockCount; i++)
                    {
                        var deviceAddress = DeserializeDeviceAddress(channel, buffer, ref index);
                        if (deviceAddress != null)
                        {
                            int dataUnit;
                            switch (deviceAddress.Value.DataType)
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
                                buffer.AddRange(channel.Read((uint)(index + dataUnit * 2 - buffer.Count), 0));

                            switch (deviceAddress.Value.DataType)
                            {
                                case DataType.Bit:
                                    if (!CnetMessage.TryParseByte(buffer, index, out var bitValue)) return null;
                                    else deviceValues.Add(new KeyValuePair<DeviceAddress, DeviceValue>(deviceAddress.Value, new DeviceValue(bitValue != 0)));
                                    break;
                                case DataType.Byte:
                                    if (!CnetMessage.TryParseByte(buffer, index, out var byteValue)) return null;
                                    else deviceValues.Add(new KeyValuePair<DeviceAddress, DeviceValue>(deviceAddress.Value, new DeviceValue(byteValue)));
                                    break;
                                case DataType.Word:
                                    if (!CnetMessage.TryParseUint16(buffer, index, out var wordValue)) return null; 
                                    else deviceValues.Add(new KeyValuePair<DeviceAddress, DeviceValue>(deviceAddress.Value, new DeviceValue(wordValue)));
                                    break;
                                case DataType.DoubleWord:
                                    if (!CnetMessage.TryParseUint32(buffer, index, out var doubleWordValue)) return null; 
                                    else deviceValues.Add(new KeyValuePair<DeviceAddress, DeviceValue>(deviceAddress.Value, new DeviceValue(doubleWordValue)));
                                    break;
                                case DataType.LongWord:
                                    if (!CnetMessage.TryParseUint64(buffer, index, out var longWordValue)) return null;
                                    else deviceValues.Add(new KeyValuePair<DeviceAddress, DeviceValue>(deviceAddress.Value, new DeviceValue(longWordValue)));
                                    break;
                            }
                            index += dataUnit * 2;
                        }
                        else return null;
                    }

                    if (DeserializeTail(channel, buffer, ref index, useBCC))
                        return new CnetWriteEachAddressRequest(stationNumber, deviceValues, useBCC);
                }
            }
            else
            {
                int index = 6;
                var deviceAddress = DeserializeDeviceAddress(channel, buffer, ref index);
                if (deviceAddress != null)
                {
                    if (buffer.Count < index + 2)
                        buffer.AddRange(channel.Read((uint)(index + 2 - buffer.Count), 0));
                    if (CnetMessage.TryParseByte(buffer, index, out var count))
                    {
                        index += 2;

                        int dataUnit;
                        switch (deviceAddress.Value.DataType)
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
                                buffer.AddRange(channel.Read((uint)(index + dataUnit * 2 - buffer.Count), 0));

                            switch (deviceAddress.Value.DataType)
                            {
                                case DataType.Bit:
                                    if (CnetMessage.TryParseByte(buffer, index, out var bitValue)) deviceValues.Add(new DeviceValue(bitValue != 0));
                                    else return null;
                                    break;
                                case DataType.Byte:
                                    if (CnetMessage.TryParseByte(buffer, index, out var byteValue)) deviceValues.Add(new DeviceValue(byteValue));
                                    else return null;
                                    break;
                                case DataType.Word:
                                    if (CnetMessage.TryParseUint16(buffer, index, out var wordValue)) deviceValues.Add(new DeviceValue(wordValue));
                                    else return null;
                                    break;
                                case DataType.DoubleWord:
                                    if (CnetMessage.TryParseUint32(buffer, index, out var doubleWordValue)) deviceValues.Add(new DeviceValue(doubleWordValue));
                                    else return null;
                                    break;
                                case DataType.LongWord:
                                    if (CnetMessage.TryParseUint64(buffer, index, out var longWordValue)) deviceValues.Add(new DeviceValue(longWordValue));
                                    else return null;
                                    break;
                            }

                            index += dataUnit * 2;
                        }

                        if (DeserializeTail(channel, buffer, ref index, useBCC))
                            return new CnetWriteAddressBlockRequest(stationNumber, deviceAddress.Value, deviceValues, useBCC);
                    }
                }
            }
            return null;
        }

        private CnetRequest DeserializeRegisterMonitorRequest(Channel channel, byte stationNumber, byte monitorNumber, List<byte> buffer, bool useBCC)
        {
            if (buffer.Count < 9 && !channel.IsDisposed)
                buffer.AddRange(channel.Read((uint)(9 - buffer.Count), 0));

            if (buffer[6] == 'R' && Enum.IsDefined(typeof(CnetCommandType), (ushort)(buffer[7] << 8) | buffer[8]))
            {
                var commandType = (CnetCommandType)(ushort)((buffer[7] << 8) | buffer[8]);

                if (commandType == CnetCommandType.Each)
                {
                    if (buffer.Count < 11 && !channel.IsDisposed)
                        buffer.AddRange(channel.Read((uint)(11 - buffer.Count), 0));
                    if (CnetMessage.TryParseByte(buffer, 9, out var blockCount))
                    {
                        List<DeviceAddress> deviceAddresses = new List<DeviceAddress>();
                        int index = 11;
                        for (int i = 0; i < blockCount; i++)
                        {
                            var deviceAddress = DeserializeDeviceAddress(channel, buffer, ref index);
                            if (deviceAddress == null)
                                return null;
                            deviceAddresses.Add(deviceAddress.Value);
                        }
                        if (DeserializeTail(channel, buffer, ref index, useBCC))
                            return new CnetRegisterMonitorEachAddressRequest(stationNumber, monitorNumber, deviceAddresses, useBCC);
                    }
                }
                else
                {
                    int index = 9;
                    var deviceAddress = DeserializeDeviceAddress(channel, buffer, ref index);
                    if (deviceAddress != null)
                    {
                        if (buffer.Count < index + 2)
                            buffer.AddRange(channel.Read((uint)(index + 2 - buffer.Count), 0));
                        if (CnetMessage.TryParseByte(buffer, index, out var count))
                        {
                            index += 2;
                            if (DeserializeTail(channel, buffer, ref index, useBCC))
                                return new CnetReadAddressBlockRequest(stationNumber, deviceAddress.Value, count, useBCC);
                        }
                    }
                }
                return null;
            }
            return null;
        }

        private CnetRequest DeserializeExecuteMonitorRequest(Channel channel, byte stationNumber, byte monitorNumber, List<byte> buffer, bool useBCC)
        {
            int index = 6;
            if (DeserializeTail(channel, buffer, ref index, useBCC)
                && monitors.TryGetValue(stationNumber, out var stationMonitors)
                && stationMonitors.TryGetValue(monitorNumber, out var monitor))
            {
                return monitor.CreateExecuteRequest(useBCC);
            }
            return null;
        }


        class ChannelTask
        {
            public ChannelTask(CnetSimulationService cnetSimulator, Channel channel, bool createdFromProvider)
            {
                this.cnetSimulator = cnetSimulator;
                this.channel = channel;
                this.createdFromProvider = createdFromProvider;
            }

            private readonly CnetSimulationService cnetSimulator;
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
                                var channelTimeout = cnetSimulator.ChannelTimeout;
                                List<byte> buffer = new List<byte>();
                                if (!createdFromProvider || channelTimeout == 0)
                                {
                                    var request = cnetSimulator.DeserializeRequest(channel, buffer);
                                    if (request != null)
                                    {
                                        channel.Logger?.Log(new CnetMessageLog(channel, request, buffer.ToArray()));
                                        switch (request.Command)
                                        {
                                            case CnetCommand.Read:
                                                cnetSimulator.OnRequestedRead((CnetReadRequest)request, channel);
                                                break;
                                            case CnetCommand.Write:
                                                cnetSimulator.OnRequestedWrite((CnetWriteRequest)request, channel);
                                                break;
                                            case CnetCommand.RegisterMonitor:
                                                cnetSimulator.OnRequestedRegisterMonitor((CnetRegisterMonitorRequest)request, channel);
                                                break;
                                            case CnetCommand.ExecuteMonitor:
                                                cnetSimulator.OnRequestedExecuteMonitor((CnetExecuteMonitorRequest)request, channel);
                                                break;
                                        }
                                    }
                                }
                                else if (!Task.Run(() => cnetSimulator.DeserializeRequest(channel, buffer)).Wait(channelTimeout))
                                {
                                    cnetSimulator.channelTasks.Remove(channel);
                                }
                            }
                            catch
                            {
                                if (createdFromProvider)
                                    cnetSimulator.channelTasks.Remove(channel);
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
    /// 요청 국번 검증 이벤트 매개변수
    /// </summary>
    public sealed class ValidatingStationNumberEventArgs : EventArgs
    {
        internal ValidatingStationNumberEventArgs(byte stationNumber, Channel channel)
        {
            StationNumber = stationNumber;
            Channel = channel;
        }

        /// <summary>
        /// 국번
        /// </summary>
        public ushort StationNumber { get; }

        /// <summary>
        /// 통신 채널
        /// </summary>
        public Channel Channel { get; }

        /// <summary>
        /// 유효한 국번 여부
        /// </summary>
        public bool IsValid { get; set; }
    }

    /// <summary>
    /// Cnet 요청 발생 이벤트 매개변수
    /// </summary>
    public abstract class RequestedEventArgs : EventArgs
    {
        internal RequestedEventArgs(byte stationNumber, Channel channel)
        {
            StationNumber = stationNumber;
            Channel = channel;
        }

        internal CnetRequest request;

        /// <summary>
        /// 국번
        /// </summary>
        public byte StationNumber { get; }

        /// <summary>
        /// 통신 채널
        /// </summary>
        public Channel Channel { get; }

        /// <summary>
        /// NAK 에러 코드
        /// </summary>
        public CnetNAKCode NAKCode { get; set; }
    }

    public sealed class RequestedReadEventArgs : RequestedEventArgs
    {
        internal RequestedReadEventArgs(CnetReadRequest request, Channel channel) : base(request.StationNumber, channel)
        {
            switch (request.CommandType)
            {
                case CnetCommandType.Each:
                    ResponseValues = ((CnetReadEachAddressRequest)request).Select(deviceAddress => new DeviceAddressValue(deviceAddress)).ToArray();
                    break;
                case CnetCommandType.Block:
                    ResponseValues = ((CnetReadAddressBlockRequest)request).ToDeviceAddresses().Select(deviceAddress => new DeviceAddressValue(deviceAddress)).ToArray();
                    break;
            }
        }
        internal RequestedReadEventArgs(CnetExecuteMonitorRequest request, Channel channel) : base(request.StationNumber, channel)
        {
            switch (request.CommandType)
            {
                case CnetCommandType.Each:
                    ResponseValues = ((CnetExecuteMonitorEachAddressRequest)request).Select(deviceAddress => new DeviceAddressValue(deviceAddress)).ToArray();
                    break;
                case CnetCommandType.Block:
                    ResponseValues = ((CnetExecuteMonitorAddressBlockRequest)request).ToDeviceAddresses().Select(deviceAddress => new DeviceAddressValue(deviceAddress)).ToArray();
                    break;
            }
        }

        public IReadOnlyList<DeviceAddressValue> ResponseValues { get; }
    }

    public sealed class RequestedWriteEventArgs : RequestedEventArgs
    {
        internal RequestedWriteEventArgs(CnetWriteRequest request, Channel channel) : base(request.StationNumber, channel)
        {
            switch(request.CommandType)
            {
                case CnetCommandType.Each:
                    Values = new ReadOnlyDictionary<DeviceAddress, DeviceValue>((CnetWriteEachAddressRequest)request);
                    break;
                case CnetCommandType.Block:
                    CnetWriteAddressBlockRequest writeAddressBlockRequest = (CnetWriteAddressBlockRequest)request;
                    var values = new Dictionary<DeviceAddress, DeviceValue>();
                    foreach (var pair in writeAddressBlockRequest.ToDeviceAddresses().Zip(writeAddressBlockRequest, (address, value) => new { address, value }))
                        values[pair.address] = pair.value;
                    Values = new ReadOnlyDictionary<DeviceAddress, DeviceValue>(values);
                    break;
            }
        }

        public IReadOnlyDictionary<DeviceAddress, DeviceValue> Values { get; }
    }



}
