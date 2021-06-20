using System;
using VagabondK.Protocols.Channels;

namespace VagabondK.Protocols.LSElectric.Cnet.Logging
{
    /// <summary>
    /// Cnet 메시지 Log
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
                return $"({ChannelDescription}) Request: {BitConverter.ToString(RawMessage)}";
            else if (Message is CnetResponse)
                return $"({ChannelDescription}) Response: {BitConverter.ToString(RawMessage)}";
            else
                return base.ToString();

        }
    }
}
