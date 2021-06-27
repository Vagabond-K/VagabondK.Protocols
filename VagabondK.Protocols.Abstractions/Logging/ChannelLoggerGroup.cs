using System;
using System.Collections;
using System.Collections.Generic;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// 채널 Logger 그룹
    /// </summary>
    public class ChannelLoggerGroup : IEnumerable<IChannelLogger>, IChannelLogger
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channelLoggers">채널 Logger 열거</param>
        public ChannelLoggerGroup(IEnumerable<IChannelLogger> channelLoggers)
        {
            foreach (var channelLogger in channelLoggers)
            {
                this.channelLoggers.Add(channelLogger);
            }
        }

        private readonly HashSet<IChannelLogger> channelLoggers = new HashSet<IChannelLogger>();

        /// <summary>
        /// 통신 채널 Logger 추가
        /// </summary>
        /// <param name="channelLogger">통신 채널 Logger</param>
        public void AddChannelLogger(IChannelLogger channelLogger)
        {
            lock (channelLoggers)
            {
                channelLoggers.Add(channelLogger);
            }
        }

        /// <summary>
        /// 통신 채널 Logger 제거
        /// </summary>
        /// <param name="channelLogger">통신 채널 Logger</param>
        /// <returns>제거 여부</returns>
        public bool RemoveChannelLogger(IChannelLogger channelLogger)
        {
            lock (channelLoggers)
            {
                return channelLoggers.Remove(channelLogger);
            }
        }

        /// <summary>
        /// 통신 채널 Log 기록
        /// </summary>
        /// <param name="log">통신 채널 Log</param>
        public void Log(ChannelLog log)
        {
            lock (channelLoggers)
            {
                foreach (var channelLogger in channelLoggers)
                {
                    channelLogger.Log(log);
                }
            }
        }

        /// <summary>
        /// 통신 채널 Logger 열거
        /// </summary>
        /// <returns>통신 채널 Logger 열거자</returns>
        public IEnumerator<IChannelLogger> GetEnumerator()
        {
            lock (channelLoggers)
            {
                foreach (var channelLogger in channelLoggers)
                    yield return channelLogger;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
