using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace VagabondK.Protocols.LSElectric.Cnet
{
    public abstract class CnetMonitor
    {
        protected CnetMonitor(CnetCommandType commandType)
        {
            CommandType = commandType;
        }

        /// <summary>
        /// 국번
        /// </summary>
        public byte StationNumber { get; set; }

        /// <summary>
        /// 국번
        /// </summary>
        public byte MonitorNumber { get; set; }

        /// <summary>
        /// 커맨드 타입
        /// </summary>
        public CnetCommandType CommandType { get; }


        public abstract CnetRegisterMonitorRequest CreateRegisterRequest(bool useBCC = true);
        public abstract CnetExecuteMonitorRequest CreateExecuteRequest(bool useBCC = true);
    }

    public class CnetMonitorByEachAddress : CnetMonitor
    {
        public CnetMonitorByEachAddress() : this(0, 0, null) { }
        public CnetMonitorByEachAddress(byte stationNumber, byte monitorNumber, IEnumerable<DeviceAddress> addresses) : base(CnetCommandType.Each)
        {
            StationNumber = stationNumber;
            MonitorNumber = monitorNumber;
            if (addresses != null)
                DeviceAddresses.AddRange(addresses);
        }

        public List<DeviceAddress> DeviceAddresses { get; } = new List<DeviceAddress>();

        public override CnetExecuteMonitorRequest CreateExecuteRequest(bool useBCC = true)
            => new CnetExecuteMonitorEachAddressRequest(new CnetRegisterMonitorEachAddressRequest(StationNumber, MonitorNumber, DeviceAddresses, useBCC));
        public override CnetRegisterMonitorRequest CreateRegisterRequest(bool useBCC = true)
            => new CnetRegisterMonitorEachAddressRequest(StationNumber, MonitorNumber, DeviceAddresses, useBCC);
    }

    public class CnetMonitorByAddressBlock : CnetMonitor
    {
        public CnetMonitorByAddressBlock() : this(0, 0) { }
        public CnetMonitorByAddressBlock(byte stationNumber, byte monitorNumber) : base(CnetCommandType.Each)
        {
            StationNumber = stationNumber;
            MonitorNumber = monitorNumber;
        }

        public DeviceAddress DeviceAddress { get; set; }
        public int Count { get; set; }

        public override CnetExecuteMonitorRequest CreateExecuteRequest(bool useBCC = true)
            => new CnetExecuteMonitorAddressBlockRequest(new CnetRegisterMonitorAddressBlockRequest(StationNumber, MonitorNumber, DeviceAddress, Count, useBCC));
        public override CnetRegisterMonitorRequest CreateRegisterRequest(bool useBCC = true)
            => new CnetRegisterMonitorAddressBlockRequest(StationNumber, MonitorNumber, DeviceAddress, Count, useBCC);
    }

}
