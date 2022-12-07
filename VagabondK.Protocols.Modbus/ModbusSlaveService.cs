﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.Modbus.Serialization;

namespace VagabondK.Protocols.Modbus
{
    /// <summary>
    /// Modbus 슬레이브 서비스
    /// </summary>
    public class ModbusSlaveService : IDisposable, IEnumerable<KeyValuePair<byte, ModbusSlave>>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        public ModbusSlaveService() { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        public ModbusSlaveService(IChannel channel)
        {
            AddChannel(channel);
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channels">통신 채널 목록</param>
        public ModbusSlaveService(IEnumerable<IChannel> channels)
        {
            foreach (var channel in channels)
                AddChannel(channel);
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="serializer">Modbus Serializer</param>
        public ModbusSlaveService(ModbusSerializer serializer)
        {
            this.serializer = serializer;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="serializer">Modbus Serializer</param>
        public ModbusSlaveService(IChannel channel, ModbusSerializer serializer)
        {
            AddChannel(channel);
            this.serializer = serializer;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channels">통신 채널 목록</param>
        /// <param name="serializer">Modbus Serializer</param>
        public ModbusSlaveService(IEnumerable<IChannel> channels, ModbusSerializer serializer)
        {
            foreach (var channel in channels)
                AddChannel(channel);
            this.serializer = serializer;
        }

        private const int maxReadRegistersLength = 125;
        private const int maxReadBooleansLength = 2008;
        private ModbusSerializer serializer;
        private readonly Dictionary<byte, ModbusSlave> modbusSlaves = new Dictionary<byte, ModbusSlave>();
        private readonly Dictionary<ModbusSlave, byte> modbusSlaveKeyMap = new Dictionary<ModbusSlave, byte>();

        /// <summary>
        /// Modbus 슬레이브 가져오기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <returns>Modbus 슬레이브</returns>
        public ModbusSlave this[byte slaveAddress]
        {
            get => modbusSlaves[slaveAddress];
            set
            {
                lock (this)
                {
                    if (value != null)
                    {
                        if (modbusSlaves.TryGetValue(slaveAddress, out var oldModbusSlave))
                            modbusSlaveKeyMap.Remove(oldModbusSlave);

                        if (oldModbusSlave != value)
                            modbusSlaves[slaveAddress] = value;

                        modbusSlaveKeyMap[value] = slaveAddress;
                    }
                    else
                        Remove(slaveAddress);
                }
            }
        }

        /// <summary>
        /// 슬레이브 주소 목록
        /// </summary>
        public Dictionary<byte, ModbusSlave>.KeyCollection SlaveAddresses { get => modbusSlaves.Keys; }

        /// <summary>
        /// Modbus 슬레이브 목록
        /// </summary>
        public Dictionary<byte, ModbusSlave>.ValueCollection ModbusSlaves { get => modbusSlaves.Values; }

        /// <summary>
        /// 슬레이브 주소 포함 여부
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <returns>Modbus 슬레이브 주소 포함 여부</returns>
        public bool ContainsSlaveAddress(byte slaveAddress) => modbusSlaves.ContainsKey(slaveAddress);

        /// <summary>
        /// Modbus 슬레이브 포함 여부
        /// </summary>
        /// <param name="modbusSlave">Modbus 슬레이브</param>
        /// <returns>Modbus 슬레이브 포함 여부</returns>
        public bool Contains(ModbusSlave modbusSlave) => modbusSlaveKeyMap.ContainsKey(modbusSlave);

        /// <summary>
        /// 슬레이브 주소 검색
        /// </summary>
        /// <param name="modbusSlave">Modbus 슬레이브</param>
        /// <returns>슬레이브 주소</returns>
        public byte? SlaveAddressOf(ModbusSlave modbusSlave)
            => modbusSlaveKeyMap.TryGetValue(modbusSlave, out var slaveAddress) ? slaveAddress : null as byte?;

        /// <summary>
        /// Modbus 슬레이브 가져오기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="modbusSlave">Modbus 슬레이브</param>
        /// <returns>Modbus 슬레이브 포함 여부</returns>
        public bool TryGetModbusSlave(byte slaveAddress, out ModbusSlave modbusSlave) => modbusSlaves.TryGetValue(slaveAddress, out modbusSlave);

        /// <summary>
        /// Modbus 슬레이브 제거
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <returns>제거 여부</returns>
        public bool Remove(byte slaveAddress)
            => modbusSlaves.TryGetValue(slaveAddress, out var oldModbusSlave)
            && modbusSlaveKeyMap.Remove(oldModbusSlave)
            && modbusSlaves.Remove(slaveAddress);

        /// <summary>
        /// Modbus Serializer
        /// </summary>
        public ModbusSerializer Serializer
        {
            get
            {
                if (serializer == null)
                    serializer = new ModbusRtuSerializer();
                return serializer;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                if (serializer != value)
                {
                    serializer = value;
                }
            }
        }

        /// <summary>
        /// 채널 유지 제한시간(밀리세컨드 단위). 이 시간 동안 요청이 발생하지 않으면 채널을 닫습니다. 기본값은 10000(10초)이고, 0이면 채널을 항상 유지합니다.
        /// </summary>
        public int ChannelTimeout { get; set; } = 10000;

        private readonly Dictionary<Channel, ChannelTask> channelTasks = new Dictionary<Channel, ChannelTask>();
        private readonly List<IChannel> channels = new List<IChannel>();

        /// <summary>
        /// 통신 채널 목록
        /// </summary>
        public IReadOnlyList<IChannel> Channels { get => channelTasks.Keys.ToList(); }

        /// <summary>
        /// 통신 채널 추가
        /// </summary>
        /// <param name="channel">통신 채널</param>
        public void AddChannel(IChannel channel)
        {
            lock (channels)
            {
                if (channel is Channel modbusChannel)
                {
                    var channelTask = new ChannelTask(this, modbusChannel, false);
                    channelTasks[modbusChannel] = channelTask;
                    channelTask.Start();
                }
                else if (channel is ChannelProvider channelProvider)
                {
                    channelProvider.Created += OnChannelCreated;
                }
                channels.Add(channel);
            }
        }

        /// <summary>
        /// 통신 채널 제거
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <returns>제거 여부</returns>
        public bool RemoveChannel(IChannel channel)
        {
            lock (channels)
            {
                if (channel is Channel modbusChannel)
                {
                    if (channelTasks.TryGetValue(modbusChannel, out var channelTask))
                    {
                        channelTask.Stop();
                        channelTasks.Remove(modbusChannel);
                    }
                }
                else if (channel is ChannelProvider channelProvider
                    && channels.Contains(channelProvider))
                {
                    channelProvider.Created -= OnChannelCreated;
                }
                return channels.Remove(channel);
            }
        }

        private void OnChannelCreated(object sender, ChannelCreatedEventArgs e)
        {
            lock (channels)
            {
                var channelTask = new ChannelTask(this, e.Channel, true);
                channelTasks[e.Channel] = channelTask;
                channelTask.Start();
            }
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            lock (channels)
            {
                foreach (var channel in channels)
                {
                    if (channel is ChannelProvider channelProvider
                        && channels.Contains(channelProvider))
                    {
                        channelProvider.Created -= OnChannelCreated;
                    }
                }
                foreach (var task in channelTasks.Values)
                {
                    task.Stop();
                }
                foreach (var channel in channels)
                {
                    channel.Dispose();
                }
            }
        }

        /// <summary>
        /// Modbus 요청 수신 처리
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="request">Modbus 요청</param>
        /// <returns>Modbus 응답</returns>
        protected virtual ModbusResponse OnReceivedModbusRequest(Channel channel, ModbusRequest request)
        {
            if (request.Address + request.Length > 0xffff)
                throw new ModbusException(ModbusExceptionCode.IllegalDataAddress);

            switch (request.Function)
            {
                case ModbusFunction.ReadCoils:
                    if (request.Length > maxReadBooleansLength)
                        throw new ModbusException(ModbusExceptionCode.IllegalDataAddress);
                    return new ModbusReadBooleanResponse(OnRequestedReadCoils((ModbusReadRequest)request, channel).Take(request.Length).ToArray(), (ModbusReadRequest)request);
                case ModbusFunction.ReadDiscreteInputs:
                    if (request.Length > maxReadBooleansLength)
                        throw new ModbusException(ModbusExceptionCode.IllegalDataAddress);
                    return new ModbusReadBooleanResponse(OnRequestedReadDiscreteInputs((ModbusReadRequest)request, channel).Take(request.Length).ToArray(), (ModbusReadRequest)request);
                case ModbusFunction.ReadHoldingRegisters:
                    if (request.Length > maxReadRegistersLength)
                        throw new ModbusException(ModbusExceptionCode.IllegalDataAddress);
                    return new ModbusReadRegisterResponse(OnRequestedReadHoldingRegisters((ModbusReadRequest)request, channel).Take(request.Length * 2).ToArray(), (ModbusReadRequest)request);
                case ModbusFunction.ReadInputRegisters:
                    if (request.Length > maxReadRegistersLength)
                        throw new ModbusException(ModbusExceptionCode.IllegalDataAddress);
                    return new ModbusReadRegisterResponse(OnRequestedReadInputRegisters((ModbusReadRequest)request, channel).Take(request.Length * 2).ToArray(), (ModbusReadRequest)request);
                case ModbusFunction.WriteSingleCoil:
                case ModbusFunction.WriteMultipleCoils:
                    OnRequestedWriteCoil((ModbusWriteCoilRequest)request, channel);
                    return new ModbusWriteResponse((ModbusWriteCoilRequest)request);
                case ModbusFunction.WriteSingleHoldingRegister:
                case ModbusFunction.WriteMultipleHoldingRegisters:
                    OnRequestedWriteHoldingRegister((ModbusWriteHoldingRegisterRequest)request, channel);
                    return new ModbusWriteResponse((ModbusWriteHoldingRegisterRequest)request);
            }

            throw new ModbusException(ModbusExceptionCode.IllegalFunction);
        }

        /// <summary>
        /// 슬레이브 주소 검증 이벤트
        /// </summary>
        public event EventHandler<ValidatingSlaveAddressEventArgs> ValidatingSlaveAddress;
        /// <summary>
        /// Coil 읽기 요청 이벤트
        /// </summary>
        public event EventHandler<RequestedReadBooleanEventArgs> RequestedReadCoils;
        /// <summary>
        /// Discrete Input 읽기 요청 이벤트
        /// </summary>
        public event EventHandler<RequestedReadBooleanEventArgs> RequestedReadDiscreteInputs;
        /// <summary>
        /// Holding Register 읽기 요청 이벤트
        /// </summary>
        public event EventHandler<RequestedReadRegisterEventArgs> RequestedReadHoldingRegisters;
        /// <summary>
        /// Input Register 읽기 요청 이벤트
        /// </summary>
        public event EventHandler<RequestedReadRegisterEventArgs> RequestedReadInputRegisters;
        /// <summary>
        /// Coil 쓰기 요청 이벤트
        /// </summary>
        public event EventHandler<RequestedWriteCoilEventArgs> RequestedWriteCoil;
        /// <summary>
        /// Holding Register 쓰기 요청 이벤트
        /// </summary>
        public event EventHandler<RequestedWriteHoldingRegisterEventArgs> RequestedWriteHoldingRegister;

        /// <summary>
        /// 슬레이브 주소 검증
        /// </summary>
        /// <param name="e">슬레이브 주소 검증 이벤트 매개변수</param>
        protected virtual void OnValidatingSlaveAddress(ValidatingSlaveAddressEventArgs e)
            => e.IsValid = modbusSlaves.Count == 0 && e.SlaveAddress == 1 || modbusSlaves.ContainsKey(e.SlaveAddress);
        /// <summary>
        /// Coil 읽기 요청 처리
        /// </summary>
        /// <param name="e">Coil 읽기 요청 발생 이벤트 매개변수</param>
        protected virtual void OnRequestedReadCoils(RequestedReadBooleanEventArgs e) 
            => e.Values = modbusSlaves.TryGetValue(e.SlaveAddress, out var modbusSlave) && modbusSlave.Coils != null ? modbusSlave.Coils.GetData(e.Address, e.Length) : throw new ModbusException(ModbusExceptionCode.IllegalFunction);
        /// <summary>
        /// Discrete Input 읽기 요청 처리
        /// </summary>
        /// <param name="e">Discrete Input 읽기 요청 발생 이벤트 매개변수</param>
        protected virtual void OnRequestedReadDiscreteInputs(RequestedReadBooleanEventArgs e)
            => e.Values = modbusSlaves.TryGetValue(e.SlaveAddress, out var modbusSlave) && modbusSlave.DiscreteInputs != null ? modbusSlave.DiscreteInputs.GetData(e.Address, e.Length) : throw new ModbusException(ModbusExceptionCode.IllegalFunction);
        /// <summary>
        /// Holding Register 읽기 요청 처리
        /// </summary>
        /// <param name="e">Holding Register 읽기 요청 발생 이벤트 매개변수</param>
        protected virtual void OnRequestedReadHoldingRegisters(RequestedReadRegisterEventArgs e)
            => e.Bytes = modbusSlaves.TryGetValue(e.SlaveAddress, out var modbusSlave) && modbusSlave.HoldingRegisters != null ? modbusSlave.HoldingRegisters.GetRawData(e.Address, e.Length * 2) : throw new ModbusException(ModbusExceptionCode.IllegalFunction);
        /// <summary>
        /// Input Register 읽기 요청 처리
        /// </summary>
        /// <param name="e">Input Register 읽기 요청 발생 이벤트 매개변수</param>
        protected virtual void OnRequestedReadInputRegisters(RequestedReadRegisterEventArgs e)
            => e.Bytes = modbusSlaves.TryGetValue(e.SlaveAddress, out var modbusSlave) && modbusSlave.InputRegisters != null ? modbusSlave.InputRegisters.GetRawData(e.Address, e.Length * 2) : throw new ModbusException(ModbusExceptionCode.IllegalFunction);
        /// <summary>
        /// Coil 쓰기 요청 처리
        /// </summary>
        /// <param name="e">Coil 쓰기 요청 발생 이벤트 매개변수</param>
        protected virtual void OnRequestedWriteCoil(RequestedWriteCoilEventArgs e)
        {
            if (modbusSlaves.TryGetValue(e.SlaveAddress, out var modbusSlave) && modbusSlave.Coils != null)
                modbusSlave.Coils.SetData(e.Address, e.Values.ToArray());
            else
                throw new ModbusException(ModbusExceptionCode.IllegalFunction);
        }
        /// <summary>
        /// Holding Register 쓰기 요청 처리
        /// </summary>
        /// <param name="e">Holding Register 쓰기 요청 발생 이벤트 매개변수</param>
        protected virtual void OnRequestedWriteHoldingRegister(RequestedWriteHoldingRegisterEventArgs e)
        {
            if (modbusSlaves.TryGetValue(e.SlaveAddress, out var modbusSlave) && modbusSlave.HoldingRegisters != null)
                modbusSlave.HoldingRegisters.SetRawData(e.Address, e.Bytes.ToArray());
            else
                throw new ModbusException(ModbusExceptionCode.IllegalFunction);
        }


        internal bool IsValidSlaveAddress(byte slaveAddress, Channel channel)
        {
            var eventArgs = new ValidatingSlaveAddressEventArgs(slaveAddress, channel);
            OnValidatingSlaveAddress(eventArgs);
            ValidatingSlaveAddress?.Invoke(this, eventArgs);

            return eventArgs.IsValid;
        }
        private IEnumerable<bool> OnRequestedReadCoils(ModbusReadRequest request, Channel channel)
            => InvokeOverrideMethodAndEvent(
                new RequestedReadBooleanEventArgs(request, channel),
                eventArgs => OnRequestedReadCoils(eventArgs),
                RequestedReadCoils).Values;
        private IEnumerable<bool> OnRequestedReadDiscreteInputs(ModbusReadRequest request, Channel channel)
            => InvokeOverrideMethodAndEvent(
                new RequestedReadBooleanEventArgs(request, channel),
                eventArgs => OnRequestedReadDiscreteInputs(eventArgs),
                RequestedReadDiscreteInputs).Values;
        private IEnumerable<byte> OnRequestedReadHoldingRegisters(ModbusReadRequest request, Channel channel)
            => InvokeOverrideMethodAndEvent(
                new RequestedReadRegisterEventArgs(request, channel),
                eventArgs => OnRequestedReadHoldingRegisters(eventArgs),
                RequestedReadHoldingRegisters).Bytes;
        private IEnumerable<byte> OnRequestedReadInputRegisters(ModbusReadRequest request, Channel channel)
            => InvokeOverrideMethodAndEvent(
                new RequestedReadRegisterEventArgs(request, channel),
                eventArgs => OnRequestedReadInputRegisters(eventArgs),
                RequestedReadInputRegisters).Bytes;
        private void OnRequestedWriteCoil(ModbusWriteCoilRequest request, Channel channel)
            => InvokeOverrideMethodAndEvent(
                new RequestedWriteCoilEventArgs(request, channel),
                eventArgs => OnRequestedWriteCoil(eventArgs),
                RequestedWriteCoil);
        private void OnRequestedWriteHoldingRegister(ModbusWriteHoldingRegisterRequest request, Channel channel)
            => InvokeOverrideMethodAndEvent(
                new RequestedWriteHoldingRegisterEventArgs(request, channel),
                eventArgs => OnRequestedWriteHoldingRegister(eventArgs),
                RequestedWriteHoldingRegister);
        
        private TEventArgs InvokeOverrideMethodAndEvent<TEventArgs>(TEventArgs eventArgs, Action<TEventArgs> action, EventHandler<TEventArgs> eventHandler)
            where TEventArgs : RequestedEventArgs
        {
            try
            {
                action(eventArgs);
                eventArgs.Succeed = true;
            }
            catch (Exception ex)
            {
                if (eventHandler == null)
                    throw ex;
            }

            eventHandler?.Invoke(this, eventArgs);

            return eventArgs;
        }

        /// <summary>
        /// Modbus 슬레이브 목록 열거
        /// </summary>
        /// <returns>Modbus 슬레이브 목록 열거</returns>
        public IEnumerator<KeyValuePair<byte, ModbusSlave>> GetEnumerator() => modbusSlaves.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        class ChannelTask
        {
            public ChannelTask(ModbusSlaveService modbusSlaveService, Channel channel, bool createdFromProvider)
            {
                this.modbusSlaveService = modbusSlaveService;
                this.channel = channel;
                this.createdFromProvider = createdFromProvider;
            }

            private readonly ModbusSlaveService modbusSlaveService;
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
                        while (isRunning && !channel.IsDisposed)
                        {
                            try
                            {
                                var channelTimeout = modbusSlaveService.ChannelTimeout;

                                RequestBuffer buffer = new RequestBuffer(modbusSlaveService, channel);

                                var serializer = modbusSlaveService.Serializer;

                                var request = serializer.Deserialize(buffer, channelTimeout);
                                if (request != null)
                                {
                                    var requestLog = new ModbusRequestLog(channel, request, buffer.ToArray(), serializer);
                                    channel.Logger?.Log(requestLog);
                                    ModbusResponse response = null;

                                    try
                                    {
                                        response = modbusSlaveService.OnReceivedModbusRequest(channel, request);
                                    }
                                    catch (ModbusException modbusException)
                                    {
                                        response = new ModbusExceptionResponse(modbusException.Code, request);
                                    }
                                    catch
                                    {
                                        response = new ModbusExceptionResponse(ModbusExceptionCode.SlaveDeviceFailure, request);
                                    }

                                    if (response != null)
                                    {
                                        var responseMessage = serializer.Serialize(response).ToArray();
                                        channel.Write(responseMessage);

                                        if (response is ModbusExceptionResponse exceptionResponse)
                                            channel?.Logger?.Log(new ModbusExceptionLog(channel, exceptionResponse, responseMessage, requestLog, serializer));
                                        else
                                            channel?.Logger?.Log(new ModbusResponseLog(channel, response, responseMessage, requestLog, serializer));
                                    }
                                }
                            }
                            catch
                            {
                                if (createdFromProvider)
                                    modbusSlaveService.RemoveChannel(channel);
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
    /// 슬레이브 주소 검증 이벤트 매개변수
    /// </summary>
    public sealed class ValidatingSlaveAddressEventArgs : EventArgs
    {
        internal ValidatingSlaveAddressEventArgs(byte slaveAddress, Channel channel)
        {
            SlaveAddress = slaveAddress;
            Channel = channel;
        }

        /// <summary>
        /// 슬레이브 주소
        /// </summary>
        public byte SlaveAddress { get; }

        /// <summary>
        /// 통신 채널
        /// </summary>
        public Channel Channel { get; }

        /// <summary>
        /// 유효한 슬레이브 주소 여부
        /// </summary>
        public bool IsValid { get; set; }
    }

    /// <summary>
    /// Modbus 요청 발생 이벤트 매개변수
    /// </summary>
    public abstract class RequestedEventArgs : EventArgs
    {
        internal RequestedEventArgs(ModbusRequest request, Channel channel)
        {
            this.request = request;
            Channel = channel;
        }

        internal ModbusRequest request;

        /// <summary>
        /// 슬레이브 주소
        /// </summary>
        public byte SlaveAddress { get => request.SlaveAddress; }

        /// <summary>
        /// Function
        /// </summary>
        public ModbusFunction Function { get => request.Function; }

        /// <summary>
        /// 데이터 주소
        /// </summary>
        public ushort Address { get => request.Address; }

        /// <summary>
        /// 통신 채널
        /// </summary>
        public Channel Channel { get; }

        /// <summary>
        /// 요청 처리 성공 여부
        /// </summary>
        public bool Succeed { get; internal set; }
    }

    /// <summary>
    /// 논리값(Coil, Discrete Input) 읽기 요청 발생 이벤트 매개변수
    /// </summary>
    public sealed class RequestedReadBooleanEventArgs : RequestedEventArgs
    {
        internal RequestedReadBooleanEventArgs(ModbusReadRequest request, Channel channel)
            : base(request, channel) { }

        /// <summary>
        /// 요청 길이
        /// </summary>
        public ushort Length { get => request.Length; }

        /// <summary>
        /// 응답할 논리값(Coil, Discrete Input) 목록
        /// </summary>
        public IEnumerable<bool> Values { get; set; }
    }

    /// <summary>
    /// 레지스터(Holding, Input) 읽기 요청 발생 이벤트 매개변수
    /// </summary>
    public sealed class RequestedReadRegisterEventArgs : RequestedEventArgs
    {
        internal RequestedReadRegisterEventArgs(ModbusReadRequest request, Channel channel)
            : base(request, channel) { }

        /// <summary>
        /// 요청 길이
        /// </summary>
        public ushort Length { get => request.Length; }

        /// <summary>
        /// 응답할 레지스터(Holding, Input)의 Raw 바이트 목록
        /// </summary>
        public IEnumerable<byte> Bytes { get; set; }
    }

    /// <summary>
    /// Coil 쓰기 요청 발생 이벤트 매개변수
    /// </summary>
    public sealed class RequestedWriteCoilEventArgs : RequestedEventArgs
    {
        internal RequestedWriteCoilEventArgs(ModbusWriteCoilRequest request, Channel channel)
            : base(request, channel)
        {
            Values = request.Values;
        }

        /// <summary>
        /// 받은 논리값(Coil, Discrete Input) 목록
        /// </summary>
        public IReadOnlyList<bool> Values { get; }
    }

    /// <summary>
    /// Holding Register 쓰기 요청 발생 이벤트 매개변수
    /// </summary>
    public sealed class RequestedWriteHoldingRegisterEventArgs : RequestedEventArgs
    {
        internal RequestedWriteHoldingRegisterEventArgs(ModbusWriteHoldingRegisterRequest request, Channel channel)
            : base(request, channel)
        {
            Bytes = request.Bytes;
        }

        private IReadOnlyList<ushort> registers;

        /// <summary>
        /// 받은 레지스터(Holding, Input)의 Raw 바이트 목록
        /// </summary>
        public IReadOnlyList<byte> Bytes { get; }

        /// <summary>
        /// 받은 레지스터(Holding, Input) 목록
        /// </summary>
        public IReadOnlyList<ushort> Registers
        {
            get
            {
                if (registers == null)
                {
                    var bytes = Bytes;
                    registers = Enumerable.Range(0, bytes.Count / 2).Select(i => (ushort)(bytes[i * 2] << 8 | bytes[i * 2 + 1])).ToArray();
                }
                return registers;
            }
        }
    }
}
