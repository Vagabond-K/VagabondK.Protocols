using System;
using System.Collections.Generic;
using System.Linq;
using VagabondK.Protocols.Logging;

namespace VagabondK.Protocols.Channels
{
    /// <summary>
    /// 통신 채널 공급자
    /// </summary>
    public abstract class ChannelProvider : IChannel
    {
        /// <summary>
        /// 채널 생성 이벤트
        /// </summary>
        public event EventHandler<ChannelCreatedEventArgs> Created;

        /// <summary>
        /// 생성된 채널 목록
        /// </summary>
        public abstract IReadOnlyList<Channel> Channels { get; }

        /// <summary>
        /// 리소스 해제 여부
        /// </summary>
        public bool IsDisposed { get; protected set; }

        /// <summary>
        /// 통신 채널 Logger
        /// </summary>
        public IChannelLogger Logger { get; set; }

        /// <summary>
        /// 채널 공급자 설명
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// 채널 생성 시작
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// 채널 생성 정지
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// 채널 생성 이벤트 호출
        /// </summary>
        /// <param name="eventArgs">이벤트 매개변수</param>
        protected void RaiseCreatedEvent(ChannelCreatedEventArgs eventArgs) => Created?.Invoke(this, eventArgs);

        /// <summary>
        /// ChannelProvider로 생성된 통신 채널 중 하나를 선택
        /// </summary>
        /// <returns>선택된 통신 채널</returns>
        protected virtual Channel OnSelectPrimaryChannel() => Channels?.LastOrDefault();

        /// <summary>
        /// 주요 사용 채널
        /// </summary>
        public Channel PrimaryChannel { get => OnSelectPrimaryChannel(); }


        /// <summary>
        /// 바이트 배열 쓰기
        /// </summary>
        /// <param name="bytes">바이트 배열</param>
        public void Write(byte[] bytes) => PrimaryChannel.Write(bytes);

        /// <summary>
        /// 1 바이트 읽기
        /// </summary>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>읽은 바이트</returns>
        public byte Read(int timeout) => PrimaryChannel.Read(timeout);

        /// <summary>
        /// 여러 개의 바이트 읽기
        /// </summary>
        /// <param name="count">읽을 개수</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>읽은 바이트 열거</returns>
        public IEnumerable<byte> Read(uint count, int timeout) => PrimaryChannel.Read(count, timeout);

        /// <summary>
        /// 채널에 남아있는 모든 바이트 읽기
        /// </summary>
        /// <returns>읽은 바이트 열거</returns>
        public IEnumerable<byte> ReadAllRemain() => PrimaryChannel.ReadAllRemain();

        /// <summary>
        /// 수신 버퍼에 있는 데이터의 바이트 수입니다.
        /// </summary>
        public uint BytesToRead { get => PrimaryChannel.BytesToRead; }
    }

    /// <summary>
    /// 통신 채널 생성 이벤트 매개변수
    /// </summary>
    public class ChannelCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        public ChannelCreatedEventArgs(Channel channel)
        {
            Channel = channel;
        }

        /// <summary>
        /// 통신 채널
        /// </summary>
        public Channel Channel { get; }
    }
}
