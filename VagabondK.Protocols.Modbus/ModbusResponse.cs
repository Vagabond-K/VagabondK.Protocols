using System;
using System.Collections.Generic;
using System.Linq;
using VagabondK.Protocols.Modbus.Data;
using VagabondK.Protocols.Modbus.Logging;

namespace VagabondK.Protocols.Modbus
{
    /// <summary>
    /// Modbus 응답
    /// </summary>
    public abstract class ModbusResponse : IModbusMessage
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="request">Modbus 요청</param>
        internal ModbusResponse(ModbusRequest request)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
        }

        /// <summary>
        /// Modbus 요청
        /// </summary>
        public ModbusRequest Request { get; private set; }

        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 된 바이트 열거</returns>
        public abstract IEnumerable<byte> Serialize();

        /// <summary>
        /// 트랜잭션 ID (Modbus TCP에서 사용)
        /// </summary>
        public ushort TransactionID { get => Request.TransactionID; set => Request.TransactionID = value; }

        /// <summary>
        /// Modbus Log 카테고리
        /// </summary>
        public abstract ModbusLogCategory LogCategory { get; }
    }

    /// <summary>
    /// Modbus 정상 응답
    /// </summary>
    public abstract class ModbusOkResponse : ModbusResponse
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="request">Modbus 요청</param>
        internal ModbusOkResponse(ModbusRequest request) : base(request) { }
    }

    /// <summary>
    /// Modbus Exception 응답
    /// </summary>
    public class ModbusExceptionResponse : ModbusOkResponse
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="exceptionCode">Modbus Exception 코드</param>
        /// <param name="request">Modbus 요청</param>
        internal ModbusExceptionResponse(ModbusExceptionCode exceptionCode, ModbusRequest request) : base(request)
        {
            ExceptionCode = exceptionCode;
        }

        /// <summary>
        /// Modbus Exception 코드
        /// </summary>
        public ModbusExceptionCode ExceptionCode { get; }

        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 된 바이트 열거</returns>
        public override IEnumerable<byte> Serialize()
        {
            yield return Request.SlaveAddress;
            yield return (byte)((int)Request.Function | 0x80);
            yield return (byte)ExceptionCode;
        }

        /// <summary>
        /// Modbus Log 카테고리
        /// </summary>
        public override ModbusLogCategory LogCategory { get => ModbusLogCategory.ResponseException; }
    }

    /// <summary>
    /// Modbus 정상 응답
    /// </summary>
    /// <typeparam name="TRequest">Modbus 요청 형식</typeparam>
    public abstract class ModbusOkResponse<TRequest> : ModbusOkResponse where TRequest : ModbusRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="request">Modbus 요청</param>
        internal ModbusOkResponse(TRequest request) : base(request) { }
    }

    /// <summary>
    /// Modbus 읽기 응답
    /// </summary>
    public abstract class ModbusReadResponse : ModbusOkResponse<ModbusReadRequest>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="request">Modbus 읽기 요청</param>
        internal ModbusReadResponse(ModbusReadRequest request) : base(request) { }
    }

    /// <summary>
    /// Modbus 논리값 읽기 응답
    /// </summary>
    public class ModbusReadBooleanResponse : ModbusReadResponse
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="values">응답할 논리값 배열</param>
        /// <param name="request">Modbus 읽기 요청</param>
        internal ModbusReadBooleanResponse(bool[] values, ModbusReadRequest request) : base(request)
        {
            switch (request.Function)
            {
                case ModbusFunction.ReadCoils:
                case ModbusFunction.ReadDiscreteInputs:
                    break;
                default:
                    throw new ArgumentException("The Function in the request does not match.", nameof(request));
            }

            Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        /// <summary>
        /// 응답 논리값 목록
        /// </summary>
        public IReadOnlyList<bool> Values { get; }

        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 된 바이트 열거</returns>
        public override IEnumerable<byte> Serialize()
        {
            yield return Request.SlaveAddress;
            yield return (byte)Request.Function;
            yield return (byte)Math.Ceiling(Values.Count / 8d);

            int value = 0;

            for (int i = 0; i < Values.Count; i++)
            {
                int bitIndex = i % 8;
                value |= (Values[i] ? 1 : 0) << bitIndex;

                if (bitIndex == 7 || i == Values.Count - 1)
                    yield return (byte)value;
            }
        }

        /// <summary>
        /// Modbus Log 카테고리
        /// </summary>
        public override ModbusLogCategory LogCategory
        {
            get => Request.ObjectType == ModbusObjectType.Coil
                ? ModbusLogCategory.ResponseReadCoil
                : ModbusLogCategory.ResponseReadDiscreteInput;
        }
    }

    /// <summary>
    /// Modbus 레지스터 읽기 응답
    /// </summary>
    public class ModbusReadRegisterResponse : ModbusReadResponse
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="bytes">응답할 레지스터들의 Raw 바이트 배열</param>
        /// <param name="request">Modbus 읽기 요청</param>
        internal ModbusReadRegisterResponse(byte[] bytes, ModbusReadRequest request) : base(request)
        {
            switch (request.Function)
            {
                case ModbusFunction.ReadHoldingRegisters:
                case ModbusFunction.ReadInputRegisters:
                    break;
                default:
                    throw new ArgumentException("The Function in the request does not match.", nameof(request));
            }

            Bytes = bytes ?? throw new ArgumentException(nameof(bytes));
        }

        private IReadOnlyList<ushort> values;

        /// <summary>
        /// 응답 레지스터들의 Raw 바이트 배열
        /// </summary>
        public IReadOnlyList<byte> Bytes { get; }

        /// <summary>
        /// 응답 레지스터 배열
        /// </summary>
        public IReadOnlyList<ushort> Values
        {
            get
            {
                if (values == null)
                {
                    var bytes = Bytes;
                    values = Enumerable.Range(0, bytes.Count / 2).Select(i => (ushort)(bytes[i * 2] << 8 | bytes[i * 2 + 1])).ToArray();
                }
                return values;
            }
        }

        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 된 바이트 열거</returns>
        public override IEnumerable<byte> Serialize()
        {
            yield return Request.SlaveAddress;
            yield return (byte)Request.Function;
            yield return (byte)(Request.Length * 2);

            for (int i = 0; i < Request.Length * 2; i++)
            {
                if (i < Bytes.Count)
                    yield return Bytes[i];
                else
                    yield return 0;
            }
        }

        /// <summary>
        /// Modbus Log 카테고리
        /// </summary>
        public override ModbusLogCategory LogCategory
        {
            get => Request.ObjectType == ModbusObjectType.HoldingRegister
                ? ModbusLogCategory.ResponseReadHoldingRegister
                : ModbusLogCategory.ResponseReadInputRegister;
        }

        private IEnumerable<byte> GetRawData(ushort address, int rawDataCount)
        {
            return Bytes.Skip((address - Request.Address) * 2).Take(rawDataCount);
        }

        /// <summary>
        /// 특정 주소로부터 부호 있는 2바이트 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public short GetInt16(ushort address) => GetInt16(address, true);
        /// <summary>
        /// 특정 주소로부터 부호 없는 2바이트 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public ushort GetUInt16(ushort address) => GetUInt16(address, true);
        /// <summary>
        /// 특정 주소로부터 부호 있는 4바이트 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public int GetInt32(ushort address) => GetInt32(address, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소로부터 부호 없는 4바이트 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public uint GetUInt32(ushort address) => GetUInt32(address, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소로부터 부호 있는 8바이트 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public long GetInt64(ushort address) => GetInt64(address, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소로부터 부호 없는 8바이트 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public ulong GetUInt64(ushort address) => GetUInt64(address, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소로부터 4바이트 실수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public float GetSingle(ushort address) => GetSingle(address, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소로부터 8바이트 실수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public double GetDouble(ushort address) => GetDouble(address, new ModbusEndian(true));

        /// <summary>
        /// 특정 주소로부터 부호 있는 2바이트 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        /// <returns>값</returns>
        public short GetInt16(ushort address, bool isBigEndian) => BitConverter.ToInt16(new ModbusEndian(isBigEndian).Sort(GetRawData(address, 2).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 부호 없는 2바이트 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        /// <returns>값</returns>
        public ushort GetUInt16(ushort address, bool isBigEndian) => BitConverter.ToUInt16(new ModbusEndian(isBigEndian).Sort(GetRawData(address, 2).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 부호 있는 4바이트 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public int GetInt32(ushort address, ModbusEndian endian) => BitConverter.ToInt32(endian.Sort(GetRawData(address, 4).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 부호 없는 4바이트 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public uint GetUInt32(ushort address, ModbusEndian endian) => BitConverter.ToUInt32(endian.Sort(GetRawData(address, 4).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 부호 있는 8바이트 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public long GetInt64(ushort address, ModbusEndian endian) => BitConverter.ToInt64(endian.Sort(GetRawData(address, 8).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 부호 없는 8바이트 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public ulong GetUInt64(ushort address, ModbusEndian endian) => BitConverter.ToUInt64(endian.Sort(GetRawData(address, 8).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 8바이트 실수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public float GetSingle(ushort address, ModbusEndian endian) => BitConverter.ToSingle(endian.Sort(GetRawData(address, 4).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 8바이트 실수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public double GetDouble(ushort address, ModbusEndian endian) => BitConverter.ToDouble(endian.Sort(GetRawData(address, 8).ToArray()), 0);
    }

    /// <summary>
    /// Modbus 쓰기에 대한 응답
    /// </summary>
    public class ModbusWriteResponse : ModbusOkResponse<ModbusWriteRequest>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="request">Modbus 읽기 요청</param>
        internal ModbusWriteResponse(ModbusWriteRequest request) : base(request)
        {
            switch (request.Function)
            {
                case ModbusFunction.WriteMultipleCoils:
                case ModbusFunction.WriteSingleCoil:
                case ModbusFunction.WriteMultipleHoldingRegisters:
                case ModbusFunction.WriteSingleHoldingRegister:
                    break;
                default:
                    throw new ArgumentException("The Function in the request does not match.", nameof(request));
            }
        }

        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 된 바이트 열거</returns>
        public override IEnumerable<byte> Serialize()
        {
            yield return Request.SlaveAddress;
            yield return (byte)Request.Function;
            yield return (byte)((Request.Address >> 8) & 0xff);
            yield return (byte)(Request.Address & 0xff);

            switch (Request.Function)
            {
                case ModbusFunction.WriteMultipleCoils:
                case ModbusFunction.WriteMultipleHoldingRegisters:
                    yield return (byte)((Request.Length >> 8) & 0xff);
                    yield return (byte)(Request.Length & 0xff);
                    break;
                case ModbusFunction.WriteSingleCoil:
                    ModbusWriteCoilRequest writeCoilRequest = Request as ModbusWriteCoilRequest;
                    yield return writeCoilRequest.SingleBooleanValue ? (byte)0xff : (byte)0x00;
                    yield return 0x00;
                    break;
                case ModbusFunction.WriteSingleHoldingRegister:
                    ModbusWriteHoldingRegisterRequest writeHoldingRegisterRequest = Request as ModbusWriteHoldingRegisterRequest;
                    yield return (byte)((writeHoldingRegisterRequest.SingleRegisterValue >> 8) & 0xff);
                    yield return (byte)(writeHoldingRegisterRequest.SingleRegisterValue  & 0xff);
                    break;
            }
        }

        /// <summary>
        /// Modbus Log 카테고리
        /// </summary>
        public override ModbusLogCategory LogCategory
        {
            get
            {
                switch (Request.Function)
                {
                    case ModbusFunction.WriteMultipleCoils:
                        return ModbusLogCategory.ResponseWriteMultiCoil;
                    case ModbusFunction.WriteSingleCoil:
                        return ModbusLogCategory.ResponseWriteSingleCoil;
                    case ModbusFunction.WriteMultipleHoldingRegisters:
                        return ModbusLogCategory.ResponseWriteMultiHoldingRegister;
                    case ModbusFunction.WriteSingleHoldingRegister:
                        return ModbusLogCategory.ResponseWriteSingleHoldingRegister;
                    default:
                        return ModbusLogCategory.None;
                }
            }
        }
    }


}
