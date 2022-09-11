using System;
using System.Collections.Generic;
using System.Text;

namespace VagabondK.Protocols.LSElectric
{
    /// <summary>
    /// 디바이스 변수의 값을 정의합니다.
    /// </summary>
    public class DeviceVariableValue
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        public DeviceVariableValue(DeviceVariable deviceVariable)
        {
            DeviceVariable = deviceVariable;
        }

        /// <summary>
        /// 디바이스 변수
        /// </summary>
        public DeviceVariable DeviceVariable { get; }

        /// <summary>
        /// 디바이스 값
        /// </summary>
        public DeviceValue DeviceValue { get; set; }

        /// <summary>
        /// 디바이스 값의 바이트 배열을 가져옵니다.
        /// </summary>
        public byte[] DeviceValueBytes => DeviceValue.GetBytes(DeviceVariable.DataType);
    }
}
