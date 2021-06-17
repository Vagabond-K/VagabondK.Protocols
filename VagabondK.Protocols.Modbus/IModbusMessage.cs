using System.Collections.Generic;
using VagabondK.Protocols.Modbus.Logging;

namespace VagabondK.Protocols.Modbus
{
    /// <summary>
    /// Modbus 메시지
    /// </summary>
    public interface IModbusMessage
    {
        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 된 바이트 열거</returns>
        IEnumerable<byte> Serialize();

        /// <summary>
        /// 트랜잭션 ID (Modbus TCP에서 사용)
        /// </summary>
        ushort TransactionID { get; set; }

        /// <summary>
        /// Modbus Log 카테고리
        /// </summary>
        ModbusLogCategory LogCategory { get; }
    }
}