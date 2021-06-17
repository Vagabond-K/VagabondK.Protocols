using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace VagabondK.Protocols.Modbus
{
    /// <summary>
    /// Modbus 통신 오류 예외
    /// </summary>
    public class ModbusCommException : Exception
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="errorCode">Modbus 통신 오류 코드</param>
        /// <param name="innerException">내부 예외</param>
        public ModbusCommException(ModbusCommErrorCode errorCode, Exception innerException) : base(null, innerException)
        {
            Code = errorCode;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="innerException">내부 예외</param>
        /// <param name="request">Modbus 요청</param>
        public ModbusCommException(Exception innerException, ModbusRequest request) : base(null, innerException)
        {
            Code = ModbusCommErrorCode.NotDefined;
            ReceivedBytes = new byte[0];
            Request = request;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="receivedMessage">응답 메시지</param>
        /// <param name="innerException">내부 예외</param>
        /// <param name="request">Modbus 요청</param>
        public ModbusCommException(IEnumerable<byte> receivedMessage, Exception innerException, ModbusRequest request) : base(null, innerException)
        {
            Code = ModbusCommErrorCode.NotDefined;
            ReceivedBytes = receivedMessage?.ToArray() ?? new byte[0];
            Request = request;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="errorCode">Modbus 통신 오류 코드</param>
        /// <param name="receivedMessage">응답 메시지</param>
        /// <param name="request">Modbus 요청</param>
        public ModbusCommException(ModbusCommErrorCode errorCode, IEnumerable<byte> receivedMessage, ModbusRequest request)
        {
            Code = errorCode;
            ReceivedBytes = receivedMessage?.ToArray() ?? new byte[0];
            Request = request;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="errorCode">Modbus 통신 오류 코드</param>
        /// <param name="receivedMessage">받은 메시지</param>
        /// <param name="innerException">내부 예외</param>
        /// <param name="request">Modbus 요청</param>
        public ModbusCommException(ModbusCommErrorCode errorCode, IEnumerable<byte> receivedMessage, Exception innerException, ModbusRequest request) : base(null, innerException)
        {
            Code = errorCode;
            ReceivedBytes = receivedMessage?.ToArray() ?? new byte[0];
            Request = request;
        }

        /// <summary>
        /// Modbus 통신 오류 코드
        /// </summary>
        public ModbusCommErrorCode Code { get; }
        /// <summary>
        /// 받은 메시지
        /// </summary>
        public IReadOnlyList<byte> ReceivedBytes { get; }
        /// <summary>
        /// Modbus 요청
        /// </summary>
        public ModbusRequest Request { get; }

        /// <summary>
        /// 예외 메시지
        /// </summary>
        public override string Message
        {
            get
            {
                var codeName = Code.ToString();
                return (typeof(ModbusCommErrorCode).GetMember(codeName, BindingFlags.Static | BindingFlags.Public)?.FirstOrDefault()?.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute)?.Description ?? codeName;
            }
        }
    }
}
