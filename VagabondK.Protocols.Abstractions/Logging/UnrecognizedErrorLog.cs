using System;
using System.Collections.Generic;
using VagabondK.Protocols.Channels;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// 인식할 수 없는 메시지 수신 오류 Log
    /// </summary>
    public class UnrecognizedErrorLog : ChannelLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="rawMessage">원본 메시지</param>
        public UnrecognizedErrorLog(IChannel channel, byte[] rawMessage) : base(channel)
        {
            RawMessage = rawMessage ?? new byte[0];
        }

        /// <summary>
        /// 원본 메시지
        /// </summary>
        public IReadOnlyList<byte> RawMessage { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString()
            => RawMessage != null && RawMessage.Count > 0 ? $"Error Message: {BitConverter.ToString(RawMessage as byte[]).Replace('-', ' ')}" : base.ToString();
    }
}
