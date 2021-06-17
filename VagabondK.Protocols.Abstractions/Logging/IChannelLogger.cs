namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// 통신 채널 Logger 인터페이스
    /// </summary>
    public interface IChannelLogger
    {
        /// <summary>
        /// 통신 채널 Log 기록
        /// </summary>
        /// <param name="log">통신 채널 Log</param>
        void Log(ChannelLog log);
    }
}
