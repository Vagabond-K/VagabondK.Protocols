using System.ComponentModel;
using System.Linq;
using System.Reflection;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// Modbus Exception Log
    /// </summary>
    public class ModbusExceptionLog : ChannelResponseLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="message">응답 메시지 인스턴스</param>
        /// <param name="rawMessage">원본 메시지</param>
        /// <param name="requestLog">관련 요청 메시지에 대한 Log</param>
        public ModbusExceptionLog(IChannel channel, ModbusExceptionResponse message, byte[] rawMessage, ChannelRequestLog requestLog) : base(channel, message, rawMessage, requestLog)
        {
            ExceptionCode = message.ExceptionCode;
        }

        /// <summary>
        /// Modbus Exception 코드
        /// </summary>
        public ModbusExceptionCode ExceptionCode { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString()
        {
            var codeName = ExceptionCode.ToString();
            return $"Exception: {(typeof(ModbusExceptionCode).GetMember(codeName, BindingFlags.Static | BindingFlags.Public)?.FirstOrDefault()?.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute)?.Description ?? codeName}";
        }
    }
}
