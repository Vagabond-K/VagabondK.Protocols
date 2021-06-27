namespace VagabondK.Protocols.LSElectric.Cnet
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 커맨드
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
    /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 커맨드 타입
    /// </summary>
    public enum CnetCommandType : ushort
    {
        /// <summary>
        /// 알 수 없음
        /// </summary>
        Unknown = 0x0000,

        /// <summary>
        /// 개별 변수 액세스
        /// </summary>
        Individual = 0x5353,
        /// <summary>
        /// 연속 변수 액세스
        /// </summary>
        Continuous = 0x5342,
    }

    /// <summary>
    /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 NAK 에러 코드
    /// </summary>
    public enum CnetNAKCode : ushort
    {
        /// <summary>
        /// 알 수 없음
        /// </summary>
        Unknown = 0x0000,

        /// <summary>
        /// 요청 블록 수 초과(최대 16개)
        /// </summary>
        OverRequestReadBlockCount = 0x0003,

        /// <summary>
        /// 변수 길이 초과(최대 12자리)
        /// </summary>
        OverVariableLength = 0x0004,

        /// <summary>
        /// 데이터 타입 오류
        /// </summary>
        DeviceVariableTypeError = 0x0007,

        /// <summary>
        /// 데이터 오류
        /// </summary>
        DataError = 0x0011,

        /// <summary>
        /// 존재하지 않는 모니터 번호
        /// </summary>
        NotExistsMonitorNumber = 0x0090,

        /// <summary>
        /// 모니터 등록 번호 범위 초과
        /// </summary>
        OutOfRangeExecuteMonitorNumber = 0x0190,

        /// <summary>
        /// 모니터 실행 번호 범위 초과
        /// </summary>
        OutOfRangeRegisterMonitorNumber = 0x0290,

        /// <summary>
        /// 지원하지 않는 디바이스 메모리
        /// </summary>
        IlegalDeviceMemory = 0x1132,

        /// <summary>
        /// 데이터 길이 초과(최대 60워드)
        /// </summary>
        OverDataLength = 0x1232,

        /// <summary>
        /// 필요 없는 데이터가 프레임에 존재함
        /// </summary>
        UnnecessaryDataInFrame = 0x1234,

        /// <summary>
        /// 개별 디바이스 변수들에 서로 다른 타입이 발견됨
        /// </summary>
        DeviceVariableTypeIsDifferent = 0x1332,

        /// <summary>
        /// 16진수로 파싱할 수 없는 문자가 발견됨
        /// </summary>
        DataParsingError = 0x1432,

        /// <summary>
        /// 디바이스 요구 영역 초과
        /// </summary>
        OutOfRangeDeviceVariable = 0x7132,
    }

    /// <summary>
    /// LS ELECTRIC(구 LS산전) Cnet 통신 오류 코드
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
