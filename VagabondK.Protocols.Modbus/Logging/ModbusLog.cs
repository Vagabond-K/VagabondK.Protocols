using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;

namespace VagabondK.Protocols.Modbus.Logging
{
    /// <summary>
    /// Modbus Log
    /// </summary>
    public abstract class ModbusLog : ChannelLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        protected ModbusLog(IChannel channel) : base(channel) { }

        /// <summary>
        /// Modbus Log 카테고리
        /// </summary>
        public abstract ModbusLogCategory Category { get; }
    }
}
