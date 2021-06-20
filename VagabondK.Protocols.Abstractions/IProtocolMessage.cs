using System.Collections.Generic;

namespace VagabondK.Protocols
{
    /// <summary>
    /// 프로토콜 메시지
    /// </summary>
    public interface IProtocolMessage
    {
        /// <summary>
        /// 직렬화
        /// </summary>
        /// <returns>직렬화 된 바이트 열거</returns>
        IEnumerable<byte> Serialize();
    }
}
