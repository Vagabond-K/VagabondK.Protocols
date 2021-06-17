using System.Collections.Generic;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// 컬렉션 기반 통신 채널 Logger
    /// </summary>
    public class CollectionChannelLogger : IChannelLogger
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="collection">통신 채널 Log 컬렉션</param>
        public CollectionChannelLogger(ICollection<ChannelLog> collection)
        {
            Collection = collection;
        }

        /// <summary>
        /// 통신 채널 Log 컬렉션
        /// </summary>
        public ICollection<ChannelLog> Collection { get; set; }

        /// <summary>
        /// 통신 채널 Log 기록
        /// </summary>
        /// <param name="log">통신 채널 Log</param>
        public void Log(ChannelLog log)
        {
            Collection?.Add(log);
        }
    }
}
