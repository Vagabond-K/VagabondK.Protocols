using System.Collections.Generic;

namespace VagabondK.Protocols.Modbus.Data
{
    /// <summary>
    /// Modbus 데이터 블록 인터페이스
    /// </summary>
    /// <typeparam name="TData">데이터 형식</typeparam>
    /// <typeparam name="TRawData">Raw 데이터 형식</typeparam>
    public interface IModbusDataBlock<TData, TRawData> : IEnumerable<TData>
    {
        /// <summary>
        /// 시작 주소
        /// </summary>
        ushort StartAddress { get; }

        /// <summary>
        /// 끝 주소
        /// </summary>
        ushort EndAddress { get; }

        /// <summary>
        /// 개수
        /// </summary>
        ushort Count { get; }

        /// <summary>
        /// Raw 데이터
        /// </summary>
        IReadOnlyList<TRawData> RawData { get; }

        /// <summary>
        /// 단위 데이터 당 Raw 데이터 개수
        /// </summary>
        int NumberOfUnit { get; }

        /// <summary>
        /// 특정 주소의 데이터 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>데이터</returns>
        TData this[ushort address] { get; }
    }
}
