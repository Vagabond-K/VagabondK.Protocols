using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace VagabondK.Protocols
{
    /// <summary>
    /// 오류 코드를 포함하는 예외
    /// </summary>
    /// <typeparam name="TErrorCode">오류 코드 형식</typeparam>
    public class ErrorCodeException<TErrorCode> : Exception where TErrorCode : Enum
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="code">Error Code</param>
        public ErrorCodeException(TErrorCode code)
        {
            Code = code;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="code">Error Code</param>
        /// <param name="innerException">내부 예외</param>
        public ErrorCodeException(TErrorCode code, Exception innerException) : base(null, innerException)
        {
            Code = code;
        }

        /// <summary>
        /// 오류 코드
        /// </summary>
        public TErrorCode Code { get; }

        /// <summary>
        /// 예외 메시지
        /// </summary>
        public override string Message
        {
            get
            {
                var codeName = Code.ToString();
                return (typeof(TErrorCode).GetMember(codeName, BindingFlags.Static | BindingFlags.Public)
                    ?.FirstOrDefault()?.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute)?.Description ?? codeName;
            }
        }
    }
}
