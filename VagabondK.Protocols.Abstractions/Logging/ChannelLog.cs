using System;
using VagabondK.Protocols.Channels;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// 채널 Log
    /// </summary>
    public abstract class ChannelLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        protected ChannelLog(IChannel channel)
        {
            TimeStamp = DateTime.Now;
            ChannelDescription = channel?.Description;
        }

        /// <summary>
        /// 타임 스탬프
        /// </summary>
        public DateTime TimeStamp { get; }

        /// <summary>
        /// 채널 설명
        /// </summary>
        public string ChannelDescription { get; }
    }
}
