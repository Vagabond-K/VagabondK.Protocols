namespace VagabondK.Protocols.LSIS.Cnet
{
    /// <summary>
    /// LS산전 Cnet 프로토콜 커맨드
    /// </summary>
    public enum CnetCommand : byte
    {
        /// <summary>
        /// 읽기
        /// </summary>
        Read = 0x52,
        /// <summary>
        /// 쓰기
        /// </summary>
        Write = 0x57,

        /// <summary>
        /// 모니터 변수 등록
        /// </summary>
        RegisterMonitor = 0x58,

        /// <summary>
        /// 모니터 실행
        /// </summary>
        ExcuteMonitor = 0x59,
    }

    /// <summary>
    /// LS산전 Cnet 프로토콜 커맨드 타입
    /// </summary>
    public enum CnetCommandType : ushort
    {
        /// <summary>
        /// 개별 주소
        /// </summary>
        Each = 0x5353,
        /// <summary>
        /// 연속 주소
        /// </summary>
        Block = 0x5342,
    }

    public enum CnetErrorCode : ushort
    {
        /// <summary>
        /// 알 수 없음
        /// </summary>
        Unknown = 0x0000,

        /// <summary>
        /// 요청 블록 수 초과
        /// </summary>
        OverRequestDataCount = 0x0003,

        //TODO: Cnet 에러 코드 상세 작성
    }
}
