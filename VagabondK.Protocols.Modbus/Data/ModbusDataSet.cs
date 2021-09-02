using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace VagabondK.Protocols.Modbus.Data
{
    /// <summary>
    /// Modbus 데이터셋
    /// </summary>
    /// <typeparam name="TData">데이터 형식</typeparam>
    /// <typeparam name="TRawData">Raw 데이터 형식</typeparam>
    public abstract class ModbusDataSet<TData, TRawData> : IEnumerable<KeyValuePair<ushort, TData>>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        protected ModbusDataSet()
        {
            DataBlocks = new ReadOnlyCollection<ModbusDataBlock<TData, TRawData>>(dataBlocks);
        }

        private readonly DummyDataBlock<TData, TRawData> dummyDataBlock = new DummyDataBlock<TData, TRawData>();
        private readonly List<ModbusDataBlock<TData, TRawData>> dataBlocks = new List<ModbusDataBlock<TData, TRawData>>();
        private bool autoAllocation = true;

        /// <summary>
        /// 데이터 블록 목록
        /// </summary>
        public IReadOnlyList<IModbusDataBlock<TData, TRawData>> DataBlocks { get; }

        /// <summary>
        /// 자동 주소 할당 여부
        /// </summary>
        public bool AutoAllocation
        {
            get => autoAllocation;
            set
            {
                lock (this)
                {
                    if (autoAllocation != value)
                    {
                        autoAllocation = value;
                    }
                }
            }
        }

        /// <summary>
        /// 특정 주소의 데이터 가져오기
        /// </summary>
        /// <param name="address">데이터 주소</param>
        /// <returns>데이터</returns>
        public TData this[ushort address]
        {
            get
            {
                lock (this)
                {
                    dummyDataBlock.StartAddress = address;
                    var index = dataBlocks.BinarySearch(dummyDataBlock, ModbusStartAddressComparer<TData, TRawData>.Instance);
                    ModbusDataBlock<TData, TRawData> dataBlock;
                    if (index >= 0)
                    {
                        dataBlock = dataBlocks[index];
                        return dataBlock[address];
                    }
                    else
                    {
                        index = ~index - 1;
                        if (index >= 0 && index < dataBlocks.Count)
                            return dataBlocks[index][address];
                        else
                            throw new ModbusException(ModbusExceptionCode.IllegalDataAddress);
                    }
                }
            }
            set
            {
                SetData(address, new TData[] { value });
            }
        }

        /// <summary>
        /// 연속 데이터 가져오기
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="dataCount">데이터 개수</param>
        /// <returns>연속 데이터 열거</returns>
        public IEnumerable<TData> GetData(ushort startAddress, ushort dataCount)
        {
            if (dataCount == 0) return new TData[0];

            lock (this)
            {
                dummyDataBlock.StartAddress = startAddress;
                var index = dataBlocks.BinarySearch(dummyDataBlock, ModbusStartAddressComparer<TData, TRawData>.Instance);
                ModbusDataBlock<TData, TRawData> dataBlock;
                if (index >= 0)
                {
                    dataBlock = dataBlocks[index];
                    if (dataBlock.Count < dataCount)
                        throw new ModbusException(ModbusExceptionCode.IllegalDataAddress);
                    return dataBlock.Take(dataCount);
                }
                else
                {
                    index = ~index - 1;
                    if (index >= 0 && index < dataBlocks.Count)
                    {
                        dataBlock = dataBlocks[index];
                        var skipCount = (startAddress - dataBlock.StartAddress);
                        if (dataBlock.Count < skipCount + dataCount)
                            throw new ModbusException(ModbusExceptionCode.IllegalDataAddress);
                        return dataBlock.Skip(skipCount).Take(dataCount);
                    }
                    else
                        throw new ModbusException(ModbusExceptionCode.IllegalDataAddress);
                }
            }
        }

        /// <summary>
        /// 연속 데이터 설정하기
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="values">데이터 배열</param>
        public void SetData(ushort startAddress, TData[] values)
        {
            SetDataBlock(CreateDataBlock(startAddress, values));
        }

        internal IEnumerable<TRawData> GetRawDataCore(ushort startAddress, int rawDataCount)
        {
            if (rawDataCount == 0) return new TRawData[0];

            lock (this)
            {
                dummyDataBlock.StartAddress = startAddress;
                var index = dataBlocks.BinarySearch(dummyDataBlock, ModbusStartAddressComparer<TData, TRawData>.Instance);
                ModbusDataBlock<TData, TRawData> dataBlock;
                if (index >= 0)
                {
                    dataBlock = dataBlocks[index];
                    if (dataBlock.rawData.Length < rawDataCount)
                        throw new ModbusException(ModbusExceptionCode.IllegalDataAddress);
                    return dataBlock.rawData.Take(rawDataCount);
                }
                else
                {
                    index = ~index - 1;
                    if (index >= 0 && index < dataBlocks.Count)
                    {
                        dataBlock = dataBlocks[index];
                        var skipCount = (startAddress - dataBlock.StartAddress) * dataBlock.NumberOfUnit;
                        if (dataBlock.rawData.Length < skipCount + rawDataCount)
                            throw new ModbusException(ModbusExceptionCode.IllegalDataAddress);
                        return dataBlock.rawData.Skip(skipCount).Take(rawDataCount);
                    }
                    else
                        throw new ModbusException(ModbusExceptionCode.IllegalDataAddress);
                }
            }
        }

        /// <summary>
        /// 연속 데이터 삭제하기
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="count">개수</param>
        public void Remove(ushort startAddress, ushort count)
        {
            if (count == 0) return;

            lock (this)
            {
                ushort endAddress = (ushort)(startAddress + count - 1);

                int startIndex = -1;
                int endIndex = -1;

                dummyDataBlock.StartAddress = startAddress;
                var index = dataBlocks.BinarySearch(dummyDataBlock, ModbusStartAddressComparer<TData, TRawData>.Instance);
                if (index >= 0)
                    startIndex = index;
                else
                {
                    index = ~index;
                    if (index > 0 && index - 1 < dataBlocks.Count && dataBlocks[index - 1].EndAddress >= startAddress)
                        startIndex = index - 1;
                    else if (index < dataBlocks.Count)
                        startIndex = index;
                }

                dummyDataBlock.EndAddress = endAddress;
                index = dataBlocks.BinarySearch(dummyDataBlock, ModbusEndAddressComparer<TData, TRawData>.Instance);
                if (index >= 0)
                    endIndex = index;
                else
                {
                    index = ~index;
                    if (index < dataBlocks.Count && dataBlocks[index].StartAddress <= endAddress)
                        endIndex = index;
                    else
                        endIndex = index - 1;
                }

                foreach (var dataBlock in Enumerable.Range(startIndex, endIndex - startIndex + 1).Where(i => i >= 0 && i < dataBlocks.Count).Select(i => dataBlocks[i]).ToArray())
                {
                    if (dataBlock.StartAddress >= startAddress && dataBlock.EndAddress <= endAddress)
                        dataBlocks.Remove(dataBlock);
                    else if (dataBlock.StartAddress >= startAddress)
                        dataBlock.StartAddress = (ushort)(endAddress + 1);
                    else if (dataBlock.EndAddress <= endAddress)
                        dataBlock.EndAddress = (ushort)(startAddress - 1);
                    else
                    {
                        dataBlocks.Insert(dataBlocks.IndexOf(dataBlock) + 1, 
                            CreateDataBlock((ushort)(endAddress + 1), dataBlock.Skip((endAddress - dataBlock.StartAddress + 1) * dataBlock.NumberOfUnit).ToArray()));
                        dataBlock.EndAddress = (ushort)(startAddress - 1);
                    }
                }
            }
        }

        internal abstract ModbusDataBlock<TData, TRawData> CreateDataBlock(ushort startAddress, TData[] values);

        internal void SetDataBlock(IModbusDataBlock<TData, TRawData> dataBlock)
        {
            lock (this) SetDataBlockCore(dataBlocks, dataBlock, autoAllocation);
        }

        internal void AllocateCore(IModbusDataBlock<TData, TRawData> dataBlock)
        {
            lock (this) SetDataBlockCore(dataBlocks, dataBlock, true);
        }

        private static void SetDataBlockCore(List<ModbusDataBlock<TData, TRawData>> dataBlocks, IModbusDataBlock<TData, TRawData> dataBlock, bool autoAllocation)
        {
            if (!(dataBlock is ModbusDataBlock<TData, TRawData> newDataBlock))
                return;

            if (dataBlocks.Count == 0)
            {
                newDataBlock.AlignRawDataArray();
                dataBlocks.Add(newDataBlock);
            }
            else
            {
                var numberOfUnit = newDataBlock.NumberOfUnit;
                var index = dataBlocks.BinarySearch(newDataBlock, ModbusStartAddressComparer<TData, TRawData>.Instance);

                if (index >= 0)
                {
                    var target = dataBlocks[index];

                    if (newDataBlock.EndAddress <= target.EndAddress)
                    {
                        newDataBlock.rawData.CopyTo(target.rawData, 0);
                    }
                    else
                    {
                        if (autoAllocation)
                        {
                            target.EndAddress = newDataBlock.EndAddress;
                            newDataBlock.rawData.CopyTo(target.rawData, 0);

                            index++;

                            MergeNext(dataBlocks, newDataBlock, index);
                        }
                        else
                        {
                            throw new ModbusException(ModbusExceptionCode.IllegalDataAddress);
                        }
                    }
                }
                else
                {
                    if (autoAllocation)
                    {
                        index = ~index;

                        bool merged = false;
                        index--;
                        if (index >= 0 && index < dataBlocks.Count)
                        {
                            var prev = dataBlocks[index];
                            if (prev.EndAddress + 1 >= newDataBlock.StartAddress)
                            {
                                prev.EndAddress = newDataBlock.EndAddress;
                                newDataBlock.rawData.CopyTo(prev.rawData, (newDataBlock.StartAddress - prev.StartAddress) * numberOfUnit);
                                newDataBlock = prev;
                                merged = true;
                            }
                        }
                        index++;

                        MergeNext(dataBlocks, newDataBlock, index);

                        if (!merged)
                        {
                            newDataBlock.AlignRawDataArray();
                            dataBlocks.Insert(index, newDataBlock);
                        }
                    }
                    else
                    {
                        throw new ModbusException(ModbusExceptionCode.IllegalDataAddress);
                    }
                }
            }
        }

        private static void MergeNext(IList<ModbusDataBlock<TData, TRawData>> dataBlocks, ModbusDataBlock<TData, TRawData> dataBlock, int index)
        {
            var numberOfUnit = dataBlock.NumberOfUnit;
            var endAddress = dataBlock.EndAddress;
            while (index < dataBlocks.Count)
            {
                var next = dataBlocks[index];
                if (endAddress + 1 >= next.StartAddress)
                {
                    var skipCount = (dataBlock.EndAddress - next.StartAddress + 1) * numberOfUnit;
                    var copyIndex = dataBlock.Count * numberOfUnit;
                    if (dataBlock.EndAddress < next.EndAddress)
                        dataBlock.EndAddress = next.EndAddress;
                    if (skipCount < next.rawData.Length)
                        next.rawData.Skip(skipCount).ToArray().CopyTo(dataBlock.rawData, copyIndex);
                    dataBlocks.Remove(next);
                }
                else break;
            }
        }

        /// <summary>
        /// 데이터셋 열거자 가져오기
        /// </summary>
        /// <returns>데이터셋 열거</returns>
        public abstract IEnumerator<KeyValuePair<ushort, TData>> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    class ModbusStartAddressComparer<TData, TRawData> : IComparer<ModbusDataBlock<TData, TRawData>>
    {
        public static ModbusStartAddressComparer<TData, TRawData> Instance { get; } = new ModbusStartAddressComparer<TData, TRawData>();
        public int Compare(ModbusDataBlock<TData, TRawData> x, ModbusDataBlock<TData, TRawData> y) => x.StartAddress.CompareTo(y.StartAddress);
    }

    class ModbusEndAddressComparer<TData, TRawData> : IComparer<ModbusDataBlock<TData, TRawData>>
    {
        public static ModbusEndAddressComparer<TData, TRawData> Instance { get; } = new ModbusEndAddressComparer<TData, TRawData>();
        public int Compare(ModbusDataBlock<TData, TRawData> x, ModbusDataBlock<TData, TRawData> y) => x.EndAddress.CompareTo(y.EndAddress);
    }


    class DummyDataBlock<TData, TRawData> : ModbusDataBlock<TData, TRawData>
    {
        public override ushort StartAddress { get; set; }

        public override ushort EndAddress { get; set; }

        public override ushort Count { get => (ushort)(EndAddress - StartAddress + 1); }


        public override int NumberOfUnit => throw new NotImplementedException();

        public override TData this[ushort address] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override IEnumerator<TData> GetEnumerator() => throw new NotImplementedException();
    }

}
