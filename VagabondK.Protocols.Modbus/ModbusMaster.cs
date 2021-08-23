using System;
using System.Collections.Generic;
using System.Linq;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.Modbus.Data;
using VagabondK.Protocols.Modbus.Serialization;

namespace VagabondK.Protocols.Modbus
{
    /// <summary>
    /// Modbus 마스터
    /// </summary>
    public class ModbusMaster : IDisposable
    {
        /// <summary>
        /// 생성자
        /// </summary>
        public ModbusMaster() { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        public ModbusMaster(IChannel channel)
        {
            this.channel = channel;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="serializer">Modbus Serializer</param>
        public ModbusMaster(ModbusSerializer serializer)
        {
            this.serializer = serializer;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="serializer">Modbus Serializer</param>
        public ModbusMaster(IChannel channel, ModbusSerializer serializer)
        {
            this.channel = channel;
            this.serializer = serializer;
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            if (serializer != null)
                serializer.Unrecognized -= OnReceivedUnrecognizedMessage;
            channel?.Dispose();
        }

        private ModbusSerializer serializer;
        private IChannel channel;

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
                if (serializer != value)
                {
                    if (serializer != null)
                        serializer.Unrecognized -= OnReceivedUnrecognizedMessage;

                    serializer = value;

                    if (serializer != null)
                        serializer.Unrecognized += OnReceivedUnrecognizedMessage;
                }
            }
        }

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
        /// Modbus Exception에 대한 예외 발생 여부
        /// </summary>
        public bool ThrowsModbusExceptions { get; set; } = true;

        private void OnReceivedUnrecognizedMessage(object sender, UnrecognizedEventArgs e)
        {
            e?.Channel?.Logger?.Log(new UnrecognizedErrorLog(e.Channel, e.UnrecognizedMessage.ToArray()));
        }

        /// <summary>
        /// Modbus 요청하기
        /// </summary>
        /// <param name="request">Modbus 요청</param>
        /// <returns>Modbus 응답</returns>
        public ModbusResponse Request(ModbusRequest request) => Request(request, Timeout);

        /// <summary>
        /// Modbus 요청하기
        /// </summary>
        /// <param name="request">Modbus 요청</param>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <returns>Modbus 응답</returns>
        public ModbusResponse Request(ModbusRequest request, int timeout)
        {
            Channel channel = (Channel as Channel) ?? (Channel as ChannelProvider)?.PrimaryChannel;

            if (channel == null)
                throw new ArgumentNullException(nameof(Channel));

            if (Serializer == null)
                throw new RequestException<ModbusCommErrorCode>(ModbusCommErrorCode.NotDefinedModbusSerializer, new byte[0], request);


            var requestMessage = Serializer.Serialize(request).ToArray();
            var buffer = new ResponseBuffer(channel);

            if (!(Serializer is ModbusTcpSerializer))
            {
                channel.ReadAllRemain().ToArray();
            }

            channel.Write(requestMessage);
            var requestLog = new ChannelRequestLog(channel, request, requestMessage);
            channel?.Logger?.Log(requestLog);

            ModbusResponse result;
            try
            {
                result = Serializer.Deserialize(buffer, request, timeout);
            }
            catch (RequestException<ModbusCommErrorCode> ex)
            {
                channel?.Logger?.Log(new ChannelErrorLog(channel, ex));
                throw ex;
            }

            if (result is ModbusExceptionResponse exceptionResponse)
            {
                channel?.Logger?.Log(new ModbusExceptionLog(channel, exceptionResponse, buffer.ToArray(), requestLog));
                if (ThrowsModbusExceptions)
                    throw new ErrorCodeException<ModbusExceptionCode>(exceptionResponse.ExceptionCode);
            }
            else
                channel?.Logger?.Log(new ChannelResponseLog(channel, result, result is ModbusCommErrorResponse ? null : buffer.ToArray(), requestLog));


            return result;
        }

        /// <summary>
        /// 다중 Coil 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="length">길이</param>
        /// <returns>Modbus 논리값 읽기 응답</returns>
        public ModbusReadBooleanResponse ReadCoils(byte slaveAddress, ushort address, ushort length) => ReadCoils(slaveAddress, address, length, Timeout);
        /// <summary>
        /// 다중 Discrete Input 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="length">길이</param>
        /// <returns>Modbus 논리값 읽기 응답</returns>
        public ModbusReadBooleanResponse ReadDiscreteInputs(byte slaveAddress, ushort address, ushort length) => ReadDiscreteInputs(slaveAddress, address, length, Timeout);
        /// <summary>
        /// 다중 Holding Register 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="length">길이</param>
        /// <returns>Modbus 레지스터 읽기 응답</returns>
        public ModbusReadRegisterResponse ReadHoldingRegisters(byte slaveAddress, ushort address, ushort length) => ReadHoldingRegisters(slaveAddress, address, length, Timeout);
        /// <summary>
        /// 다중 Input Register 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="length">길이</param>
        /// <returns>Modbus 레지스터 읽기 응답</returns>
        public ModbusReadRegisterResponse ReadInputRegisters(byte slaveAddress, ushort address, ushort length) => ReadInputRegisters(slaveAddress, address, length, Timeout);
        /// <summary>
        /// 다중 Holding Register를 Raw 바이트 배열로 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="length">길이</param>
        /// <returns>Holding Register들의 Raw 바이트 배열</returns>
        public byte[] ReadHoldingRegisterBytes(byte slaveAddress, ushort address, ushort length) => ReadHoldingRegisterBytes(slaveAddress, address, length, Timeout);
        /// <summary>
        /// 다중 Input Register를 Raw 바이트 배열로 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="length">길이</param>
        /// <returns>Input Register들의 Raw 바이트 배열</returns>
        public byte[] ReadInputRegisterBytes(byte slaveAddress, ushort address, ushort length) => ReadInputRegisterBytes(slaveAddress, address, length, Timeout);
        /// <summary>
        /// 다중 Coil 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="values">보낼 Coil 값 목록</param>
        public void WriteCoils(byte slaveAddress, ushort address, IEnumerable<bool> values) => WriteCoils(slaveAddress, address, values, Timeout);
        /// <summary>
        /// 다중 Holding Register 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="values">보낼 Holding Register 값 목록</param>
        public void WriteHoldingRegisters(byte slaveAddress, ushort address, IEnumerable<ushort> values) => WriteHoldingRegisters(slaveAddress, address, values, Timeout);
        /// <summary>
        /// 다중 Holding Register를 Raw 바이트 배열로 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="bytes">보낼 Holding Register 값들의 Raw 바이트 목록</param>
        public void WriteHoldingRegisterBytes(byte slaveAddress, ushort address, IEnumerable<byte> bytes) => WriteHoldingRegisterBytes(slaveAddress, address, bytes, Timeout);

        /// <summary>
        /// 단일 Coil 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Coil 값</returns>
        public bool? ReadCoil(byte slaveAddress, ushort address) => ReadCoil(slaveAddress, address, Timeout);
        /// <summary>
        /// 단일 Discrete Input 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Discrete Input 값</returns>
        public bool? ReadDiscreteInput(byte slaveAddress, ushort address) => ReadDiscreteInput(slaveAddress, address, Timeout);
        /// <summary>
        /// 단일 Holding Register 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Holding Register 값</returns>
        public ushort? ReadHoldingRegister(byte slaveAddress, ushort address) => ReadHoldingRegister(slaveAddress, address, Timeout);
        /// <summary>
        /// 단일 Input Register 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Input Register 값</returns>
        public ushort? ReadInputRegister(byte slaveAddress, ushort address) => ReadInputRegister(slaveAddress, address, Timeout);
        /// <summary>
        /// 단일 Coil 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">보낼 Coil 값</param>
        public void WriteCoil(byte slaveAddress, ushort address, bool value) => WriteCoil(slaveAddress, address, value, Timeout);
        /// <summary>
        /// 단일 Holding Register 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">보낼 Holding Register 값</param>
        public void WriteHoldingRegister(byte slaveAddress, ushort address, ushort value) => WriteHoldingRegister(slaveAddress, address, value, Timeout);

        /// <summary>
        /// Input Register에서 부호 있는 2바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        /// <returns>Input Register의 부호 있는 2바이트 정수 값</returns>
        public short ReadInt16FromInputRegisters(byte slaveAddress, ushort address, bool isBigEndian) => ReadInt16FromInputRegisters(slaveAddress, address, isBigEndian, Timeout);
        /// <summary>
        /// Input Register에서 부호 없는 2바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        /// <returns>Input Register의 부호 없는 2바이트 정수 값</returns>
        public ushort ReadUInt16FromInputRegisters(byte slaveAddress, ushort address, bool isBigEndian) => ReadUInt16FromInputRegisters(slaveAddress, address, isBigEndian, Timeout);
        /// <summary>
        /// Input Register에서 부호 있는 4바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>Input Register의 부호 있는 4바이트 정수 값</returns>
        public int ReadInt32FromInputRegisters(byte slaveAddress, ushort address, ModbusEndian endian) => ReadInt32FromInputRegisters(slaveAddress, address, endian, Timeout);
        /// <summary>
        /// Input Register에서 부호 없는 4바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>Input Register의 부호 없는 4바이트 정수 값</returns>
        public uint ReadUInt32FromInputRegisters(byte slaveAddress, ushort address, ModbusEndian endian) => ReadUInt32FromInputRegisters(slaveAddress, address, endian, Timeout);
        /// <summary>
        /// Input Register에서 부호 있는 8바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>Input Register의 부호 있는 8바이트 정수 값</returns>
        public long ReadInt64FromInputRegisters(byte slaveAddress, ushort address, ModbusEndian endian) => ReadInt64FromInputRegisters(slaveAddress, address, endian, Timeout);
        /// <summary>
        /// Input Register에서 부호 없는 8바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>Input Register의 부호 없는 8바이트 정수 값</returns>
        public ulong ReadUInt64FromInputRegisters(byte slaveAddress, ushort address, ModbusEndian endian) => ReadUInt64FromInputRegisters(slaveAddress, address, endian, Timeout);
        /// <summary>
        /// Input Register에서 4바이트 실수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>Input Register의 4바이트 실수 값</returns>
        public float ReadSingleFromInputRegisters(byte slaveAddress, ushort address, ModbusEndian endian) => ReadSingleFromInputRegisters(slaveAddress, address, endian, Timeout);
        /// <summary>
        /// Input Register에서 8바이트 실수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>Input Register의 8바이트 실수 값</returns>
        public double ReadDoubleFromInputRegisters(byte slaveAddress, ushort address, ModbusEndian endian) => ReadDoubleFromInputRegisters(slaveAddress, address, endian, Timeout);

        /// <summary>
        /// Input Register에서 부호 있는 2바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Input Register의 부호 있는 2바이트 정수 값</returns>
        public short ReadInt16FromInputRegisters(byte slaveAddress, ushort address) => ReadInt16FromInputRegisters(slaveAddress, address, true, Timeout);
        /// <summary>
        /// Input Register에서 부호 없는 2바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Input Register의 부호 없는 2바이트 정수 값</returns>
        public ushort ReadUInt16FromInputRegisters(byte slaveAddress, ushort address) => ReadUInt16FromInputRegisters(slaveAddress, address, true, Timeout);
        /// <summary>
        /// Input Register에서 부호 있는 4바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Input Register의 부호 있는 4바이트 정수 값</returns>
        public int ReadInt32FromInputRegisters(byte slaveAddress, ushort address) => ReadInt32FromInputRegisters(slaveAddress, address, new ModbusEndian(true), Timeout);
        /// <summary>
        /// Input Register에서 부호 없는 4바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Input Register의 부호 없는 4바이트 정수 값</returns>
        public uint ReadUInt32FromInputRegisters(byte slaveAddress, ushort address) => ReadUInt32FromInputRegisters(slaveAddress, address, new ModbusEndian(true), Timeout);
        /// <summary>
        /// Input Register에서 부호 있는 8바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Input Register의 부호 있는 8바이트 정수 값</returns>
        public long ReadInt64FromInputRegisters(byte slaveAddress, ushort address) => ReadInt64FromInputRegisters(slaveAddress, address, new ModbusEndian(true), Timeout);
        /// <summary>
        /// Input Register에서 부호 없는 8바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Input Register의 부호 없는 8바이트 정수 값</returns>
        public ulong ReadUInt64FromInputRegisters(byte slaveAddress, ushort address) => ReadUInt64FromInputRegisters(slaveAddress, address, new ModbusEndian(true), Timeout);
        /// <summary>
        /// Input Register에서 4바이트 실수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Input Register의 4바이트 실수 값</returns>
        public float ReadSingleFromInputRegisters(byte slaveAddress, ushort address) => ReadSingleFromInputRegisters(slaveAddress, address, new ModbusEndian(true), Timeout);
        /// <summary>
        /// Input Register에서 8바이트 실수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Input Register의 8바이트 실수 값</returns>
        public double ReadDoubleFromInputRegisters(byte slaveAddress, ushort address) => ReadDoubleFromInputRegisters(slaveAddress, address, new ModbusEndian(true), Timeout);




        /// <summary>
        /// Holding Register에서 부호 있는 2바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        /// <returns>Holding Register의 부호 있는 2바이트 정수 값</returns>
        public short ReadInt16FromHoldingRegisters(byte slaveAddress, ushort address, bool isBigEndian) => ReadInt16FromHoldingRegisters(slaveAddress, address, isBigEndian, Timeout);
        /// <summary>
        /// Holding Register에서 부호 없는 2바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        /// <returns>Holding Register의 부호 없는 2바이트 정수 값</returns>
        public ushort ReadUInt16FromHoldingRegisters(byte slaveAddress, ushort address, bool isBigEndian) => ReadUInt16FromHoldingRegisters(slaveAddress, address, isBigEndian, Timeout);
        /// <summary>
        /// Holding Register에서 부호 있는 4바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>Holding Register의 부호 있는 4바이트 정수 값</returns>
        public int ReadInt32FromHoldingRegisters(byte slaveAddress, ushort address, ModbusEndian endian) => ReadInt32FromHoldingRegisters(slaveAddress, address, endian, Timeout);
        /// <summary>
        /// Holding Register에서 부호 없는 4바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>Holding Register의 부호 없는 4바이트 정수 값</returns>
        public uint ReadUInt32FromHoldingRegisters(byte slaveAddress, ushort address, ModbusEndian endian) => ReadUInt32FromHoldingRegisters(slaveAddress, address, endian, Timeout);
        /// <summary>
        /// Holding Register에서 부호 있는 8바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>Holding Register의 부호 있는 8바이트 정수 값</returns>
        public long ReadInt64FromHoldingRegisters(byte slaveAddress, ushort address, ModbusEndian endian) => ReadInt64FromHoldingRegisters(slaveAddress, address, endian, Timeout);
        /// <summary>
        /// Holding Register에서 부호 없는 8바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>Holding Register의 부호 없는 8바이트 정수 값</returns>
        public ulong ReadUInt64FromHoldingRegisters(byte slaveAddress, ushort address, ModbusEndian endian) => ReadUInt64FromHoldingRegisters(slaveAddress, address, endian, Timeout);
        /// <summary>
        /// Holding Register에서 4바이트 실수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>Holding Register의 4바이트 실수 값</returns>
        public float ReadSingleFromHoldingRegisters(byte slaveAddress, ushort address, ModbusEndian endian) => ReadSingleFromHoldingRegisters(slaveAddress, address, endian, Timeout);
        /// <summary>
        /// Holding Register에서 8바이트 실수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>Holding Register의 8바이트 실수 값</returns>
        public double ReadDoubleFromHoldingRegisters(byte slaveAddress, ushort address, ModbusEndian endian) => ReadDoubleFromHoldingRegisters(slaveAddress, address, endian, Timeout);

        /// <summary>
        /// Holding Register에서 부호 있는 2바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Holding Register의 부호 있는 2바이트 정수 값</returns>
        public short ReadInt16FromHoldingRegisters(byte slaveAddress, ushort address) => ReadInt16FromHoldingRegisters(slaveAddress, address, true, Timeout);
        /// <summary>
        /// Holding Register에서 부호 없는 2바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Holding Register의 부호 없는 2바이트 정수 값</returns>
        public ushort ReadUInt16FromHoldingRegisters(byte slaveAddress, ushort address) => ReadUInt16FromHoldingRegisters(slaveAddress, address, true, Timeout);
        /// <summary>
        /// Holding Register에서 부호 있는 4바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Holding Register의 부호 있는 4바이트 정수 값</returns>
        public int ReadInt32FromHoldingRegisters(byte slaveAddress, ushort address) => ReadInt32FromHoldingRegisters(slaveAddress, address, new ModbusEndian(true), Timeout);
        /// <summary>
        /// Holding Register에서 부호 없는 4바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Holding Register의 부호 없는 4바이트 정수 값</returns>
        public uint ReadUInt32FromHoldingRegisters(byte slaveAddress, ushort address) => ReadUInt32FromHoldingRegisters(slaveAddress, address, new ModbusEndian(true), Timeout);
        /// <summary>
        /// Holding Register에서 부호 있는 8바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Holding Register의 부호 있는 8바이트 정수 값</returns>
        public long ReadInt64FromHoldingRegisters(byte slaveAddress, ushort address) => ReadInt64FromHoldingRegisters(slaveAddress, address, new ModbusEndian(true), Timeout);
        /// <summary>
        /// Holding Register에서 부호 없는 8바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Holding Register의 부호 없는 8바이트 정수 값</returns>
        public ulong ReadUInt64FromHoldingRegisters(byte slaveAddress, ushort address) => ReadUInt64FromHoldingRegisters(slaveAddress, address, new ModbusEndian(true), Timeout);
        /// <summary>
        /// Holding Register에서 4바이트 실수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Holding Register의 4바이트 실수 값</returns>
        public float ReadSingleFromHoldingRegisters(byte slaveAddress, ushort address) => ReadSingleFromHoldingRegisters(slaveAddress, address, new ModbusEndian(true), Timeout);
        /// <summary>
        /// Holding Register에서 8바이트 실수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <returns>Holding Register의 8바이트 실수 값</returns>
        public double ReadDoubleFromHoldingRegisters(byte slaveAddress, ushort address) => ReadDoubleFromHoldingRegisters(slaveAddress, address, new ModbusEndian(true), Timeout);

        /// <summary>
        /// 부호 있는 2바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        public void Write(byte slaveAddress, ushort address, short value, bool isBigEndian) => WriteHoldingRegisterBytes(slaveAddress, address, new ModbusEndian(isBigEndian).Sort(BitConverter.GetBytes(value)), Timeout);
        /// <summary>
        /// 부호 없는 2바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        public void Write(byte slaveAddress, ushort address, ushort value, bool isBigEndian) => WriteHoldingRegisterBytes(slaveAddress, address, new ModbusEndian(isBigEndian).Sort(BitConverter.GetBytes(value)), Timeout);
        /// <summary>
        /// 부호 있는 4바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="endian">엔디안</param>
        public void Write(byte slaveAddress, ushort address, int value, ModbusEndian endian) => WriteHoldingRegisterBytes(slaveAddress, address, endian.Sort(BitConverter.GetBytes(value)), Timeout);
        /// <summary>
        /// 부호 없는 4바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="endian">엔디안</param>
        public void Write(byte slaveAddress, ushort address, uint value, ModbusEndian endian) => WriteHoldingRegisterBytes(slaveAddress, address, endian.Sort(BitConverter.GetBytes(value)), Timeout);
        /// <summary>
        /// 부호 있는 8바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="endian">엔디안</param>
        public void Write(byte slaveAddress, ushort address, long value, ModbusEndian endian) => WriteHoldingRegisterBytes(slaveAddress, address, endian.Sort(BitConverter.GetBytes(value)), Timeout);
        /// <summary>
        /// 부호 없는 8바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="endian">엔디안</param>
        public void Write(byte slaveAddress, ushort address, ulong value, ModbusEndian endian) => WriteHoldingRegisterBytes(slaveAddress, address, endian.Sort(BitConverter.GetBytes(value)), Timeout);
        /// <summary>
        /// 4바이트 실수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="endian">엔디안</param>
        public void Write(byte slaveAddress, ushort address, float value, ModbusEndian endian) => WriteHoldingRegisterBytes(slaveAddress, address, endian.Sort(BitConverter.GetBytes(value)), Timeout);
        /// <summary>
        /// 8바이트 실수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="endian">엔디안</param>
        public void Write(byte slaveAddress, ushort address, double value, ModbusEndian endian) => WriteHoldingRegisterBytes(slaveAddress, address, endian.Sort(BitConverter.GetBytes(value)), Timeout);

        /// <summary>
        /// 부호 있는 2바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        public void Write(byte slaveAddress, ushort address, short value) => Write(slaveAddress, address, value, true, Timeout);
        /// <summary>
        /// 부호 없는 2바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        public void Write(byte slaveAddress, ushort address, ushort value) => Write(slaveAddress, address, value, true, Timeout);
        /// <summary>
        /// 부호 있는 4바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        public void Write(byte slaveAddress, ushort address, int value) => Write(slaveAddress, address, value, new ModbusEndian(true), Timeout);
        /// <summary>
        /// 부호 없는 4바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        public void Write(byte slaveAddress, ushort address, uint value) => Write(slaveAddress, address, value, new ModbusEndian(true), Timeout);
        /// <summary>
        /// 부호 있는 8바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        public void Write(byte slaveAddress, ushort address, long value) => Write(slaveAddress, address, value, new ModbusEndian(true), Timeout);
        /// <summary>
        /// 부호 없는 8바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        public void Write(byte slaveAddress, ushort address, ulong value) => Write(slaveAddress, address, value, new ModbusEndian(true), Timeout);
        /// <summary>
        /// 4바이트 실수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        public void Write(byte slaveAddress, ushort address, float value) => Write(slaveAddress, address, value, new ModbusEndian(true), Timeout);
        /// <summary>
        /// 8바이트 실수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        public void Write(byte slaveAddress, ushort address, double value) => Write(slaveAddress, address, value, new ModbusEndian(true), Timeout);

        /// <summary>
        /// 다중 Coil 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="length">길이</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Modbus 논리값 읽기 응답</returns>
        public ModbusReadBooleanResponse ReadCoils(byte slaveAddress, ushort address, ushort length, int timeout) => Request(new ModbusReadRequest(slaveAddress, ModbusObjectType.Coil, address, length), timeout) as ModbusReadBooleanResponse;
        /// <summary>
        /// 다중 Discrete Input 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="length">길이</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Modbus 논리값 읽기 응답</returns>
        public ModbusReadBooleanResponse ReadDiscreteInputs(byte slaveAddress, ushort address, ushort length, int timeout) => Request(new ModbusReadRequest(slaveAddress, ModbusObjectType.DiscreteInput, address, length), timeout) as ModbusReadBooleanResponse;
        /// <summary>
        /// 다중 Holding Register 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="length">길이</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Modbus 레지스터 읽기 응답</returns>
        public ModbusReadRegisterResponse ReadHoldingRegisters(byte slaveAddress, ushort address, ushort length, int timeout) => Request(new ModbusReadRequest(slaveAddress, ModbusObjectType.HoldingRegister, address, length), timeout) as ModbusReadRegisterResponse;
        /// <summary>
        /// 다중 Input Register 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="length">길이</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Modbus 레지스터 읽기 응답</returns>
        public ModbusReadRegisterResponse ReadInputRegisters(byte slaveAddress, ushort address, ushort length, int timeout) => Request(new ModbusReadRequest(slaveAddress, ModbusObjectType.InputRegister, address, length), timeout) as ModbusReadRegisterResponse;
        /// <summary>
        /// 다중 Holding Register를 Raw 바이트 배열로 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="length">길이</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register들의 Raw 바이트 배열</returns>
        public byte[] ReadHoldingRegisterBytes(byte slaveAddress, ushort address, ushort length, int timeout) => (Request(new ModbusReadRequest(slaveAddress, ModbusObjectType.HoldingRegister, address, length), timeout) as ModbusReadRegisterResponse)?.Bytes?.ToArray();
        /// <summary>
        /// 다중 Input Register를 Raw 바이트 배열로 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="length">길이</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register들의 Raw 바이트 배열</returns>
        public byte[] ReadInputRegisterBytes(byte slaveAddress, ushort address, ushort length, int timeout) => (Request(new ModbusReadRequest(slaveAddress, ModbusObjectType.InputRegister, address, length), timeout) as ModbusReadRegisterResponse)?.Bytes?.ToArray();
        /// <summary>
        /// 다중 Coil 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="values">보낼 Coil 값 목록</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void WriteCoils(byte slaveAddress, ushort address, IEnumerable<bool> values, int timeout) => Request(new ModbusWriteCoilRequest(slaveAddress, address, values), timeout);
        /// <summary>
        /// 다중 Holding Register 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="values">보낼 Holding Register 값 목록</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void WriteHoldingRegisters(byte slaveAddress, ushort address, IEnumerable<ushort> values, int timeout) => Request(new ModbusWriteHoldingRegisterRequest(slaveAddress, address, values), timeout);
        /// <summary>
        /// 다중 Holding Register를 Raw 바이트 배열로 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="bytes">보낼 Holding Register 값들의 Raw 바이트 목록</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void WriteHoldingRegisterBytes(byte slaveAddress, ushort address, IEnumerable<byte> bytes, int timeout) => Request(new ModbusWriteHoldingRegisterRequest(slaveAddress, address, bytes), timeout);

        /// <summary>
        /// 단일 Coil 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Coil 값</returns>
        public bool? ReadCoil(byte slaveAddress, ushort address, int timeout) => (Request(new ModbusReadRequest(slaveAddress, ModbusObjectType.Coil, address, 1), timeout) as ModbusReadBooleanResponse)?.Values?.FirstOrDefault();
        /// <summary>
        /// 단일 Discrete Input 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Discrete Input 값</returns>
        public bool? ReadDiscreteInput(byte slaveAddress, ushort address, int timeout) => (Request(new ModbusReadRequest(slaveAddress, ModbusObjectType.DiscreteInput, address, 1), timeout) as ModbusReadBooleanResponse)?.Values?.FirstOrDefault();
        /// <summary>
        /// 단일 Holding Register 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register 값</returns>
        public ushort? ReadHoldingRegister(byte slaveAddress, ushort address, int timeout) => (Request(new ModbusReadRequest(slaveAddress, ModbusObjectType.HoldingRegister, address, 1), timeout) as ModbusReadRegisterResponse)?.Values?.FirstOrDefault();
        /// <summary>
        /// 단일 Input Register 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register 값</returns>
        public ushort? ReadInputRegister(byte slaveAddress, ushort address, int timeout) => (Request(new ModbusReadRequest(slaveAddress, ModbusObjectType.InputRegister, address, 1), timeout) as ModbusReadRegisterResponse)?.Values?.FirstOrDefault();
        /// <summary>
        /// 단일 Coil 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">보낼 Coil 값</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void WriteCoil(byte slaveAddress, ushort address, bool value, int timeout) => Request(new ModbusWriteCoilRequest(slaveAddress, address, value), timeout);
        /// <summary>
        /// 단일 Holding Register 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">보낼 Holding Register 값</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void WriteHoldingRegister(byte slaveAddress, ushort address, ushort value, int timeout) => Request(new ModbusWriteHoldingRegisterRequest(slaveAddress, address, value), timeout);

        /// <summary>
        /// Input Register에서 부호 있는 2바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register의 부호 있는 2바이트 정수 값</returns>
        public short ReadInt16FromInputRegisters(byte slaveAddress, ushort address, bool isBigEndian, int timeout) => BitConverter.ToInt16(new ModbusEndian(isBigEndian).Sort(ReadInputRegisterBytes(slaveAddress, address, 1, timeout)), 0);
        /// <summary>
        /// Input Register에서 부호 없는 2바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register의 부호 없는 2바이트 정수 값</returns>
        public ushort ReadUInt16FromInputRegisters(byte slaveAddress, ushort address, bool isBigEndian, int timeout) => BitConverter.ToUInt16(new ModbusEndian(isBigEndian).Sort(ReadInputRegisterBytes(slaveAddress, address, 1, timeout)), 0);
        /// <summary>
        /// Input Register에서 부호 있는 4바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register의 부호 있는 4바이트 정수 값</returns>
        public int ReadInt32FromInputRegisters(byte slaveAddress, ushort address, ModbusEndian endian, int timeout) => BitConverter.ToInt32(endian.Sort(ReadInputRegisterBytes(slaveAddress, address, 2, timeout)), 0);
        /// <summary>
        /// Input Register에서 부호 없는 4바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register의 부호 없는 4바이트 정수 값</returns>
        public uint ReadUInt32FromInputRegisters(byte slaveAddress, ushort address, ModbusEndian endian, int timeout) => BitConverter.ToUInt32(endian.Sort(ReadInputRegisterBytes(slaveAddress, address, 2, timeout)), 0);
        /// <summary>
        /// Input Register에서 부호 있는 8바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register의 부호 있는 8바이트 정수 값</returns>
        public long ReadInt64FromInputRegisters(byte slaveAddress, ushort address, ModbusEndian endian, int timeout) => BitConverter.ToInt64(endian.Sort(ReadInputRegisterBytes(slaveAddress, address, 4, timeout)), 0);
        /// <summary>
        /// Input Register에서 부호 없는 8바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register의 부호 없는 8바이트 정수 값</returns>
        public ulong ReadUInt64FromInputRegisters(byte slaveAddress, ushort address, ModbusEndian endian, int timeout) => BitConverter.ToUInt64(endian.Sort(ReadInputRegisterBytes(slaveAddress, address, 4, timeout)), 0);
        /// <summary>
        /// Input Register에서 4바이트 실수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register의 4바이트 실수 값</returns>
        public float ReadSingleFromInputRegisters(byte slaveAddress, ushort address, ModbusEndian endian, int timeout) => BitConverter.ToSingle(endian.Sort(ReadInputRegisterBytes(slaveAddress, address, 2, timeout)), 0);
        /// <summary>
        /// Input Register에서 8바이트 실수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register의 8바이트 실수 값</returns>
        public double ReadDoubleFromInputRegisters(byte slaveAddress, ushort address, ModbusEndian endian, int timeout) => BitConverter.ToDouble(endian.Sort(ReadInputRegisterBytes(slaveAddress, address, 4, timeout)), 0);

        /// <summary>
        /// Input Register에서 부호 있는 2바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register의 부호 있는 2바이트 정수 값</returns>
        public short ReadInt16FromInputRegisters(byte slaveAddress, ushort address, int timeout) => ReadInt16FromInputRegisters(slaveAddress, address, true, timeout);
        /// <summary>
        /// Input Register에서 부호 없는 2바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register의 부호 없는 2바이트 정수 값</returns>
        public ushort ReadUInt16FromInputRegisters(byte slaveAddress, ushort address, int timeout) => ReadUInt16FromInputRegisters(slaveAddress, address, true, timeout);
        /// <summary>
        /// Input Register에서 부호 있는 4바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register의 부호 있는 4바이트 정수 값</returns>
        public int ReadInt32FromInputRegisters(byte slaveAddress, ushort address, int timeout) => ReadInt32FromInputRegisters(slaveAddress, address, new ModbusEndian(true), timeout);
        /// <summary>
        /// Input Register에서 부호 없는 4바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register의 부호 없는 4바이트 정수 값</returns>
        public uint ReadUInt32FromInputRegisters(byte slaveAddress, ushort address, int timeout) => ReadUInt32FromInputRegisters(slaveAddress, address, new ModbusEndian(true), timeout);
        /// <summary>
        /// Input Register에서 부호 있는 8바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register의 부호 있는 8바이트 정수 값</returns>
        public long ReadInt64FromInputRegisters(byte slaveAddress, ushort address, int timeout) => ReadInt64FromInputRegisters(slaveAddress, address, new ModbusEndian(true), timeout);
        /// <summary>
        /// Input Register에서 부호 없는 8바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register의 부호 없는 8바이트 정수 값</returns>
        public ulong ReadUInt64FromInputRegisters(byte slaveAddress, ushort address, int timeout) => ReadUInt64FromInputRegisters(slaveAddress, address, new ModbusEndian(true), timeout);
        /// <summary>
        /// Input Register에서 4바이트 실수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register의 4바이트 실수 값</returns>
        public float ReadSingleFromInputRegisters(byte slaveAddress, ushort address, int timeout) => ReadSingleFromInputRegisters(slaveAddress, address, new ModbusEndian(true), timeout);
        /// <summary>
        /// Input Register에서 8바이트 실수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Input Register의 8바이트 실수 값</returns>
        public double ReadDoubleFromInputRegisters(byte slaveAddress, ushort address, int timeout) => ReadDoubleFromInputRegisters(slaveAddress, address, new ModbusEndian(true), timeout);

        /// <summary>
        /// Holding Register에서 부호 있는 2바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register의 부호 있는 2바이트 정수 값</returns>
        public short ReadInt16FromHoldingRegisters(byte slaveAddress, ushort address, bool isBigEndian, int timeout) => BitConverter.ToInt16(new ModbusEndian(isBigEndian).Sort(ReadHoldingRegisterBytes(slaveAddress, address, 1, timeout)), 0);
        /// <summary>
        /// Holding Register에서 부호 없는 2바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register의 부호 없는 2바이트 정수 값</returns>
        public ushort ReadUInt16FromHoldingRegisters(byte slaveAddress, ushort address, bool isBigEndian, int timeout) => BitConverter.ToUInt16(new ModbusEndian(isBigEndian).Sort(ReadHoldingRegisterBytes(slaveAddress, address, 1, timeout)), 0);
        /// <summary>
        /// Holding Register에서 부호 있는 4바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register의 부호 있는 4바이트 정수 값</returns>
        public int ReadInt32FromHoldingRegisters(byte slaveAddress, ushort address, ModbusEndian endian, int timeout) => BitConverter.ToInt32(endian.Sort(ReadHoldingRegisterBytes(slaveAddress, address, 2, timeout)), 0);
        /// <summary>
        /// Holding Register에서 부호 없는 4바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register의 부호 없는 4바이트 정수 값</returns>
        public uint ReadUInt32FromHoldingRegisters(byte slaveAddress, ushort address, ModbusEndian endian, int timeout) => BitConverter.ToUInt32(endian.Sort(ReadHoldingRegisterBytes(slaveAddress, address, 2, timeout)), 0);
        /// <summary>
        /// Holding Register에서 부호 있는 8바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register의 부호 있는 8바이트 정수 값</returns>
        public long ReadInt64FromHoldingRegisters(byte slaveAddress, ushort address, ModbusEndian endian, int timeout) => BitConverter.ToInt64(endian.Sort(ReadHoldingRegisterBytes(slaveAddress, address, 4, timeout)), 0);
        /// <summary>
        /// Holding Register에서 부호 없는 8바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register의 부호 없는 8바이트 정수 값</returns>
        public ulong ReadUInt64FromHoldingRegisters(byte slaveAddress, ushort address, ModbusEndian endian, int timeout) => BitConverter.ToUInt64(endian.Sort(ReadHoldingRegisterBytes(slaveAddress, address, 4, timeout)), 0);
        /// <summary>
        /// Holding Register에서 4바이트 실수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register의 4바이트 실수 값</returns>
        public float ReadSingleFromHoldingRegisters(byte slaveAddress, ushort address, ModbusEndian endian, int timeout) => BitConverter.ToSingle(endian.Sort(ReadHoldingRegisterBytes(slaveAddress, address, 2, timeout)), 0);
        /// <summary>
        /// Holding Register에서 8바이트 실수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register의 8바이트 실수 값</returns>
        public double ReadDoubleFromHoldingRegisters(byte slaveAddress, ushort address, ModbusEndian endian, int timeout) => BitConverter.ToDouble(endian.Sort(ReadHoldingRegisterBytes(slaveAddress, address, 4, timeout)), 0);

        /// <summary>
        /// Holding Register에서 부호 있는 2바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register의 부호 있는 2바이트 정수 값</returns>
        public short ReadInt16FromHoldingRegisters(byte slaveAddress, ushort address, int timeout) => ReadInt16FromHoldingRegisters(slaveAddress, address, true, timeout);
        /// <summary>
        /// Holding Register에서 부호 없는 2바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register의 부호 없는 2바이트 정수 값</returns>
        public ushort ReadUInt16FromHoldingRegisters(byte slaveAddress, ushort address, int timeout) => ReadUInt16FromHoldingRegisters(slaveAddress, address, true, timeout);
        /// <summary>
        /// Holding Register에서 부호 있는 4바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register의 부호 있는 4바이트 정수 값</returns>
        public int ReadInt32FromHoldingRegisters(byte slaveAddress, ushort address, int timeout) => ReadInt32FromHoldingRegisters(slaveAddress, address, new ModbusEndian(true), timeout);
        /// <summary>
        /// Holding Register에서 부호 없는 4바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register의 부호 없는 4바이트 정수 값</returns>
        public uint ReadUInt32FromHoldingRegisters(byte slaveAddress, ushort address, int timeout) => ReadUInt32FromHoldingRegisters(slaveAddress, address, new ModbusEndian(true), timeout);
        /// <summary>
        /// Holding Register에서 부호 있는 8바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register의 부호 있는 8바이트 정수 값</returns>
        public long ReadInt64FromHoldingRegisters(byte slaveAddress, ushort address, int timeout) => ReadInt64FromHoldingRegisters(slaveAddress, address, new ModbusEndian(true), timeout);
        /// <summary>
        /// Holding Register에서 부호 없는 8바이트 정수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register의 부호 없는 8바이트 정수 값</returns>
        public ulong ReadUInt64FromHoldingRegisters(byte slaveAddress, ushort address, int timeout) => ReadUInt64FromHoldingRegisters(slaveAddress, address, new ModbusEndian(true), timeout);
        /// <summary>
        /// Holding Register에서 4바이트 실수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register의 4바이트 실수 값</returns>
        public float ReadSingleFromHoldingRegisters(byte slaveAddress, ushort address, int timeout) => ReadSingleFromHoldingRegisters(slaveAddress, address, new ModbusEndian(true), timeout);
        /// <summary>
        /// Holding Register에서 8바이트 실수 값 읽기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>Holding Register의 8바이트 실수 값</returns>
        public double ReadDoubleFromHoldingRegisters(byte slaveAddress, ushort address, int timeout) => ReadDoubleFromHoldingRegisters(slaveAddress, address, new ModbusEndian(true), timeout);

        /// <summary>
        /// 부호 있는 2바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void Write(byte slaveAddress, ushort address, short value, bool isBigEndian, int timeout) => WriteHoldingRegisterBytes(slaveAddress, address, new ModbusEndian(isBigEndian).Sort(BitConverter.GetBytes(value)), timeout);
        /// <summary>
        /// 부호 없는 2바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void Write(byte slaveAddress, ushort address, ushort value, bool isBigEndian, int timeout) => WriteHoldingRegisterBytes(slaveAddress, address, new ModbusEndian(isBigEndian).Sort(BitConverter.GetBytes(value)), timeout);
        /// <summary>
        /// 부호 있는 4바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void Write(byte slaveAddress, ushort address, int value, ModbusEndian endian, int timeout) => WriteHoldingRegisterBytes(slaveAddress, address, endian.Sort(BitConverter.GetBytes(value)), timeout);
        /// <summary>
        /// 부호 없는 4바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void Write(byte slaveAddress, ushort address, uint value, ModbusEndian endian, int timeout) => WriteHoldingRegisterBytes(slaveAddress, address, endian.Sort(BitConverter.GetBytes(value)), timeout);
        /// <summary>
        /// 부호 있는 8바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void Write(byte slaveAddress, ushort address, long value, ModbusEndian endian, int timeout) => WriteHoldingRegisterBytes(slaveAddress, address, endian.Sort(BitConverter.GetBytes(value)), timeout);
        /// <summary>
        /// 부호 없는 8바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void Write(byte slaveAddress, ushort address, ulong value, ModbusEndian endian, int timeout) => WriteHoldingRegisterBytes(slaveAddress, address, endian.Sort(BitConverter.GetBytes(value)), timeout);
        /// <summary>
        /// 4바이트 실수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void Write(byte slaveAddress, ushort address, float value, ModbusEndian endian, int timeout) => WriteHoldingRegisterBytes(slaveAddress, address, endian.Sort(BitConverter.GetBytes(value)), timeout);
        /// <summary>
        /// 8바이트 실수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="endian">엔디안</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void Write(byte slaveAddress, ushort address, double value, ModbusEndian endian, int timeout) => WriteHoldingRegisterBytes(slaveAddress, address, endian.Sort(BitConverter.GetBytes(value)), timeout);

        /// <summary>
        /// 부호 있는 2바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void Write(byte slaveAddress, ushort address, short value, int timeout) => Write(slaveAddress, address, value, true, timeout);
        /// <summary>
        /// 부호 없는 2바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void Write(byte slaveAddress, ushort address, ushort value, int timeout) => Write(slaveAddress, address, value, true, timeout);
        /// <summary>
        /// 부호 있는 4바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void Write(byte slaveAddress, ushort address, int value, int timeout) => Write(slaveAddress, address, value, new ModbusEndian(true), timeout);
        /// <summary>
        /// 부호 없는 4바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void Write(byte slaveAddress, ushort address, uint value, int timeout) => Write(slaveAddress, address, value, new ModbusEndian(true), timeout);
        /// <summary>
        /// 부호 있는 8바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void Write(byte slaveAddress, ushort address, long value, int timeout) => Write(slaveAddress, address, value, new ModbusEndian(true), timeout);
        /// <summary>
        /// 부호 없는 8바이트 정수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void Write(byte slaveAddress, ushort address, ulong value, int timeout) => Write(slaveAddress, address, value, new ModbusEndian(true), timeout);
        /// <summary>
        /// 4바이트 실수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void Write(byte slaveAddress, ushort address, float value, int timeout) => Write(slaveAddress, address, value, new ModbusEndian(true), timeout);
        /// <summary>
        /// 8바이트 실수 값 쓰기
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        public void Write(byte slaveAddress, ushort address, double value, int timeout) => Write(slaveAddress, address, value, new ModbusEndian(true), timeout);

    }
}
