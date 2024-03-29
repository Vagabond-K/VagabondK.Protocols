﻿using System;
using System.Collections.Generic;
using System.Linq;
using VagabondK.Protocols.Modbus.Data;

namespace VagabondK.Protocols.Modbus
{
    /// <summary>
    /// Modbus 응답
    /// </summary>
    public abstract class ModbusResponse : IModbusMessage, IResponse
    {
        private ushort? transactionID = null;

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
        /// <returns>직렬화 된 Byte 열거</returns>
        public abstract IEnumerable<byte> Serialize();

        /// <summary>
        /// 트랜잭션 ID (Modbus TCP에서 사용)
        /// </summary>
        public ushort? TransactionID { get => transactionID ?? Request.TransactionID; set => transactionID = value; }

        /// <summary>
        /// Modbus 메시지 카테고리
        /// </summary>
        public abstract ModbusMessageCategory MessageCategory { get; }
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
        /// <returns>직렬화 된 Byte 열거</returns>
        public override IEnumerable<byte> Serialize()
        {
            yield return Request.SlaveAddress;
            yield return (byte)((int)Request.Function | 0x80);
            yield return (byte)ExceptionCode;
        }

        /// <summary>
        /// Modbus 메시지 카테고리
        /// </summary>
        public override ModbusMessageCategory MessageCategory { get => ModbusMessageCategory.ResponseException; }
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
    /// Bit(Coil, Discrete Input) 읽기 응답
    /// </summary>
    public class ModbusReadBitResponse : ModbusReadResponse
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="values">응답할 Bit(Coil, Discrete Input) 배열</param>
        /// <param name="request">Modbus 읽기 요청</param>
        internal ModbusReadBitResponse(bool[] values, ModbusReadRequest request) : base(request)
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
        /// 응답 Bit(Coil, Discrete Input) 목록
        /// </summary>
        public IReadOnlyList<bool> Values { get; }

        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 된 Byte 열거</returns>
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
        /// Modbus 메시지 카테고리
        /// </summary>
        public override ModbusMessageCategory MessageCategory
        {
            get => Request.ObjectType == ModbusObjectType.Coil
                ? ModbusMessageCategory.ResponseReadCoil
                : ModbusMessageCategory.ResponseReadDiscreteInput;
        }
    }

    /// <summary>
    /// Word(Holding Register, Input Register) 읽기 응답
    /// </summary>
    public class ModbusReadWordResponse : ModbusReadResponse
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="bytes">응답할 Word(Holding Register, Input Register)들의 Raw Byte 배열</param>
        /// <param name="request">Modbus 읽기 요청</param>
        internal ModbusReadWordResponse(byte[] bytes, ModbusReadRequest request) : base(request)
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
        /// 응답 Word(Holding Register, Input Register)들의 Raw Byte 배열
        /// </summary>
        public IReadOnlyList<byte> Bytes { get; }

        /// <summary>
        /// 응답 Word(Holding Register, Input Register) 배열
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
        /// <returns>직렬화 된 Byte 열거</returns>
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
        /// Modbus 메시지 카테고리
        /// </summary>
        public override ModbusMessageCategory MessageCategory
        {
            get => Request.ObjectType == ModbusObjectType.HoldingRegister
                ? ModbusMessageCategory.ResponseReadHoldingRegister
                : ModbusMessageCategory.ResponseReadInputRegister;
        }

        private IEnumerable<byte> GetRawData(ushort address, int rawDataCount)
        {
            return Bytes.Skip((address - Request.Address) * 2).Take(rawDataCount);
        }

        /// <summary>
        /// 특정 주소로부터 부호 있는 2 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public short GetInt16(ushort address) => GetInt16(address, true);
        /// <summary>
        /// 특정 주소로부터 부호 없는 2 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public ushort GetUInt16(ushort address) => GetUInt16(address, true);
        /// <summary>
        /// 특정 주소로부터 부호 있는 4 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public int GetInt32(ushort address) => GetInt32(address, ModbusEndian.AllBig);
        /// <summary>
        /// 특정 주소로부터 부호 없는 4 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public uint GetUInt32(ushort address) => GetUInt32(address, ModbusEndian.AllBig);
        /// <summary>
        /// 특정 주소로부터 부호 있는 8 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public long GetInt64(ushort address) => GetInt64(address, ModbusEndian.AllBig);
        /// <summary>
        /// 특정 주소로부터 부호 없는 8 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public ulong GetUInt64(ushort address) => GetUInt64(address, ModbusEndian.AllBig);
        /// <summary>
        /// 특정 주소로부터 4 Byte 실수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public float GetSingle(ushort address) => GetSingle(address, ModbusEndian.AllBig);
        /// <summary>
        /// 특정 주소로부터 8 Byte 실수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>값</returns>
        public double GetDouble(ushort address) => GetDouble(address, ModbusEndian.AllBig);

        /// <summary>
        /// 특정 주소로부터 부호 있는 2 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        /// <returns>값</returns>
        public short GetInt16(ushort address, bool isBigEndian) => BitConverter.ToInt16((isBigEndian ? ModbusEndian.AllBig : ModbusEndian.AllLittle).Sort(GetRawData(address, 2).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 부호 없는 2 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="isBigEndian">빅 엔디안 여부</param>
        /// <returns>값</returns>
        public ushort GetUInt16(ushort address, bool isBigEndian) => BitConverter.ToUInt16((isBigEndian ? ModbusEndian.AllBig : ModbusEndian.AllLittle).Sort(GetRawData(address, 2).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 부호 있는 4 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public int GetInt32(ushort address, ModbusEndian endian) => BitConverter.ToInt32(endian.Sort(GetRawData(address, 4).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 부호 없는 4 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public uint GetUInt32(ushort address, ModbusEndian endian) => BitConverter.ToUInt32(endian.Sort(GetRawData(address, 4).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 부호 있는 8 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public long GetInt64(ushort address, ModbusEndian endian) => BitConverter.ToInt64(endian.Sort(GetRawData(address, 8).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 부호 없는 8 Byte 정수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public ulong GetUInt64(ushort address, ModbusEndian endian) => BitConverter.ToUInt64(endian.Sort(GetRawData(address, 8).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 8 Byte 실수 값 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="endian">엔디안</param>
        /// <returns>값</returns>
        public float GetSingle(ushort address, ModbusEndian endian) => BitConverter.ToSingle(endian.Sort(GetRawData(address, 4).ToArray()), 0);
        /// <summary>
        /// 특정 주소로부터 8 Byte 실수 값 가져오기
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
        /// <returns>직렬화 된 Byte 열거</returns>
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
                    yield return writeCoilRequest.SingleBitValue ? (byte)0xff : (byte)0x00;
                    yield return 0x00;
                    break;
                case ModbusFunction.WriteSingleHoldingRegister:
                    ModbusWriteHoldingRegisterRequest writeHoldingRegisterRequest = Request as ModbusWriteHoldingRegisterRequest;
                    yield return (byte)((writeHoldingRegisterRequest.SingleWordValue >> 8) & 0xff);
                    yield return (byte)(writeHoldingRegisterRequest.SingleWordValue  & 0xff);
                    break;
            }
        }

        /// <summary>
        /// Modbus 메시지 카테고리
        /// </summary>
        public override ModbusMessageCategory MessageCategory
        {
            get
            {
                switch (Request.Function)
                {
                    case ModbusFunction.WriteMultipleCoils:
                        return ModbusMessageCategory.ResponseWriteMultiCoil;
                    case ModbusFunction.WriteSingleCoil:
                        return ModbusMessageCategory.ResponseWriteSingleCoil;
                    case ModbusFunction.WriteMultipleHoldingRegisters:
                        return ModbusMessageCategory.ResponseWriteMultiHoldingRegister;
                    case ModbusFunction.WriteSingleHoldingRegister:
                        return ModbusMessageCategory.ResponseWriteSingleHoldingRegister;
                    default:
                        return ModbusMessageCategory.None;
                }
            }
        }
    }


}
