using System;
using System.Collections.Generic;
using System.Linq;
using VagabondK.Protocols.Modbus.Logging;

namespace VagabondK.Protocols.Modbus
{
    /// <summary>
    /// Modbus 요청
    /// </summary>
    public abstract class ModbusRequest : IModbusMessage, IRequest<ModbusCommErrorCode>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="function">Function</param>
        /// <param name="address">데이터 주소</param>
        protected ModbusRequest(byte slaveAddress, ModbusFunction function, ushort address)
        {
            SlaveAddress = slaveAddress;
            Function = function;
            Address = address;
        }

        /// <summary>
        /// Modbus Object 형식
        /// </summary>
        public abstract ModbusObjectType ObjectType { get; }
        /// <summary>
        /// 슬레이브 주소
        /// </summary>
        public byte SlaveAddress { get; set; }
        /// <summary>
        /// Function
        /// </summary>
        public ModbusFunction Function { get; }
        /// <summary>
        /// 데이터 주소
        /// </summary>
        public ushort Address { get; set; }
        /// <summary>
        /// 요청 길이
        /// </summary>
        public abstract ushort Length { get; }

        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 결과 바이트 열거</returns>
        public abstract IEnumerable<byte> Serialize();

        /// <summary>
        /// 트랜잭션 ID (Modbus TCP에서 사용)
        /// </summary>
        public ushort TransactionID { get; set; }

        /// <summary>
        /// Modbus Log 카테고리
        /// </summary>
        public abstract ModbusLogCategory LogCategory { get; }
    }

    /// <summary>
    /// Modbus 읽기 요청
    /// </summary>
    public class ModbusReadRequest : ModbusRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="objectType">Modbus Object 형식</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="length">요청 길이</param>
        public ModbusReadRequest(byte slaveAddress, ModbusObjectType objectType, ushort address, ushort length)
            : base(slaveAddress, (ModbusFunction)objectType, address)
        {
            Length = length;
        }

        /// <summary>
        /// Modbus Object 형식
        /// </summary>
        public override ModbusObjectType ObjectType { get => (ModbusObjectType)Function; }
        /// <summary>
        /// 요청 길이
        /// </summary>
        public override ushort Length { get; }

        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 결과 바이트 열거</returns>
        public override IEnumerable<byte> Serialize()
        {
            yield return SlaveAddress;
            yield return (byte)Function;
            yield return (byte)((Address >> 8) & 0xff);
            yield return (byte)(Address & 0xff);
            yield return (byte)((Length >> 8) & 0xff);
            yield return (byte)(Length & 0xff);
        }

        /// <summary>
        /// Modbus Log 카테고리
        /// </summary>
        public override ModbusLogCategory LogCategory
        {
            get
            {
                switch (ObjectType)
                {
                    case ModbusObjectType.Coil:
                        return ModbusLogCategory.RequestReadCoil;
                    case ModbusObjectType.DiscreteInput:
                        return ModbusLogCategory.RequestReadDiscreteInput;
                    case ModbusObjectType.HoldingRegister:
                        return ModbusLogCategory.RequestReadHoldingRegister;
                    case ModbusObjectType.InputRegister:
                        return ModbusLogCategory.RequestReadInputRegister;
                    default:
                        return ModbusLogCategory.None;
                }
            }
        }
    }

    /// <summary>
    /// Modbus 쓰기 요청
    /// </summary>
    public abstract class ModbusWriteRequest : ModbusRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="function">Function</param>
        /// <param name="address">데이터 주소</param>
        protected ModbusWriteRequest(byte slaveAddress, ModbusFunction function, ushort address) : base(slaveAddress, function, address) { }
    }

    /// <summary>
    /// Modbus Coil 쓰기 요청
    /// </summary>
    public class ModbusWriteCoilRequest : ModbusWriteRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        public ModbusWriteCoilRequest(byte slaveAddress, ushort address)
            : base(slaveAddress, ModbusFunction.WriteSingleCoil, address)
        {
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">Coil 값</param>
        public ModbusWriteCoilRequest(byte slaveAddress, ushort address, bool value)
            : base(slaveAddress, ModbusFunction.WriteSingleCoil, address)
        {
            Values = new List<bool> { value };
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="values">Coil 값 목록</param>
        public ModbusWriteCoilRequest(byte slaveAddress, ushort address, IEnumerable<bool> values)
            : base(slaveAddress, ModbusFunction.WriteMultipleCoils, address)
        {
            Values = values as List<bool> ?? values.ToList();
            byteLength = (byte)Math.Ceiling(Length / 8d);
        }

        /// <summary>
        /// 단일 논리값
        /// </summary>
        public bool SingleBooleanValue => Values != null && Values.Count> 0 ? Values[0] : throw new ErrorCodeException<ModbusExceptionCode>(ModbusExceptionCode.IllegalDataValue);
        /// <summary>
        /// 다중 논리값 목록
        /// </summary>
        public List<bool> Values { get; }
        /// <summary>
        /// 길이
        /// </summary>
        public override ushort Length => (ushort)(Values?.Count ?? throw new ErrorCodeException<ModbusExceptionCode>(ModbusExceptionCode.IllegalDataValue));
        private readonly byte byteLength = 0;

        /// <summary>
        /// Modbus Object 형식
        /// </summary>
        public override ModbusObjectType ObjectType { get => ModbusObjectType.Coil; }

        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 결과 바이트 열거</returns>
        public override IEnumerable<byte> Serialize()
        {
            yield return SlaveAddress;
            yield return (byte)Function;
            yield return (byte)((Address >> 8) & 0xff);
            yield return (byte)(Address & 0xff);

            switch (Function)
            {
                case ModbusFunction.WriteSingleCoil:
                    yield return SingleBooleanValue ? (byte)0xff : (byte)0x00;
                    yield return 0x00;
                    break;
                case ModbusFunction.WriteMultipleCoils:
                    yield return (byte)((Length >> 8) & 0xff);
                    yield return (byte)(Length & 0xff);
                    yield return byteLength;

                    int i = 0;
                    int byteValue = 0;
                    foreach (var bit in Values)
                    {
                        if (bit)
                            byteValue |= 1 << i;
                        i++;
                        if (i >= 8)
                        {
                            i = 0;
                            yield return (byte)byteValue;
                        }
                    }

                    if (i < 8)
                        yield return (byte)byteValue;
                    break;
            }
        }

        /// <summary>
        /// Modbus Log 카테고리
        /// </summary>
        public override ModbusLogCategory LogCategory
        {
            get => Function == ModbusFunction.WriteMultipleCoils 
                ? ModbusLogCategory.RequestWriteMultiCoil 
                : ModbusLogCategory.RequestWriteSingleCoil;
        }
    }

    /// <summary>
    /// Modbus Holding Register 쓰기 요청
    /// </summary>
    public class ModbusWriteHoldingRegisterRequest : ModbusWriteRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="value">Holding Register 값</param>
        public ModbusWriteHoldingRegisterRequest(byte slaveAddress, ushort address, ushort value)
            : base(slaveAddress, ModbusFunction.WriteSingleHoldingRegister, address)
        {
            Bytes = new List<byte> { (byte)((value >> 8) & 0xff), (byte)(value & 0xff) };
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="bytes">Holding Register 값들의 Raw 바이트 목록</param>
        public ModbusWriteHoldingRegisterRequest(byte slaveAddress, ushort address, IEnumerable<byte> bytes)
            : base(slaveAddress, ModbusFunction.WriteMultipleHoldingRegisters, address)
        {
            Bytes = bytes as List<byte> ?? bytes.ToList();
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="values">Holding Register 값 목록</param>
        public ModbusWriteHoldingRegisterRequest(byte slaveAddress, ushort address, IEnumerable<ushort> values)
            : base(slaveAddress, ModbusFunction.WriteMultipleHoldingRegisters, address)
        {
            Bytes = values.SelectMany(register => new byte[] { (byte)((register >> 8) & 0xff), (byte)(register & 0xff) }).ToList();
        }

        /// <summary>
        /// 단일 Holding Register 값
        /// </summary>
        public ushort SingleRegisterValue => Bytes.Count >= 2 ?
            (ushort)(Bytes[0] << 8 | Bytes[1]) : throw new ErrorCodeException<ModbusExceptionCode>(ModbusExceptionCode.IllegalDataValue);
        /// <summary>
        /// Holding Register 값들의 Raw 바이트 목록
        /// </summary>
        public List<byte> Bytes { get; }
        /// <summary>
        /// 길이
        /// </summary>
        public override ushort Length => (ushort)Math.Ceiling(Bytes.Count / 2d);

        /// <summary>
        /// Modbus Object 형식
        /// </summary>
        public override ModbusObjectType ObjectType { get => ModbusObjectType.HoldingRegister; }

        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 결과 바이트 열거</returns>
        public override IEnumerable<byte> Serialize()
        {
            if (Bytes.Count < 2)
                throw new ErrorCodeException<ModbusExceptionCode>(ModbusExceptionCode.IllegalDataValue);

            yield return SlaveAddress;
            yield return (byte)Function;
            yield return (byte)((Address >> 8) & 0xff);
            yield return (byte)(Address & 0xff);

            byte byteLength = (byte)(Math.Ceiling(Bytes.Count / 2d) * 2);

            switch (Function)
            {
                case ModbusFunction.WriteSingleHoldingRegister:
                    yield return Bytes[0];
                    yield return Bytes[1];
                    break;
                case ModbusFunction.WriteMultipleHoldingRegisters:
                    yield return (byte)((Length >> 8) & 0xff);
                    yield return (byte)(Length & 0xff);
                    yield return byteLength;

                    int i = 0;
                    foreach (var b in Bytes)
                    {
                        yield return b;
                        i++;
                    }

                    if (i % 2 == 1)
                        yield return 0;
                    break;
            }
        }

        /// <summary>
        /// Modbus Log 카테고리
        /// </summary>
        public override ModbusLogCategory LogCategory
        {
            get => Function == ModbusFunction.WriteMultipleHoldingRegisters
                ? ModbusLogCategory.RequestWriteMultiHoldingRegister
                : ModbusLogCategory.RequestWriteSingleHoldingRegister;
        }
    }

}
