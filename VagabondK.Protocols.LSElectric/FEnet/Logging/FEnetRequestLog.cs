using System;
using System.Text;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.LSElectric.FEnet;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) FEnet 프로토콜 요청 메시지 Log
    /// </summary>
    public class FEnetRequestLog : ChannelRequestLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="request">FEnet 요청 메시지</param>
        /// <param name="rawMessage">원본 메시지</param>
        public FEnetRequestLog(IChannel channel, FEnetRequest request, byte[] rawMessage) : base(channel, request, rawMessage)
        {
            FEnetRequest = request;
        }

        /// <summary>
        /// FEnet 요청 메시지
        /// </summary>
        public FEnetRequest FEnetRequest { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString()
        {
            var stringBuilder = new StringBuilder("REQ: ");
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

            return stringBuilder.ToString();
        }
    }
}
