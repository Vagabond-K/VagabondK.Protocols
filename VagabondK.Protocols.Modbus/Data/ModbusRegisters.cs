using System;
using System.Collections.Generic;
using System.Linq;

namespace VagabondK.Protocols.Modbus.Data
{
    /// <summary>
    /// Modbus 레지스터(2 바이트) 데이터셋
    /// </summary>
    public class ModbusRegisters : ModbusDataSet<ushort, byte>
    {
        /// <summary>
        /// 데이터셋 열거자 가져오기
        /// </summary>
        /// <returns>데이터셋 열거</returns>
        public override IEnumerator<KeyValuePair<ushort, ushort>> GetEnumerator()
        {
            foreach (ModbusRegisterDataBlock dataBlock in DataBlocks)
            {
                ushort address = dataBlock.StartAddress;
                foreach (var value in dataBlock)
                    yield return new KeyValuePair<ushort, ushort>(address++, value);
            }
        }

        /// <summary>
        /// Raw 데이터 기반 주소 할당
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="bytes">Raw 데이터 배열</param>
        public void Allocate(ushort startAddress, byte[] bytes)
        {
            AllocateCore(new ModbusRegisterDataBlock(startAddress, bytes));
        }

        /// <summary>
        /// 레지스터(2 바이트) 기반 주소 할당
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="values">레지스터 데이터 배열</param>
        public void Allocate(ushort startAddress, ushort[] values)
        {
            AllocateCore(new ModbusRegisterDataBlock(startAddress, values));
        }

        /// <summary>
        /// 연속 Raw 데이터 가져오기
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="rawDataCount">Raw 데이터 개수</param>
        /// <returns>Raw 데이터 열거</returns>
        public IEnumerable<byte> GetRawData(ushort startAddress, int rawDataCount)
        {
            return GetRawDataCore(startAddress, rawDataCount);
        }

        /// <summary>
        /// 연속 Raw 데이터 설정하기
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="bytes">Raw 데이터 배열</param>
        public void SetRawData(ushort startAddress, byte[] bytes)
        {
            SetDataBlock(new ModbusRegisterDataBlock(startAddress, bytes));
        }

        /// <summary>
        /// 특정 주소에 부호 있는 2바이트 정수 값 할당
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        public void Allocate(ushort address, short value) => Allocate(address, value, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소에 부호 없는 2바이트 정수 값 할당
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        public void Allocate(ushort address, ushort value) => Allocate(address, value, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소에 부호 있는 4바이트 정수 값 할당
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        public void Allocate(ushort address, int value) => Allocate(address, value, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소에 부호 없는 4바이트 정수 값 할당
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        public void Allocate(ushort address, uint value) => Allocate(address, value, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소에 부호 있는 8바이트 정수 값 할당
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        public void Allocate(ushort address, long value) => Allocate(address, value, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소에 부호 없는 8바이트 정수 값 할당
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        public void Allocate(ushort address, ulong value) => Allocate(address, value, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소에 4바이트 실수 값 할당
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        public void Allocate(ushort address, float value) => Allocate(address, value, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소에 8바이트 실수 값 할당
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        public void Allocate(ushort address, double value) => Allocate(address, value, new ModbusEndian(true));

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
        /// 특정 주소에 부호 있는 2바이트 정수 값 설정
        /// (자동 할당 설정이 되어있지 않으면서 미리 할당된 주소가 아니면 오류 발생)
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        public void SetValue(ushort address, short value) => SetValue(address, value, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소에 부호 없는 2바이트 정수 값 설정
        /// (자동 할당 설정이 되어있지 않으면서 미리 할당된 주소가 아니면 오류 발생)
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        public void SetValue(ushort address, ushort value) => SetValue(address, value, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소에 부호 있는 4바이트 정수 값 설정
        /// (자동 할당 설정이 되어있지 않으면서 미리 할당된 주소가 아니면 오류 발생)
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        public void SetValue(ushort address, int value) => SetValue(address, value, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소에 부호 없는 4바이트 정수 값 설정
        /// (자동 할당 설정이 되어있지 않으면서 미리 할당된 주소가 아니면 오류 발생)
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        public void SetValue(ushort address, uint value) => SetValue(address, value, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소에 부호 있는 8바이트 정수 값 설정
        /// (자동 할당 설정이 되어있지 않으면서 미리 할당된 주소가 아니면 오류 발생)
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        public void SetValue(ushort address, long value) => SetValue(address, value, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소에 부호 없는 8바이트 정수 값 설정
        /// (자동 할당 설정이 되어있지 않으면서 미리 할당된 주소가 아니면 오류 발생)
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        public void SetValue(ushort address, ulong value) => SetValue(address, value, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소에 4바이트 실수 값 설정
        /// (자동 할당 설정이 되어있지 않으면서 미리 할당된 주소가 아니면 오류 발생)
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        public void SetValue(ushort address, float value) => SetValue(address, value, new ModbusEndian(true));
        /// <summary>
        /// 특정 주소에 8바이트 실수 값 설정
        /// (자동 할당 설정이 되어있지 않으면서 미리 할당된 주소가 아니면 오류 발생)
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        public void SetValue(ushort address, double value) => SetValue(address, value, new ModbusEndian(true));

        /// <summary>
        /// 특정 주소에 부호 있는 2바이트 정수 값 할당
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        /// <param name="endian">엔디안</param>
        public void Allocate(ushort address, short value, ModbusEndian endian) => Allocate(address, endian.Sort(BitConverter.GetBytes(value)));
        /// <summary>
        /// 특정 주소에 부호 없는 2바이트 정수 값 할당
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        /// <param name="endian">엔디안</param>
        public void Allocate(ushort address, ushort value, ModbusEndian endian) => Allocate(address, endian.Sort(BitConverter.GetBytes(value)));
        /// <summary>
        /// 특정 주소에 부호 있는 4바이트 정수 값 할당
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        /// <param name="endian">엔디안</param>
        public void Allocate(ushort address, int value, ModbusEndian endian) => Allocate(address, endian.Sort(BitConverter.GetBytes(value)));
        /// <summary>
        /// 특정 주소에 부호 없는 4바이트 정수 값 할당
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        /// <param name="endian">엔디안</param>
        public void Allocate(ushort address, uint value, ModbusEndian endian) => Allocate(address, endian.Sort(BitConverter.GetBytes(value)));
        /// <summary>
        /// 특정 주소에 부호 있는 8바이트 정수 값 할당
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        /// <param name="endian">엔디안</param>
        public void Allocate(ushort address, long value, ModbusEndian endian) => Allocate(address, endian.Sort(BitConverter.GetBytes(value)));
        /// <summary>
        /// 특정 주소에 부호 없는 8바이트 정수 값 할당
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        /// <param name="endian">엔디안</param>
        public void Allocate(ushort address, ulong value, ModbusEndian endian) => Allocate(address, endian.Sort(BitConverter.GetBytes(value)));
        /// <summary>
        /// 특정 주소에 4바이트 실수 값 할당
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        /// <param name="endian">엔디안</param>
        public void Allocate(ushort address, float value, ModbusEndian endian) => Allocate(address, endian.Sort(BitConverter.GetBytes(value)));
        /// <summary>
        /// 특정 주소에 8바이트 실수 값 할당
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        /// <param name="endian">엔디안</param>
        public void Allocate(ushort address, double value, ModbusEndian endian) => Allocate(address, endian.Sort(BitConverter.GetBytes(value)));

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

        /// <summary>
        /// 특정 주소에 부호 있는 2바이트 정수 값 설정
        /// (자동 할당 설정이 되어있지 않으면서 미리 할당된 주소가 아니면 오류 발생)
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        /// <param name="endian">엔디안</param>
        public void SetValue(ushort address, short value, ModbusEndian endian) => SetRawData(address, endian.Sort(BitConverter.GetBytes(value)));
        /// <summary>
        /// 특정 주소에 부호 없는 2바이트 정수 값 설정
        /// (자동 할당 설정이 되어있지 않으면서 미리 할당된 주소가 아니면 오류 발생)
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        /// <param name="endian">엔디안</param>
        public void SetValue(ushort address, ushort value, ModbusEndian endian) => SetRawData(address, endian.Sort(BitConverter.GetBytes(value)));
        /// <summary>
        /// 특정 주소에 부호 있는 4바이트 정수 값 설정
        /// (자동 할당 설정이 되어있지 않으면서 미리 할당된 주소가 아니면 오류 발생)
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        /// <param name="endian">엔디안</param>
        public void SetValue(ushort address, int value, ModbusEndian endian) => SetRawData(address, endian.Sort(BitConverter.GetBytes(value)));
        /// <summary>
        /// 특정 주소에 부호 없는 4바이트 정수 값 설정
        /// (자동 할당 설정이 되어있지 않으면서 미리 할당된 주소가 아니면 오류 발생)
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        /// <param name="endian">엔디안</param>
        public void SetValue(ushort address, uint value, ModbusEndian endian) => SetRawData(address, endian.Sort(BitConverter.GetBytes(value)));
        /// <summary>
        /// 특정 주소에 부호 있는 8바이트 정수 값 설정
        /// (자동 할당 설정이 되어있지 않으면서 미리 할당된 주소가 아니면 오류 발생)
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        /// <param name="endian">엔디안</param>
        public void SetValue(ushort address, long value, ModbusEndian endian) => SetRawData(address, endian.Sort(BitConverter.GetBytes(value)));
        /// <summary>
        /// 특정 주소에 부호 없는 8바이트 정수 값 설정
        /// (자동 할당 설정이 되어있지 않으면서 미리 할당된 주소가 아니면 오류 발생)
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        /// <param name="endian">엔디안</param>
        public void SetValue(ushort address, ulong value, ModbusEndian endian) => SetRawData(address, endian.Sort(BitConverter.GetBytes(value)));
        /// <summary>
        /// 특정 주소에 4바이트 실수 값 설정
        /// (자동 할당 설정이 되어있지 않으면서 미리 할당된 주소가 아니면 오류 발생)
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        /// <param name="endian">엔디안</param>
        public void SetValue(ushort address, float value, ModbusEndian endian) => SetRawData(address, endian.Sort(BitConverter.GetBytes(value)));
        /// <summary>
        /// 특정 주소에 8바이트 실수 값 설정
        /// (자동 할당 설정이 되어있지 않으면서 미리 할당된 주소가 아니면 오류 발생)
        /// </summary>
        /// <param name="address">주소</param>
        /// <param name="value">값</param>
        /// <param name="endian">엔디안</param>
        public void SetValue(ushort address, double value, ModbusEndian endian) => SetRawData(address, endian.Sort(BitConverter.GetBytes(value)));

        internal override ModbusDataBlock<ushort, byte> CreateDataBlock(ushort startAddress, ushort[] values)
            => new ModbusRegisterDataBlock(startAddress, values);


        class ModbusRegisterDataBlock : ModbusDataBlock<ushort, byte>
        {
            public ModbusRegisterDataBlock(ushort startAddress, byte[] bytes)
            {
                this.startAddress = startAddress;
                rawData = bytes;
            }

            public ModbusRegisterDataBlock(ushort startAddress, ushort[] values)
            {
                this.startAddress = startAddress;
                rawData = values.SelectMany(value => new[] { (byte)(value >> 8), (byte)(value & 0xff) }).ToArray();
            }

            private ushort startAddress = 0;

            public override ushort StartAddress
            {
                get => startAddress;
                set
                {
                    if (value > EndAddress)
                        value = EndAddress;

                    if (startAddress != value)
                    {
                        if (startAddress > value)
                            rawData = Enumerable.Repeat((byte)0, (startAddress - value) * NumberOfUnit).Concat(rawData).ToArray();
                        else
                            rawData = rawData.Skip((value - startAddress) * NumberOfUnit).ToArray();

                        startAddress = value;
                    }
                }
            }
            public override ushort EndAddress { get => (ushort)(StartAddress + Count - 1); set => Array.Resize(ref rawData, Math.Max(value - StartAddress + 1, 0) * NumberOfUnit); }
            public override ushort Count { get => (ushort)Math.Ceiling((double)rawData.Length / NumberOfUnit); }
            public override int NumberOfUnit { get => 2; }

            public override ushort this[ushort address]
            {
                get
                {
                    if (address >= StartAddress && address <= EndAddress)
                    {
                        var index = (address - StartAddress) * NumberOfUnit;
                        if (index + 1 < rawData.Length)
                            return (ushort)((rawData[index] << 8) | rawData[index + 1]);
                        else
                            return (ushort)(rawData[index] << 8);
                    }
                    else
                    {
                        throw new ModbusException(ModbusExceptionCode.IllegalDataAddress);
                    }
                }
                set
                {
                    var index = (address - StartAddress) * NumberOfUnit;
                    rawData[index] = (byte)(value >> 8);
                    if (index + 1 >= rawData.Length)
                        Array.Resize(ref rawData, rawData.Length + 1);
                    rawData[index + 1] = (byte)(value & 0xff);
                }
            }

            public override IEnumerator<ushort> GetEnumerator()
            {
                int count = Count;
                for (int i = 0; i < count; i++)
                {
                    if (i * 2 + 1 < rawData.Length)
                    {
                        yield return (ushort)((rawData[i * 2] << 8) | rawData[i * 2 + 1]);
                    }
                    else
                    {
                        yield return (ushort)(rawData[i * 2] << 8);
                    }
                }
            }
        }
    }
}
