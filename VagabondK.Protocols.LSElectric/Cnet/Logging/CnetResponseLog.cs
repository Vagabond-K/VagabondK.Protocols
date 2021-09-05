using VagabondK.Protocols.Channels;
using VagabondK.Protocols.LSElectric.Cnet;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 응답 메시지 Log
    /// </summary>
    public class CnetResponseLog : ChannelResponseLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="response">Cnet 응답 메시지</param>
        /// <param name="rawMessage">원본 메시지</param>
        /// <param name="requestLog">관련 요청 메시지에 대한 Log</param>
        public CnetResponseLog(IChannel channel, CnetResponse response, byte[] rawMessage, CnetRequestLog requestLog) : base(channel, response, rawMessage, requestLog)
        {
            CnetResponse = response;
        }

        /// <summary>
        /// Cnet 응답 메시지
        /// </summary>
        public CnetResponse CnetResponse { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString()
        {
            return $"Response: {RawMessage.CnetRawMessageToString()}";
        }
    }
}
