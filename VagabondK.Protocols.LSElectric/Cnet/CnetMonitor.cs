using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace VagabondK.Protocols.LSElectric.Cnet
{
    /// <summary>
    /// Cnet 프로토콜에서 사용하는 모니터를 정의합니다.
    /// </summary>
    public abstract class CnetMonitor
    {
        internal CnetMonitor(byte stationNumber, byte monitorNumber, CnetCommandType commandType)
        {
            StationNumber = stationNumber;
            MonitorNumber = monitorNumber;
            CommandType = commandType;
        }

        /// <summary>
        /// 국번
        /// </summary>
        public byte StationNumber { get; }

        /// <summary>
        /// 국번
        /// </summary>
        public byte MonitorNumber { get; }

        /// <summary>
        /// 커맨드 타입
        /// </summary>
        public CnetCommandType CommandType { get; }

        /// <summary>
        /// 모니터 등록 요청 생성
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <returns>모니터 등록 요청</returns>
        public abstract CnetRegisterMonitorRequest CreateRegisterRequest(bool useBCC = true);

        /// <summary>
        /// 모니터 실행 요청 생성
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <returns>모니터 실행 요청</returns>
        public abstract CnetExecuteMonitorRequest CreateExecuteRequest(bool useBCC = true);
    }

    /// <summary>
    /// 직접변수 개별 읽기 모니터
    /// </summary>
    public class CnetMonitorByIndividualAccess : CnetMonitor
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="deviceVariables">디바이스 변수 목록</param>
        public CnetMonitorByIndividualAccess(byte stationNumber, byte monitorNumber, IEnumerable<DeviceVariable> deviceVariables) : base(stationNumber, monitorNumber, CnetCommandType.Individual)
        {
            if (deviceVariables != null)
                DeviceVariables.AddRange(deviceVariables);
        }

        /// <summary>
        /// 디바이스 변수 목록
        /// </summary>
        public List<DeviceVariable> DeviceVariables { get; } = new List<DeviceVariable>();

        /// <summary>
        /// 모니터 등록 요청 생성
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <returns>모니터 등록 요청</returns>
        public override CnetExecuteMonitorRequest CreateExecuteRequest(bool useBCC = true)
            => new CnetExecuteMonitorIndividualRequest(new CnetRegisterMonitorIndividualRequest(StationNumber, MonitorNumber, DeviceVariables, useBCC));
        /// <summary>
        /// 모니터 실행 요청 생성
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <returns>모니터 실행 요청</returns>
        public override CnetRegisterMonitorRequest CreateRegisterRequest(bool useBCC = true)
            => new CnetRegisterMonitorIndividualRequest(StationNumber, MonitorNumber, DeviceVariables, useBCC);
    }

    /// <summary>
    /// 직접변수 연속 읽기 모니터
    /// </summary>
    public class CnetMonitorByContinuousAccess : CnetMonitor
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="count">읽을 개수</param>
        public CnetMonitorByContinuousAccess(byte stationNumber, byte monitorNumber, DeviceVariable startDeviceVariable, int count) : base(stationNumber, monitorNumber, CnetCommandType.Continuous)
        {
            StartDeviceVariable = startDeviceVariable;
            Count = count;
        }

        /// <summary>
        /// 시작 디바이스 변수
        /// </summary>
        public DeviceVariable StartDeviceVariable { get; }

        /// <summary>
        /// 읽을 개수
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// 모니터 등록 요청 생성
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <returns>모니터 등록 요청</returns>
        public override CnetExecuteMonitorRequest CreateExecuteRequest(bool useBCC = true)
            => new CnetExecuteMonitorContinuousRequest(new CnetRegisterMonitorContinuousRequest(StationNumber, MonitorNumber, StartDeviceVariable, Count, useBCC));
        /// <summary>
        /// 모니터 실행 요청 생성
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <returns>모니터 실행 요청</returns>
        public override CnetRegisterMonitorRequest CreateRegisterRequest(bool useBCC = true)
            => new CnetRegisterMonitorContinuousRequest(StationNumber, MonitorNumber, StartDeviceVariable, Count, useBCC);
    }

}
