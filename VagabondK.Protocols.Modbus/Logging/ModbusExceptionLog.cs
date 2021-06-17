using System.ComponentModel;
using System.Linq;
using System.Reflection;
using VagabondK.Protocols.Channels;

namespace VagabondK.Protocols.Modbus.Logging
{
    /// <summary>
    /// Modbus Exception Log
    /// </summary>
    public class ModbusExceptionLog : ModbusLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="exceptionCode">Modbus Exception 코드</param>
        /// <param name="rawMessage">원본 메시지</param>
        public ModbusExceptionLog(IChannel channel, ModbusExceptionCode exceptionCode, byte[] rawMessage) : base(channel)
        {
            ExceptionCode = exceptionCode;
            RawMessage = rawMessage;
        }

        /// <summary>
        /// Modbus Exception 코드
        /// </summary>
        public ModbusExceptionCode ExceptionCode { get; }

        /// <summary>
        /// 원본 메시지
        /// </summary>
        public byte[] RawMessage { get; }

        /// <summary>
        /// Modbus Log 카테고리
        /// </summary>
        public override ModbusLogCategory Category { get => ModbusLogCategory.ResponseException; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString()
        {
            var codeName = ExceptionCode.ToString();
            return $"({ChannelDescription}) Exception: {(typeof(ModbusExceptionCode).GetMember(codeName, BindingFlags.Static | BindingFlags.Public)?.FirstOrDefault()?.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute)?.Description ?? codeName}";
        }
    }
}
