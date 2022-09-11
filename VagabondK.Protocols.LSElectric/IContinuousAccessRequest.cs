using System;
using System.Collections.Generic;
using System.Text;

namespace VagabondK.Protocols.LSElectric
{
    /// <summary>
    /// 연속 디바이스 변수 액세스 요청 인터페이스
    /// </summary>
    public interface IContinuousAccessRequest
    {
        /// <summary>
        /// 시작 디바이스 변수
        /// </summary>
        DeviceVariable StartDeviceVariable { get; }

        /// <summary>
        /// 연속 액세스 개수
        /// </summary>
        int Count { get; }
    }


    /// <summary>
    /// 연속 디바이스 변수 액세스 요청에 대한 확장 메서드 모음
    /// </summary>
    public static class ContinuousRequestExtensions
    {
        /// <summary>
        /// 시작 디바이스 변수로부터 연속으로 읽을 변수들을 목록으로 변환
        /// </summary>
        /// <param name="request">연속 디바이스 변수 액세스 요청</param>
        /// <returns>디바이스 변수 목록</returns>
        public static IEnumerable<DeviceVariable> ToDeviceVariables(this IContinuousAccessRequest request)
        {
            var deviceVariable = request.StartDeviceVariable;
            for (int i = 0; i < request.Count; i++)
            {
                yield return deviceVariable;
                deviceVariable = deviceVariable.Increase();
            }
        }
    }
}
