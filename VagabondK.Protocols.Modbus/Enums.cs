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

}
