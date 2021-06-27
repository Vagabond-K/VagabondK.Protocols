using System;
using System.Text;
using VagabondK.Protocols.Channels;

namespace VagabondK.Protocols.LSElectric.Cnet.Logging
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 메시지 Log
    /// </summary>
    public class CnetMessageLog : CnetLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="message">Cnet 메시지</param>
        /// <param name="rawMessage">원본 메시지</param>
        public CnetMessageLog(IChannel channel, CnetMessage message, byte[] rawMessage) : base(channel)
        {
            Message = message;
            RawMessage = rawMessage ?? new byte[0];
        }

        /// <summary>
        /// Cnet 메시지
        /// </summary>
        public CnetMessage Message { get; }
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
            if (Message is CnetRequest)
                return $"({ChannelDescription}) Request: {RawMessageToString()}";
            else if (Message is CnetResponse)
                return $"({ChannelDescription}) Response: {RawMessageToString()}";
            else
                return base.ToString();
        }

        private string RawMessageToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var b in RawMessage)
            {
                switch (b)
                {
                    case CnetMessage.ENQ:
                        stringBuilder.Append($"{{{nameof(CnetMessage.ENQ)}}}");
                        break;
                    case CnetMessage.EOT:
                        stringBuilder.Append($"{{{nameof(CnetMessage.EOT)}}}");
                        break;
                    case CnetMessage.ACK:
                        stringBuilder.Append($"{{{nameof(CnetMessage.ACK)}}}");
                        break;
                    case CnetMessage.NAK:
                        stringBuilder.Append($"{{{nameof(CnetMessage.NAK)}}}");
                        break;
                    case CnetMessage.ETX:
                        stringBuilder.Append($"{{{nameof(CnetMessage.ETX)}}}");
                        break;
                    default:
                        if (b >= 33 && b <= 126)
                            stringBuilder.Append((char)b);
                        else
                            stringBuilder.Append(b.ToString("X2"));
                        break;
                }
            }
            return stringBuilder.ToString();
        }
    }
}
