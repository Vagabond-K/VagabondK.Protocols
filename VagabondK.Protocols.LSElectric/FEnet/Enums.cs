using System;
using System.Collections.Generic;
using System.Text;

namespace VagabondK.Protocols.LSElectric.FEnet
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) FEnet 프로토콜 커맨드
    /// </summary>
    public enum FEnetCommand : ushort
    {
        /// <summary>
        /// 읽기
        /// </summary>
        Read = 0x0054,
        /// <summary>
        /// 쓰기
        /// </summary>
        Write = 0x0058,
    }

    /// <summary>
    /// LS ELECTRIC(구 LS산전) FEnet 프로토콜 커맨드의 데이터 타입
    /// </summary>
    public enum FEnetDataType : ushort
    {
        /// <summary>
        /// 비트
        /// </summary>
        Bit = 0x0000,
        /// <summary>
        /// 바이트
        /// </summary>
        Byte = 0x0001,
        /// <summary>
        /// 워드, 2바이트
        /// </summary>
        Word = 0x0002,
        /// <summary>
        /// 더블 워드, 4바이트
        /// </summary>
        DoubleWord = 0x0003,
        /// <summary>
        /// 롱 워드, 8바이트
        /// </summary>
        LongWord = 0x0004,
        /// <summary>
        /// 연속 데이터, 가변 길이
        /// </summary>
        Continuous = 0x0014,
    }

    /// <summary>
    /// LS ELECTRIC(구 LS산전) FEnet 프로토콜 NAK 에러 코드
    /// </summary>
    public enum FEnetNAKCode : ushort
    {
        /// <summary>
        /// 알 수 없음
        /// </summary>
        Unknown = 0x0000,

        /// <summary>
        /// 요청 블록 수 초과(최대 16개)
        /// </summary>
        OverRequestReadBlockCount = 0x0001,

        /// <summary>
        /// 데이터 타입 오류
        /// </summary>
        DeviceVariableTypeError = 0x0002,

        /// <summary>
        /// 지원하지 않는 디바이스 메모리
        /// </summary>
        IlegalDeviceMemory = 0x0003,

        /// <summary>
        /// 디바이스 요구 영역 초과
        /// </summary>
        OutOfRangeDeviceVariable = 0x0004,

        /// <summary>
        /// 개별 블록 데이터 길이 초과(최대 1400바이트)
        /// </summary>
        OverDataLengthIndividual = 0x0005,

        /// <summary>
        /// 블록별 총 데이터 길이 초과(최대 1400바이트)
        /// </summary>
        OverDataLengthTotal = 0x0006,

        /// <summary>
        /// 잘못된 CompanyID
        /// </summary>
        IlegalCompanyID = 0x0075,

        /// <summary>
        /// 프레임 헤더의 Length 가 잘못됨
        /// </summary>
        IlegalLength = 0x0076,

        /// <summary>
        /// 프레임 헤더의 Chacksum이 잘못됨
        /// </summary>
        ErrorChacksum = 0x0076,

        /// <summary>
        /// 명령어 오류
        /// </summary>
        IlegalCommand = 0x0077,
    }

    /// <summary>
    /// LS ELECTRIC(구 LS산전) FEnet 통신 오류 코드
    /// </summary>
    public enum FEnetCommErrorCode
    {
        /// <summary>
        /// Not Defined
        /// </summary>
        NotDefined,
        /// <summary>
        /// 응답의 길이 정보와 실제 길이가 일치하지 않음.
        /// </summary>
        ResponseLengthDoNotMatch,
        /// <summary>
        /// 요청과 응답의 커맨드가 일치하지 않음.
        /// </summary>
        ResponseCommandDoNotMatch,
        /// <summary>
        /// 요청과 응답의 데이터 타입이 일치하지 않음.
        /// </summary>
        ResponseDataTypeDoNotMatch,
        /// <summary>
        /// 요청과 응답의 데이터 블록 개수가 일치하지 않음.
        /// </summary>
        ResponseDataBlockCountDoNotMatch,
        /// <summary>
        /// 요청과 응답의 데이터 개수가 일치하지 않음.
        /// </summary>
        ResponseDataCountNotMatch,
        /// <summary>
        /// 잘못된 데이터 개수.
        /// </summary>
        ErrorDataCount,
        /// <summary>
        /// BCC 오류
        /// </summary>
        ErrorChecksum,
        /// <summary>
        /// 응답 타임아웃
        /// </summary>
        ResponseTimeout
    }
}
