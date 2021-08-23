using System;
using System.Collections.Generic;
using System.Text;
using VagabondK.Protocols.Channels;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// 통신 채널을 통해 주고 받은 메시지에 대한 Log
    /// </summary>
    public abstract class ChannelMessageLog : ChannelLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="message">프로토콜 메시지 인스턴스</param>
        /// <param name="rawMessage">원본 메시지</param>
        protected ChannelMessageLog(IChannel channel, IProtocolMessage message, byte[] rawMessage) : base(channel)
        {
            Message = message;
            RawMessage = rawMessage ?? new byte[0];
        }

        /// <summary>
        /// 프로토콜 메시지 인스턴스
        /// </summary>
        public IProtocolMessage Message { get; }
        /// <summary>
        /// 원본 메시지
        /// </summary>
        public IReadOnlyList<byte> RawMessage { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString() => BitConverter.ToString(RawMessage as byte[]).Replace('-', ' ');
    }
}
