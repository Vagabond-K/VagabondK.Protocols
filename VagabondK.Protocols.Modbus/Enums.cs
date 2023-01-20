using System;
using System.ComponentModel;

namespace VagabondK.Protocols.Modbus
{
    /// <summary>
    /// Modbus Object 형식
    /// </summary>
    public enum ModbusObjectType : byte
    {
        /// <summary>
        /// Input Register
        /// </summary>
        [Description("Input Register")]
        InputRegister = 0x04,
        /// <summary>
        /// Discrete Input
        /// </summary>
        [Description("Discrete Input")]
        DiscreteInput = 0x02,
        /// <summary>
        /// Holding Register
        /// </summary>
        [Description("Holding Register")]
        HoldingRegister = 0x03,
        /// <summary>
        /// Coil
        /// </summary>
        [Description("Coil")]
        Coil = 0x01,
    }

    /// <summary>
    /// Modbus Function
    /// </summary>
    public enum ModbusFunction : byte
    {
        /// <summary>
        /// Read Coils
        /// </summary>
        [Description("Read Coils")]
        ReadCoils = 0x01,
        /// <summary>
        /// Read Discrete Inputs
        /// </summary>
        [Description("Read Discrete Inputs")]
        ReadDiscreteInputs = 0x02,
        /// <summary>
        /// Read Holding Registers
        /// </summary>
        [Description("Read Holding Registers")]
        ReadHoldingRegisters = 0x03,
        /// <summary>
        /// Read Input Registers
        /// </summary>
        [Description("Read Input Registers")]
        ReadInputRegisters = 0x04,

        /// <summary>
        /// Write Single Coil
        /// </summary>
        [Description("Write Single Coil")]
        WriteSingleCoil = 0x05,
        /// <summary>
        /// Write Single Holding Register
        /// </summary>
        [Description("Write Single Holding Register")]
        WriteSingleHoldingRegister = 0x06,
        /// <summary>
        /// Write Multiple Coils
        /// </summary>
        [Description("Write Multiple Coils")]
        WriteMultipleCoils = 0x0f,
        /// <summary>
        /// Write Multiple Holding Registers
        /// </summary>
        [Description("Write Multiple Holding Registers")]
        WriteMultipleHoldingRegisters = 0x10,
    }

    /// <summary>
    /// Modbus Exception 코드. 각 코드별 주석은 https://en.wikipedia.org/wiki/Modbus 에서 발췌.
    /// </summary>
    public enum ModbusExceptionCode : byte
    {
        /// <summary>
        /// Not Defined
        /// </summary>
        [Description("Not Defined")]
        NotDefined = 0,
        /// <summary>
        /// Function code received in the query is not recognized or allowed by slave.
        /// </summary>
        [Description("Illegal Function")]
        IllegalFunction = 1,
        /// <summary>
        /// Data address of some or all the required entities are not allowed or do not exist in slave.
        /// </summary>
        [Description("Illegal Data Address")]
        IllegalDataAddress = 2,
        /// <summary>
        /// Value is not accepted by slave.
        /// </summary>
        [Description("Illegal Data Value")]
        IllegalDataValue = 3,
        /// <summary>
        /// Unrecoverable error occurred while slave was attempting to perform requested action.
        /// </summary>
        [Description("Slave Device Failure")]
        SlaveDeviceFailure = 4,
        /// <summary>
        /// Slave has accepted request and is processing it, but a long duration of time is required. 
        /// This response is returned to prevent a timeout error from occurring in the master. 
        /// Master can next issue a Poll Program Complete message to determine whether processing is completed.
        /// </summary>
        [Description("Acknowledge")]
        Acknowledge = 5,
        /// <summary>
        /// Slave is engaged in processing a long-duration command. Master should retry later.
        /// </summary>
        [Description("Slave Device Busy")]
        SlaveDeviceBusy = 6,
        /// <summary>
        /// Slave cannot perform the programming functions. Master should request diagnostic or error information from slave.
        /// </summary>
        [Description("Negative Acknowledge")]
        NegativeAcknowledge = 7,
        /// <summary>
        /// Slave detected a parity error in memory. Master can retry the request, but service may be required on the slave device.
        /// </summary>
        [Description("Memory Parity Error")]
        MemoryParityError = 8,
        /// <summary>
        /// Specialized for Modbus gateways. Indicates a misconfigured gateway.
        /// </summary>
        [Description("Gateway Path Unavailable")]
        GatewayPathUnavailable = 10,
        /// <summary>
        /// Specialized for Modbus gateways. Sent when slave fails to respond.
        /// </summary>
        [Description("Gateway Target Device Failed to Respond")]
        GatewayTargetDeviceFailedToRespond = 11
    }

    /// <summary>
    /// Modbus 통신 오류 코드
    /// </summary>
    public enum ModbusCommErrorCode
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
        /// Modbus TCP 프로토콜 아이디가 0이 아님.
        /// </summary>
        ModbusTcpSymbolError,
        /// <summary>
        /// Modbus TCP 헤더에 표시된 길이와 Modbus 메시지의 길이가 일치하지 않음.
        /// </summary>
        ResponseTcpLengthDoNotMatch,
        /// <summary>
        /// Modbus ASCII의 시작 문자(:)를 찾을 수 없음.
        /// </summary>
        ResponseAsciiStartError,
        /// <summary>
        /// Modbus ASCII의 종결 문자(CR LF)를 찾을 수 없음.
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
        /// Modbus 직렬화 형식이 정의되지 않았음.
        /// </summary>
        NotDefinedModbusSerializer,
    }



    /// <summary>
    /// Modbus 메시지 카테고리
    /// </summary>
    [Flags]
    public enum ModbusMessageCategory : int
    {
        /// <summary>
        /// 없음
        /// </summary>
        None = 0,

        /// <summary>
        /// 모든 카테고리
        /// </summary>
        All = 0b_0011_0111_1111_1111_1111_1111,

        /// <summary>
        /// Modbus 메시지 Log
        /// </summary>
        ModbusMessage = 0b_0000_0000_1111_1111_1111_1111,
        /// <summary>
        /// Modbus 응답 Log
        /// </summary>
        Response = 0b_0000_0000_0000_0000_1111_1111,
        /// <summary>
        /// Modbus 읽기 응답 Log
        /// </summary>
        ResponseRead = 0b_0000_0000_0000_0000_0000_1111,
        /// <summary>
        /// Modbus 쓰기 응답 Log
        /// </summary>
        ResponseWrite = 0b_0000_0000_0000_0000_1111_0000,
        /// <summary>
        /// Modbus 요청 Log
        /// </summary>
        Request = 0b_0000_0000_1111_1111_0000_0000,
        /// <summary>
        /// Modbus 읽기 요청 Log
        /// </summary>
        RequestRead = 0b_0000_0000_0000_1111_0000_0000,
        /// <summary>
        /// Modbus 쓰기 요청 Log
        /// </summary>
        RequestWrite = 0b_0000_0000_1111_0000_0000_0000,
        /// <summary>
        /// 오류 Log
        /// </summary>
        Error = 0b_0000_0011_0000_0000_0000_0000,


        /// <summary>
        /// Modbus Coil 읽기 응답 Log
        /// </summary>
        ResponseReadCoil = 0b_0000_0000_0000_0000_0000_0001,
        /// <summary>
        /// Modbus Discrete Input 읽기 응답 Log
        /// </summary>
        ResponseReadDiscreteInput = 0b_0000_0000_0000_0000_0000_0010,
        /// <summary>
        /// Modbus Input Register 읽기 응답 Log
        /// </summary>
        ResponseReadInputRegister = 0b_0000_0000_0000_0000_0000_0100,
        /// <summary>
        /// Modbus Holding Register 읽기 응답 Log
        /// </summary>
        ResponseReadHoldingRegister = 0b_0000_0000_0000_0000_0000_1000,
        /// <summary>
        /// Modbus Coil 쓰기 응답 Log
        /// </summary>
        ResponseWriteCoil = 0b_0000_0000_0000_0000_1100_0000,
        /// <summary>
        /// Modbus Coil 한 개 쓰기 응답 Log
        /// </summary>
        ResponseWriteSingleCoil = 0b_0000_0000_0000_0000_1000_0000,
        /// <summary>
        /// Modbus Coil 여러 개 쓰기 응답 Log
        /// </summary>
        ResponseWriteMultiCoil = 0b_0000_0000_0000_0000_0100_0000,
        /// <summary>
        /// Modbus Holding Register 쓰기 응답 Log
        /// </summary>
        ResponseWriteHoldingRegister = 0b_0000_0000_0000_0000_0011_0000,
        /// <summary>
        /// Modbus Holding Register 한 개 쓰기 응답 Log
        /// </summary>
        ResponseWriteSingleHoldingRegister = 0b_0000_0000_0000_0000_0010_0000,
        /// <summary>
        /// Modbus Holding Register 여러 개 쓰기 응답 Log
        /// </summary>
        ResponseWriteMultiHoldingRegister = 0b_0000_0000_0000_0000_0001_0000,

        /// <summary>
        /// Modbus Coil 읽기 요청 Log
        /// </summary>
        RequestReadCoil = 0b_0000_0000_0000_0001_0000_0000,
        /// <summary>
        /// Modbus Discrete Input 읽기 요청 Log
        /// </summary>
        RequestReadDiscreteInput = 0b_0000_0000_0000_0010_0000_0000,
        /// <summary>
        /// Modbus Input Register 읽기 요청 Log
        /// </summary>
        RequestReadInputRegister = 0b_0000_0000_0000_0100_0000_0000,
        /// <summary>
        /// Modbus Holding Register 읽기 요청 Log
        /// </summary>
        RequestReadHoldingRegister = 0b_0000_0000_0000_1000_0000_0000,
        /// <summary>
        /// Modbus Coil 쓰기 요청 Log
        /// </summary>
        RequestWriteCoil = 0b_0000_0000_1100_0000_0000_0000,
        /// <summary>
        /// Modbus Coil 한 개 쓰기 요청 Log
        /// </summary>
        RequestWriteSingleCoil = 0b_0000_0000_1000_0000_0000_0000,
        /// <summary>
        /// Modbus Coil 여러 개 쓰기 요청 Log
        /// </summary>
        RequestWriteMultiCoil = 0b_0000_0000_0100_0000_0000_0000,
        /// <summary>
        /// Modbus Holding Register 쓰기 요청 Log
        /// </summary>
        RequestWriteHoldingRegister = 0b_0000_0000_0011_0000_0000_0000,
        /// <summary>
        /// Modbus Holding Register 한 개 쓰기 요청 Log
        /// </summary>
        RequestWriteSingleHoldingRegister = 0b_0000_0000_0010_0000_0000_0000,
        /// <summary>
        /// Modbus Holding Register 여러 개 쓰기 요청 Log
        /// </summary>
        RequestWriteMultiHoldingRegister = 0b_0000_0000_0001_0000_0000_0000,

        /// <summary>
        /// 통신 오류
        /// </summary>
        CommError = 0b_0000_0010_0000_0000_0000_0000,
        /// <summary>
        /// Modbus Exception 코드 수신 오류
        /// </summary>
        ResponseException = 0b_0000_0001_0000_0000_0000_0000,
    }

    /// <summary>
    /// Modbus 엔디안
    /// </summary>
    [Flags]
    public enum ModbusEndian : byte
    {
        /// <summary>
        /// 전체 리틀 엔디안
        /// </summary>
        [Description("DCBA")]
        AllLittle = 0b00,
        /// <summary>
        /// Word(2Byte) 내부 빅 엔디안, Word 단위는 리틀 엔디안
        /// </summary>
        [Description("CDAB")]
        InnerBig = 0b01,
        /// <summary>
        /// Word(2Byte) 단위 빅 엔디안, Word 내부는 리틀 엔디안
        /// </summary>
        [Description("BADC")]
        OuterBig = 0b10,
        /// <summary>
        /// 전체 빅 엔디안
        /// </summary>
        [Description("ABCD")]
        AllBig = 0b11,
    }
}
