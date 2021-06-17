using System;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// 통신 채널 Log 수신기
    /// </summary>
    public class ChannelLogListener : IChannelLogger
    {
        /// <summary>
        /// 통신 채널 Log 발생 이벤트
        /// </summary>
        public event EventHandler<ChannelLoggedEventArgs> Logged;

        /// <summary>
        /// 통신 채널 Log 기록
        /// </summary>
        /// <param name="log">통신 채널 Log</param>
        public void Log(ChannelLog log)
        {
            Logged?.Invoke(this, new ChannelLoggedEventArgs(log));
        }
    }

    /// <summary>
    /// 통신 채널 Log 발생 이벤트 매개변수
    /// </summary>
    public class ChannelLoggedEventArgs : EventArgs
    {
        internal ChannelLoggedEventArgs(ChannelLog log)
        {
            Log = log;
        }

        /// <summary>
        /// 통신 채널 Log
        /// </summary>
        public ChannelLog Log { get; }
    }
}
