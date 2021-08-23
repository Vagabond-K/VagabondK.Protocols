using System;
using VagabondK.Protocols.Channels;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// 통신 채널 오류 Log
    /// </summary>
    public class ChannelErrorLog : ChannelLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="exception">통신 오류 예외</param>
        public ChannelErrorLog(IChannel channel, Exception exception) : base(channel)
        {
            Exception = exception;
        }

        /// <summary>
        /// 통신 오류 예외
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString() => $"Comm Error: {Exception?.Message ?? Exception.ToString()}";
    }
}
