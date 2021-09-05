using System;
using System.Collections.Generic;
using System.Text;
using VagabondK.Protocols.Channels;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// 통신 채널을 통해 주고 받은 요청 메시지에 대한 Log
    /// </summary>
    public class ChannelRequestLog : ChannelMessageLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="request">요청 메시지 인스턴스</param>
        /// <param name="rawMessage">원본 메시지</param>
        public ChannelRequestLog(IChannel channel, IRequest request, byte[] rawMessage) : base(channel, request, rawMessage)
        {
            Request = request;
        }

        /// <summary>
        /// 요청 메시지 인스턴스
        /// </summary>
        public IRequest Request { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString() => $"Request: {base.ToString()}";
    }
}
