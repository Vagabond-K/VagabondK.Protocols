using System.Collections.Generic;
using System.Linq;

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
        /// 모니터 번호
        /// </summary>
        public byte MonitorNumber { get; }

        /// <summary>
        /// 커맨드 타입
        /// </summary>
        public CnetCommandType CommandType { get; }

        /// <summary>
        /// 모니터 등록 요청 생성
        /// </summary>
        /// <returns>모니터 등록 요청</returns>
        public abstract CnetRegisterMonitorRequest CreateRegisterRequest();

        /// <summary>
        /// 모니터 실행 요청 생성
        /// </summary>
        /// <returns>모니터 실행 요청</returns>
        public abstract CnetExecuteMonitorRequest CreateExecuteRequest();
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
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="moreDeviceVariables">추가로 읽을 디바이스 변수 목록</param>
        public CnetMonitorByIndividualAccess(byte stationNumber, byte monitorNumber, DeviceVariable deviceVariable, params DeviceVariable[] moreDeviceVariables) : base(stationNumber, monitorNumber, CnetCommandType.Individual)
        {
            DeviceVariables = new DeviceVariable[] { deviceVariable }.Concat(moreDeviceVariables).ToArray();
        }

        internal CnetMonitorByIndividualAccess(byte stationNumber, byte monitorNumber, IEnumerable<DeviceVariable> deviceVariables) : base(stationNumber, monitorNumber, CnetCommandType.Individual)
        {
            DeviceVariables = deviceVariables.ToArray();
        }


        /// <summary>
        /// 디바이스 변수 목록
        /// </summary>
        public IReadOnlyList<DeviceVariable> DeviceVariables { get; } = new List<DeviceVariable>();

        /// <summary>
        /// 모니터 등록 요청 생성
        /// </summary>
        /// <returns>모니터 등록 요청</returns>
        public override CnetRegisterMonitorRequest CreateRegisterRequest()
            => new CnetRegisterMonitorIndividualRequest(StationNumber, MonitorNumber, DeviceVariables);

        /// <summary>
        /// 모니터 실행 요청 생성
        /// </summary>
        /// <returns>모니터 실행 요청</returns>
        public override CnetExecuteMonitorRequest CreateExecuteRequest()
            => new CnetExecuteMonitorIndividualRequest(new CnetRegisterMonitorIndividualRequest(StationNumber, MonitorNumber, DeviceVariables));
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
        /// <returns>모니터 등록 요청</returns>
        public override CnetRegisterMonitorRequest CreateRegisterRequest()
            => new CnetRegisterMonitorContinuousRequest(StationNumber, MonitorNumber, StartDeviceVariable, Count);

        /// <summary>
        /// 모니터 실행 요청 생성
        /// </summary>
        /// <returns>모니터 실행 요청</returns>
        public override CnetExecuteMonitorRequest CreateExecuteRequest()
            => new CnetExecuteMonitorContinuousRequest(new CnetRegisterMonitorContinuousRequest(StationNumber, MonitorNumber, StartDeviceVariable, Count));
    }

}
