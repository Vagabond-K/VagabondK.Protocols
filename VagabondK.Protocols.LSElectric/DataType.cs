namespace VagabondK.Protocols.LSElectric
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) PLC 데이터 형식
    /// </summary>
    public enum DataType : byte
    {
        /// <summary>
        /// 알 수 없음
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// 비트
        /// </summary>
        Bit = 0x58,
        /// <summary>
        /// 바이트
        /// </summary>
        Byte = 0x42,
        /// <summary>
        /// 워드, 2바이트
        /// </summary>
        Word = 0x57,
        /// <summary>
        /// 더블 워드, 4바이트
        /// </summary>
        DoubleWord = 0x44,
        /// <summary>
        /// 롱 워드, 8바이트
        /// </summary>
        LongWord = 0x4c
    }
}
