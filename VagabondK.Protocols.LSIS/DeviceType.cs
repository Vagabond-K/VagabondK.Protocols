namespace VagabondK.Protocols.LSIS
{
    /// <summary>
    /// LS산전 PLC 디바이스 영역
    /// </summary>
    public enum DeviceType : byte
    {
        /// <summary>
        /// 알 수 없음
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// P, 입출력 릴레이
        /// </summary>
        P = 0x50,
        /// <summary>
        /// M, 내부 릴레이
        /// </summary>
        M = 0x4d,
        /// <summary>
        /// K, 정전시 상태 유지 릴레이
        /// </summary>
        K = 0x4b,
        /// <summary>
        /// F, 특수 릴레이
        /// </summary>
        F = 0x46,
        /// <summary>
        /// T, 타이머
        /// </summary>
        T = 0x54,
        /// <summary>
        /// C, 카운터
        /// </summary>
        C = 0x43,
        /// <summary>
        /// L, 링크 릴레이
        /// </summary>
        L = 0x4c,
        /// <summary>
        /// D, 데이터 레지스터
        /// </summary>
        D = 0x44,
        /// <summary>
        /// S, 스텝 릴레이
        /// </summary>
        S = 0x53,
        /// <summary>
        /// Z, 인덱스 전용 레지스터
        /// </summary>
        Z = 0x5a,
        /// <summary>
        /// 통신 모듈의 P2P 서비스 주소 영역
        /// </summary>
        N = 0x4e,
        /// <summary>
        /// 플래시 메모리 전용 영역
        /// </summary>
        R = 0x52,

        ///// <summary>
        ///// U, 아날로그 리프레시
        ///// </summary>
        //U = 0x55,
    }
}
