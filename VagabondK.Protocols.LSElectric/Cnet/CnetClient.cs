using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.LSElectric.Cnet.Logging;

namespace VagabondK.Protocols.LSElectric.Cnet
{
    /// <summary>
    /// LS ELECTRIC Cnet 프로토콜 기반 클라이언트입니다.
    /// XGT 시리즈 제품의 Cnet I/F 모듈과 통신 가능합니다.
    /// </summary>
    public class CnetClient : IDisposable
    {
        /// <summary>
        /// 생성자
        /// </summary>
        public CnetClient() { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        public CnetClient(IChannel channel)
        {
            this.channel = channel;
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            channel?.Dispose();
        }


        private IChannel channel;


        /// <summary>
        /// 통신 채널
        /// </summary>
        public IChannel Channel
        {
            get => channel;
            set
            {
                if (channel != value)
                {
                    channel = value;
                }
            }
        }


        /// <summary>
        /// 응답 제한시간(밀리초)
        /// </summary>
        public int Timeout { get; set; } = 1000;

        /// <summary>
        /// Error Code에 대한 예외 발생 여부
        /// </summary>
        public bool ThrowsExceptionFromNAK { get; set; } = true;


        /// <summary>
        /// Cnet 요청하기
        /// </summary>
        /// <param name="request">Cnet 요청</param>
        /// <returns>Cnet 응답</returns>
        public CnetResponse Request(CnetRequest request) => Request(request, Timeout);

        /// <summary>
        /// Cnet 요청하기
        /// </summary>
        /// <param name="request">Cnet 요청</param>
        /// <param name="timeout">응답 제한시간(밀리초)</param>
        /// <returns>Cnet 응답</returns>
        public CnetResponse Request(CnetRequest request, int timeout)
        {
            request = (CnetRequest)request.Clone();

            Channel channel = (Channel as Channel) ?? (Channel as ChannelProvider)?.PrimaryChannel;

            if (channel == null)
                throw new ArgumentNullException(nameof(Channel));


            var requestMessage = request.Serialize().ToArray();
            channel.ReadAllRemain().ToArray();
            channel.Write(requestMessage);
            channel?.Logger?.Log(new CnetMessageLog(channel, request, requestMessage));

            CnetResponse result;
            List<byte> buffer = new List<byte>();

            try
            {
                result = Deserialize(buffer, request, timeout);
            }
            catch (RequestException<CnetCommErrorCode> ex)
            {
                channel?.Logger?.Log(new ChannelErrorLog(channel, ex));
                throw ex;
            }

            if (result is CnetNAKResponse nakResponse)
            {
                channel?.Logger?.Log(new CnetNAKLog(channel, nakResponse.NAKCode, buffer.ToArray()));
                //if (ThrowsExceptionFromNAK)
                //    throw new CnetException(nakResponse.ExceptionCode);
            }
            else
                channel?.Logger?.Log(new CnetMessageLog(channel, result, result is CnetCommErrorResponse ? null : buffer.ToArray()));


            return result;
        }

        private CnetResponse Deserialize(List<byte> buffer, CnetRequest request, int timeout)
        {

            return null;
        }

        class CnetCommErrorResponse : CnetResponse
        {
            public CnetCommErrorResponse(CnetCommErrorCode errorCode, IEnumerable<byte> receivedMessage, CnetRequest request) : base(request)
            {
                ErrorCode = errorCode;
                ReceivedBytes = receivedMessage?.ToArray() ?? new byte[0];
            }

            public CnetCommErrorCode ErrorCode { get; }
            public IReadOnlyList<byte> ReceivedBytes { get; }

            public override byte Header => throw new NotImplementedException();

            public override IEnumerable<byte> Serialize()
            {
                return ReceivedBytes;
            }

            protected override void OnCreateFrame(List<byte> byteList, out bool useBCC)
            {
                useBCC = Request.UseBCC;
            }

            protected override void OnCreateFrameData(List<byte> byteList) { }

            public override string ToString()
            {
                string errorName = ErrorCode.ToString();

                if (ReceivedBytes != null && ReceivedBytes.Count > 0)
                    return $"{errorName}: {BitConverter.ToString(ReceivedBytes as byte[])}";
                else
                    return errorName;
            }
        }
    }
}