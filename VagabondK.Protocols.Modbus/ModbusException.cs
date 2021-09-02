using System;
using System.Collections.Generic;
using System.Text;

namespace VagabondK.Protocols.Modbus
{
    /// <summary>
    /// Modbus Exception 코드 기반 예외
    /// </summary>
    public class ModbusException : ErrorCodeException<ModbusExceptionCode>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="code">Modbus Exception 코드</param>
        public ModbusException(ModbusExceptionCode code) : base(code) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="code">Modbus Exception 코드</param>
        /// <param name="innerException">내부 예외</param>
        public ModbusException(ModbusExceptionCode code, Exception innerException) : base(code, innerException) { }
    }
}
