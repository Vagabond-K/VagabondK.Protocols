namespace VagabondK.Protocols.LSElectric.Cnet
{
    /// <summary>
    /// LS ELECTRIC Cnet 프로토콜 커맨드
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
        ExecuteMonitor = 0x59,
    }

    /// <summary>
    /// LS ELECTRIC Cnet 프로토콜 커맨드 타입
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

    public enum CnetNAKCode : ushort
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

    /// <summary>
    /// Cnet 통신 오류 코드
    /// </summary>
    public enum CnetCommErrorCode
    {
        /// <summary>
        /// Not Defined
        /// </summary>
        NotDefined,
        /// <summary>
        /// 응답 헤더가 ACK나 NAK가 아님
        /// </summary>
        ResponseHeaderError,
        /// <summary>
        /// 요청과 응답의 국번이 일치하지 않음.
        /// </summary>
        ResponseStationNumberDoNotMatch,
        /// <summary>
        /// 요청과 응답의 커맨드가 일치하지 않음.
        /// </summary>
        ResponseCommandDoNotMatch,
        /// <summary>
        /// 요청과 응답의 커맨드 타입이 일치하지 않음.
        /// </summary>
        ResponseCommandTypeDoNotMatch,
        /// <summary>
        /// 요청과 응답의 모니터 번호가 일치하지 않음.
        /// </summary>
        ResponseMonitorNumberDoNotMatch,
        /// <summary>
        /// 요청과 응답의 데이터 블록 개수가 일치하지 않음.
        /// </summary>
        ResponseDataBlockCountDoNotMatch,
        /// <summary>
        /// 요청과 응답의 데이터 개수가 일치하지 않음.
        /// </summary>
        ResponseDataCountDoNotMatch,
        /// <summary>
        /// 응답 종결이 EOT가 아님.
        /// </summary>
        ResponseTailError,
        /// <summary>
        /// 응답 메시지에서 16진수 문자열 파싱 중 오류 발생.
        /// </summary>
        ResponseParseHexError,
        /// <summary>
        /// BCC 오류
        /// </summary>
        ErrorBCC,
        /// <summary>
        /// 응답 타임아웃
        /// </summary>
        ResponseTimeout
    }

}
