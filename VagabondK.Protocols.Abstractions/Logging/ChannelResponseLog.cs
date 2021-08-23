using System;
using System.Collections.Generic;
using System.Text;
using VagabondK.Protocols.Channels;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// 통신 채널을 통해 주고 받은 응답 메시지에 대한 Log
    /// </summary>
    public class ChannelResponseLog : ChannelMessageLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="message">응답 메시지 인스턴스</param>
        /// <param name="rawMessage">원본 메시지</param>
        /// <param name="requestLog">관련 요청 메시지에 대한 Log</param>
        public ChannelResponseLog(IChannel channel, IResponse message, byte[] rawMessage, ChannelRequestLog requestLog) : base(channel, message, rawMessage)
        {
            Message = message;
            RequestLog = requestLog;
        }

        /// <summary>
        /// 관련 요청 메시지에 대한 Log
        /// </summary>
        public ChannelRequestLog RequestLog { get; }

        /// <summary>
        /// 응답 메시지 인스턴스
        /// </summary>
        public new IResponse Message { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString() => $"Response: {base.ToString()}";
    }
}
