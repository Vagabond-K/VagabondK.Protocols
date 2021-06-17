using System;
using System.Collections;
using System.Collections.Generic;

namespace VagabondK.Protocols.Modbus.Data
{
    abstract class ModbusDataBlock<TData, TRawData> : IModbusDataBlock<TData, TRawData>
    {
        internal TRawData[] rawData;

        public abstract TData this[ushort address] { get; set; }

        public abstract ushort StartAddress { get; set; }
        public abstract ushort EndAddress { get; set; }
        public abstract ushort Count { get; }
        public IReadOnlyList<TRawData> RawData { get => rawData; }
        public abstract int NumberOfUnit { get; }

        public abstract IEnumerator<TData> GetEnumerator();

        internal void AlignRawDataArray()
        {
            var rawDataLength = Count * NumberOfUnit;
            if (rawDataLength > rawData.Length)
                Array.Resize(ref rawData, rawDataLength);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
