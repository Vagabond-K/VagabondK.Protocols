using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.LSElectric.FEnet;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) FEnet 프로토콜 오류 발생 Log
    /// </summary>
    public class FEnetNAKLog : FEnetResponseLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="message">FEnet 오류 응답 메시지</param>
        /// <param name="rawMessage">원본 메시지</param>
        /// <param name="requestLog">관련 요청 메시지에 대한 Log</param>
        public FEnetNAKLog(IChannel channel, FEnetNAKResponse message, byte[] rawMessage, FEnetRequestLog requestLog) : base(channel, message, rawMessage, requestLog)
        {
            NAKCode = message.NAKCode;
        }

        /// <summary>
        /// FEnet Error 코드
        /// </summary>
        public FEnetNAKCode NAKCode { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString()
        {
            var stringBuilder = new StringBuilder("NAK: ");
            stringBuilder.Append('"');
            stringBuilder.Append(Encoding.ASCII.GetString(RawMessage as byte[], 0, 10).Replace("\0", "\\0"));
            stringBuilder.Append('"');
            stringBuilder.Append(' ');
            stringBuilder.Append(BitConverter.ToString(RawMessage as byte[], 10, 2).Replace("-", ""));
            stringBuilder.Append(' ');
            stringBuilder.Append(BitConverter.ToString(RawMessage as byte[], 12, 1));
            stringBuilder.Append(' ');
            stringBuilder.Append(BitConverter.ToString(RawMessage as byte[], 13, 1));
            stringBuilder.Append(' ');
            stringBuilder.Append(BitConverter.ToString(RawMessage as byte[], 14, 2).Replace("-", ""));
            stringBuilder.Append(' ');
            stringBuilder.Append(BitConverter.ToString(RawMessage as byte[], 16, 2).Replace("-", ""));
            stringBuilder.Append(' ');
            stringBuilder.Append(BitConverter.ToString(RawMessage as byte[], 18, 1));
            stringBuilder.Append(' ');
            stringBuilder.Append(BitConverter.ToString(RawMessage as byte[], 19, 1));
            stringBuilder.Append(' ');
            stringBuilder.Append(BitConverter.ToString(RawMessage as byte[], 20, 2).Replace("-", ""));
            stringBuilder.Append(' ');
            stringBuilder.Append(BitConverter.ToString(RawMessage as byte[], 22, 2).Replace("-", ""));
            stringBuilder.Append(' ');
            stringBuilder.Append(BitConverter.ToString(RawMessage as byte[], 24, 2).Replace("-", ""));
            stringBuilder.Append(' ');
            stringBuilder.Append(BitConverter.ToString(RawMessage as byte[], 26, 2).Replace("-", ""));
            stringBuilder.Append(' ');
            stringBuilder.Append(BitConverter.ToString(RawMessage as byte[], 28).Replace("-", ""));
            stringBuilder.Append(' ');
            var codeName = NAKCode.ToString();
            stringBuilder.Append($"Error: {(typeof(FEnetNAKCode).GetMember(codeName, BindingFlags.Static | BindingFlags.Public)?.FirstOrDefault()?.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute)?.Description ?? codeName}");

            return stringBuilder.ToString();
        }
    }
}
