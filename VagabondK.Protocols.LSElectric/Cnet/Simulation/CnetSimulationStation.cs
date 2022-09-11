using System;
using System.Collections.Generic;
using System.Text;

namespace VagabondK.Protocols.LSElectric.Cnet.Simulation
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 가상 스테이션입니다.
    /// Cnet 클라이언트를 테스트하는 용도로 사용 가능합니다.
    /// </summary>
    public class CnetSimulationStation
    {
        /// <summary>
        /// 클라이언트에서 등록한 모니터 사전
        /// </summary>
        public Dictionary<byte, CnetMonitor> Monitors { get; } = new Dictionary<byte, CnetMonitor>();

        /// <summary>
        /// 읽기 요청 이벤트
        /// </summary>
        public event EventHandler<CnetRequestedReadEventArgs> RequestedRead;

        /// <summary>
        /// 쓰기 요청 이벤트
        /// </summary>
        public event EventHandler<CnetRequestedWriteEventArgs> RequestedWrite;

        internal void OnRequestedRead(CnetRequestedReadEventArgs eventArgs) => RequestedRead?.Invoke(this, eventArgs);
        internal void OnRequestedWrite(CnetRequestedWriteEventArgs eventArgs) => RequestedWrite?.Invoke(this, eventArgs);
    }
}
