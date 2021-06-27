using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;

namespace VagabondK.Protocols.LSElectric.Cnet.Logging
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 Log
    /// </summary>
    public abstract class CnetLog : ChannelLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        protected CnetLog(IChannel channel) : base(channel) { }
    }
}
