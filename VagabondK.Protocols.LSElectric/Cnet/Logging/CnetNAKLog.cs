using System.ComponentModel;
using System.Linq;
using System.Reflection;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.LSElectric.Cnet;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 오류 발생 Log
    /// </summary>
    public class CnetNAKLog : CnetResponseLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="message">Cnet 오류 응답 메시지</param>
        /// <param name="rawMessage">원본 메시지</param>
        /// <param name="requestLog">관련 요청 메시지에 대한 Log</param>
        public CnetNAKLog(IChannel channel, CnetNAKResponse message, byte[] rawMessage, CnetRequestLog requestLog) : base(channel, message, rawMessage, requestLog)
        {
            NAKCode = message.NAKCode;
        }

        /// <summary>
        /// Cnet Error 코드
        /// </summary>
        public CnetNAKCode NAKCode { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString()
        {
            var codeName = NAKCode.ToString();
            return $"Exception: {(typeof(CnetNAKCode).GetMember(codeName, BindingFlags.Static | BindingFlags.Public)?.FirstOrDefault()?.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute)?.Description ?? codeName}";
        }
    }
}
