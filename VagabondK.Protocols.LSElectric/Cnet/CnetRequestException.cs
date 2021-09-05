using System;
using System.Collections.Generic;
using System.Text;

namespace VagabondK.Protocols.LSElectric.Cnet
{
    /// <summary>
    /// Cnet 요청 오류 예외
    /// </summary>
    public class CnetRequestException : RequestException<CnetCommErrorCode>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="errorCode">오류 코드</param>
        /// <param name="receivedMessage">응답 메시지</param>
        /// <param name="request">요청</param>
        public CnetRequestException(CnetCommErrorCode errorCode, IEnumerable<byte> receivedMessage, IRequest<CnetCommErrorCode> request) : base(errorCode, receivedMessage, request)
        {
        }
    }
}
