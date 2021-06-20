using System;
using System.Collections.Generic;
using System.Linq;

namespace VagabondK.Protocols.Modbus.Data
{
    /// <summary>
    /// 논리값 Modbus 데이터셋
    /// </summary>
    public class ModbusBooleans : ModbusDataSet<bool, bool>
    {
        /// <summary>
        /// 데이터셋 열거자 가져오기
        /// </summary>
        /// <returns>데이터셋 열거</returns>
        public override IEnumerator<KeyValuePair<ushort, bool>> GetEnumerator()
        {
            foreach (ModbusBooleanDataBlock dataBlock in DataBlocks)
            {
                ushort address = dataBlock.StartAddress;
                foreach (var value in dataBlock)
                    yield return new KeyValuePair<ushort, bool>(address++, value);
            }
        }

        /// <summary>
        /// 주소 할당
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="data">데이터 배열</param>
        public void Allocate(ushort startAddress, bool[] data)
        {
            AllocateCore(new ModbusBooleanDataBlock(startAddress, data));
        }

        internal override ModbusDataBlock<bool, bool> CreateDataBlock(ushort startAddress, bool[] values)
            => new ModbusBooleanDataBlock(startAddress, values);

        class ModbusBooleanDataBlock : ModbusDataBlock<bool, bool>
        {
            public ModbusBooleanDataBlock(ushort startAddress, bool[] values)
            {
                this.startAddress = startAddress;
                rawData = values;
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
                            rawData = Enumerable.Repeat(false, startAddress - value).Concat(rawData).ToArray();
                        else
                            rawData = rawData.Skip(value - startAddress).ToArray();

                        startAddress = value;
                    }
                }
            }
            public override ushort EndAddress { get => (ushort)(StartAddress + Count - 1); set => Array.Resize(ref rawData, Math.Max(value - StartAddress + 1, 0)); }
            public override ushort Count { get => (ushort)rawData.Length; }
            public override int NumberOfUnit { get => 1; }

            public override bool this[ushort address]
            {
                get
                {
                    if (address >= StartAddress && address <= EndAddress)
                    {
                        return rawData[(address - StartAddress) * NumberOfUnit];
                    }
                    else
                    {
                        throw new ErrorCodeException<ModbusExceptionCode>(ModbusExceptionCode.IllegalDataAddress);
                    }
                }
                set
                {
                    rawData[(address - StartAddress) * NumberOfUnit] = value;
                }
            }

            public override IEnumerator<bool> GetEnumerator()
            {
                foreach (var value in rawData)
                    yield return value;
            }
        }
    }
}
