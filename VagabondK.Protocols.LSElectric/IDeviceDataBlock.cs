using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VagabondK.Protocols.LSElectric
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) PLC의 연속 데이터 블록
    /// </summary>
    public interface IDeviceDataBlock : IReadOnlyList<byte>
    {
        /// <summary>
        /// 시작 디바이스 변수
        /// </summary>
        DeviceVariable StartDeviceVariable { get; }

        /// <summary>
        /// 디바이스 값 개수
        /// </summary>
        int DeviceValueCount { get; }

        /// <summary>
        /// 디바이스 값을 가져옵니다.
        /// </summary>
        /// <param name="dataType">데이터 형식</param>
        /// <param name="index">데이터 인덱스(절대주소)</param>
        /// <returns>디바이스 값</returns>
        DeviceValue this[DataType dataType, uint index] { get; }
    }

    /// <summary>
    /// LS ELECTRIC(구 LS산전) PLC의 연속 데이터 블록과 관련한 확장 메서드
    /// </summary>
    public static class IDeviceDataBlockExtensions
    {
        /// <summary>
        /// LS ELECTRIC(구 LS산전) PLC의 연속 데이터 블록을 변환된 데이터 형식으로 열거함.
        /// </summary>
        /// <param name="deviceDataBlock">LS ELECTRIC(구 LS산전) PLC의 연속 데이터 블록</param>
        /// <param name="dataType">데이터 형식</param>
        /// <returns>변환된 데이터들</returns>
        public static IEnumerable<DeviceValue> Cast(this IDeviceDataBlock deviceDataBlock, DataType dataType)
        {
            var startDeviceVariable = deviceDataBlock.StartDeviceVariable;
            int valueCount = 0;
            switch (startDeviceVariable.DataType)
            {
                case DataType.Byte:
                    valueCount = deviceDataBlock.Count;
                    break;
                case DataType.Word:
                    valueCount = deviceDataBlock.Count / 2;
                    break;
                case DataType.DoubleWord:
                    valueCount = deviceDataBlock.Count / 4;
                    break;
                case DataType.LongWord:
                    valueCount = deviceDataBlock.Count / 8;
                    break;
            }

            switch (dataType)
            {
                case DataType.Bit:
                    foreach (var b in deviceDataBlock)
                        for (int i = 0; i < 8; i++)
                            yield return ((b >> i) & 1) == 1;
                    break;
                case DataType.Byte:
                    foreach (var b in deviceDataBlock)
                        yield return b;
                    break;
                case DataType.Word:
                    var bytes = deviceDataBlock.ToArray();
                    for (int i = 0; i < valueCount; i += 2)
                        yield return BitConverter.ToInt16(bytes, i);
                    break;
                case DataType.DoubleWord:
                    bytes = deviceDataBlock.ToArray();
                    for (int i = 0; i < valueCount; i += 4)
                        yield return BitConverter.ToInt32(bytes, i);
                    break;
                case DataType.LongWord:
                    bytes = deviceDataBlock.ToArray();
                    for (int i = 0; i < valueCount; i += 8)
                        yield return BitConverter.ToInt64(bytes, i);
                    break;
                default:
                    throw new ArgumentException(nameof(dataType));
            }
        }
    }
}
