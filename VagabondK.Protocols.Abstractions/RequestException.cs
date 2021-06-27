using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace VagabondK.Protocols
{
    /// <summary>
    /// 통신 요청 오류 예외
    /// </summary>
    /// <typeparam name="TErrorCode">오류 코드</typeparam>
    public class RequestException<TErrorCode> : ErrorCodeException<TErrorCode> where TErrorCode : Enum
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="errorCode">통신 요청 오류 코드</param>
        /// <param name="innerException">내부 예외</param>
        public RequestException(TErrorCode errorCode, Exception innerException) : base(errorCode, innerException)
        {
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="innerException">내부 예외</param>
        /// <param name="request">Modbus 요청</param>
        public RequestException(Exception innerException, IRequest<TErrorCode> request) : base(default(TErrorCode), innerException)
        {
            ReceivedBytes = new byte[0];
            Request = request;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="receivedMessage">응답 메시지</param>
        /// <param name="innerException">내부 예외</param>
        /// <param name="request">Modbus 요청</param>
        public RequestException(IEnumerable<byte> receivedMessage, Exception innerException, IRequest<TErrorCode> request) : base(default(TErrorCode), innerException)
        {
            ReceivedBytes = receivedMessage?.ToArray() ?? new byte[0];
            Request = request;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="errorCode">통신 요청 오류 코드</param>
        /// <param name="receivedMessage">응답 메시지</param>
        /// <param name="request">Modbus 요청</param>
        public RequestException(TErrorCode errorCode, IEnumerable<byte> receivedMessage, IRequest<TErrorCode> request) : base(errorCode)
        {
            ReceivedBytes = receivedMessage?.ToArray() ?? new byte[0];
            Request = request;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="errorCode">통신 요청 오류 코드</param>
        /// <param name="receivedMessage">받은 메시지</param>
        /// <param name="innerException">내부 예외</param>
        /// <param name="request">Modbus 요청</param>
        public RequestException(TErrorCode errorCode, IEnumerable<byte> receivedMessage, Exception innerException, IRequest<TErrorCode> request) : base(errorCode, innerException)
        {
            ReceivedBytes = receivedMessage?.ToArray() ?? new byte[0];
            Request = request;
        }

        /// <summary>
        /// 받은 메시지
        /// </summary>
        public IReadOnlyList<byte> ReceivedBytes { get; }
        /// <summary>
        /// Modbus 요청
        /// </summary>
        public IRequest<TErrorCode> Request { get; }
    }
}
