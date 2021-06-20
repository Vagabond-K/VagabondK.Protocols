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
        ExcuteMonitor = 0x59,
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
        /// 요청과 응답의 Slave Address가 일치하지 않음.
        /// </summary>
        ResponseSlaveAddressDoNotMatch,
        /// <summary>
        /// 요청과 응답의 Function 코드가 일치하지 않음.
        /// </summary>
        ResponseFunctionDoNotMatch,
        /// <summary>
        /// 요청과 응답의 Address가 일치하지 않음.
        /// </summary>
        ResponseAddressDoNotMatch,
        /// <summary>
        /// 요청과 응답의 데이터 길이가 일치하지 않음.
        /// </summary>
        ResponseLengthDoNotMatch,
        /// <summary>
        /// 요청과 응답의 쓰기 값이 일치하지 않음.
        /// </summary>
        ResponseWritedValueDoNotMatch,
        /// <summary>
        /// 요청과 응답의 다중 쓰기 길이가 일치하지 않음.
        /// </summary>
        ResponseWritedLengthDoNotMatch,
        /// <summary>
        /// Cnet TCP 프로토콜 아이디가 0이 아님.
        /// </summary>
        CnetTcpSymbolError,
        /// <summary>
        /// Cnet TCP 헤더에 표시된 길이와 Cnet 메시지의 길이가 일치하지 않음.
        /// </summary>
        ResponseTcpLengthDoNotMatch,
        /// <summary>
        /// Cnet ASCII의 시작 문자(:)를 찾을 수 없음.
        /// </summary>
        ResponseAsciiStartError,
        /// <summary>
        /// Cnet ASCII의 종결 문자(CR LF)를 찾을 수 없음.
        /// </summary>
        ResponseAsciiEndError,
        /// <summary>
        /// CRC 오류
        /// </summary>
        ErrorCRC,
        /// <summary>
        /// LRC 오류
        /// </summary>
        ErrorLRC,
        /// <summary>
        /// 응답 타임아웃
        /// </summary>
        ResponseTimeout,
        /// <summary>
        /// 연결 타임아웃
        /// </summary>
        ConnectTimeout,
        /// <summary>
        /// 채널이 없음
        /// </summary>
        NullChannelError,
        /// <summary>
        /// Cnet 직렬화 형식이 정의되지 않았음.
        /// </summary>
        NotDefinedCnetSerializer,
    }

}
