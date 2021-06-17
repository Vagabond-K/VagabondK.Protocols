namespace VagabondK.Protocols.Modbus.Logging
{
    /// <summary>
    /// Modbus Log 카테고리
    /// </summary>
    public enum ModbusLogCategory : int
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
}
