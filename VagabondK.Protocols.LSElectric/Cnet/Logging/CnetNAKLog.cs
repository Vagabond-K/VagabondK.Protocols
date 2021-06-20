using System.ComponentModel;
using System.Linq;
using System.Reflection;
using VagabondK.Protocols.Channels;

namespace VagabondK.Protocols.LSElectric.Cnet.Logging
{
    /// <summary>
    /// Cnet Error Log
    /// </summary>
    public class CnetNAKLog : CnetLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="nakCode">Cnet Error 코드</param>
        /// <param name="rawMessage">원본 메시지</param>
        public CnetNAKLog(IChannel channel, CnetNAKCode nakCode, byte[] rawMessage) : base(channel)
        {
            NAKCode = nakCode;
            RawMessage = rawMessage;
        }

        /// <summary>
        /// Cnet Error 코드
        /// </summary>
        public CnetNAKCode NAKCode { get; }

        /// <summary>
        /// 원본 메시지
        /// </summary>
        public byte[] RawMessage { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString()
        {
            var codeName = NAKCode.ToString();
            return $"({ChannelDescription}) Exception: {(typeof(CnetNAKCode).GetMember(codeName, BindingFlags.Static | BindingFlags.Public)?.FirstOrDefault()?.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute)?.Description ?? codeName}";
        }
    }
}
