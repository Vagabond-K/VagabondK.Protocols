using System;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// 콘솔 기반 통신 채널 Logger
    /// </summary>
    public class ConsoleChannelLogger : IChannelLogger
    {
        /// <summary>
        /// 통신 채널 Log 기록
        /// </summary>
        /// <param name="log">통신 채널 Log</param>
        public void Log(ChannelLog log)
        {
            Console.Write($"({log.ChannelDescription}) ");
            Console.WriteLine(log);
        }
    }
}
