using VagabondK.Protocols.Channels;
using VagabondK.Protocols.LSElectric.Cnet;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 요청 메시지 Log
    /// </summary>
    public class CnetRequestLog : ChannelRequestLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="message">Cnet 요청 메시지</param>
        /// <param name="rawMessage">원본 메시지</param>
        public CnetRequestLog(IChannel channel, CnetRequest message, byte[] rawMessage) : base(channel, message, rawMessage)
        {
            Message = message;
        }

        /// <summary>
        /// Cnet 요청 메시지
        /// </summary>
        public new CnetRequest Message { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString()
        {
            return $"Request: {RawMessage.CnetRawMessageToString()}";
        }
    }
}
