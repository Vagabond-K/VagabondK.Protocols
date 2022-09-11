using System;
using System.Collections.Generic;
using System.Text;

namespace VagabondK.Protocols.LSElectric.FEnet
{
    /// <summary>
    /// FEnet 요청 오류 예외
    /// </summary>
    public class FEnetNAKException : ErrorCodeException<FEnetNAKCode>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="nakCode">NAK 에러 코드</param>
        public FEnetNAKException(FEnetNAKCode nakCode) : base(nakCode)
        {
            NAKCodeValue = (ushort)nakCode;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="nakCode">NAK 에러 코드</param>
        /// <param name="nakCodeValue">오류 코드 원본 값</param>
        public FEnetNAKException(FEnetNAKCode nakCode, ushort nakCodeValue) : base(nakCode)
        {
            NAKCodeValue = nakCodeValue;
        }

        /// <summary>
        /// 오류 코드 원본 값
        /// </summary>
        public ushort NAKCodeValue { get; }
    }
}
