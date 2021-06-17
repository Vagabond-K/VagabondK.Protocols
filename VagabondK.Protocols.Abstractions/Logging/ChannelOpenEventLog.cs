using VagabondK.Protocols.Channels;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// 채널 열림 이벤트 Log
    /// </summary>
    public class ChannelOpenEventLog : ChannelLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        public ChannelOpenEventLog(IChannel channel) : base(channel) { }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString()
            => $"({ChannelDescription}) Opened Channel";
    }
}
