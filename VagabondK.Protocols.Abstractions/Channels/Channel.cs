using System.Collections.Generic;
using VagabondK.Protocols.Logging;

namespace VagabondK.Protocols.Channels
{
    /// <summary>
    /// 통신 채널
    /// </summary>
    public abstract class Channel : IChannel
    {
        /// <summary>
        /// 리소스 해제 여부
        /// </summary>
        public bool IsDisposed { get; protected set; }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// 바이트 배열 쓰기
        /// </summary>
        /// <param name="bytes">바이트 배열</param>
        public abstract void Write(byte[] bytes);

        /// <summary>
        /// 1 바이트 읽기
        /// </summary>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>읽은 바이트</returns>
        public abstract byte Read(int timeout);

        /// <summary>
        /// 여러 개의 바이트 읽기
        /// </summary>
        /// <param name="count">읽을 개수</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>읽은 바이트 열거</returns>
        public abstract IEnumerable<byte> Read(uint count, int timeout);

        /// <summary>
        /// 채널에 남아있는 모든 바이트 읽기
        /// </summary>
        /// <returns>읽은 바이트 열거</returns>
        public abstract IEnumerable<byte> ReadAllRemain();

        /// <summary>
        /// 통신 채널 Logger
        /// </summary>
        public IChannelLogger Logger { get; set; }

        /// <summary>
        /// 채널 설명
        /// </summary>
        public abstract string Description { get; }
    }
}
