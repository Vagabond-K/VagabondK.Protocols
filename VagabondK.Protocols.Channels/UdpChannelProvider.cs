using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VagabondK.Protocols.Logging;

namespace VagabondK.Protocols.Channels
{
    /// <summary>
    /// UDP 소켓 기반 통신 채널 공급자
    /// </summary>
    public class UdpChannelProvider : ChannelProvider
    {
        /// <summary>
        /// 생성자
        /// </summary>
        public UdpChannelProvider() : this(502) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="port">UDP 메시지 수신 포트</param>
        /// <param name="addressFamily">주소 지정 체계</param>
        public UdpChannelProvider(int port, AddressFamily addressFamily)
        {
            Port = port;
            AddressFamily = addressFamily;
            udpClient = new UdpClient(Port, AddressFamily);
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="port">UDP 메시지 수신 포트</param>
        public UdpChannelProvider(int port)
        {
            Port = port;
            udpClient = new UdpClient(port);
        }

        /// <summary>
        /// 주소 지정 체계
        /// </summary>
        public AddressFamily AddressFamily { get; }

        /// <summary>
        /// UDP 메시지 수신 포트
        /// </summary>
        public int Port { get; }

        private readonly UdpClient udpClient;
        private readonly Dictionary<string, WeakReference<UdpClientChannel>> channels = new Dictionary<string, WeakReference<UdpClientChannel>>();
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// 수신된 UDP 메시지의 원격 엔드포인트 기반 채널 목록
        /// </summary>
        public override IReadOnlyList<Channel> Channels { get => channels.Values.Select(w => w.TryGetTarget(out var channel) ? channel : null).Where(c => c != null).ToList(); }

        /// <summary>
        /// 채널 공급자 설명
        /// </summary>
        public override string Description { get => udpClient.Client?.LocalEndPoint?.ToString(); }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public override void Dispose()
        {
            lock (this)
            {
                if (!IsDisposed)
                {
                    IsDisposed = true;
                    Stop();
                }
            }
        }

        /// <summary>
        /// UDP 메시지 수신 시작
        /// </summary>
        public override void Start()
        {
            lock (this)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(UdpChannelProvider));

                cancellationTokenSource = new CancellationTokenSource();
                Task.Run(() =>
                {
                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        IPEndPoint remoteEndPoint = null;
                        try
                        {
                            var received = udpClient.Receive(ref remoteEndPoint);

                            lock (channels)
                            {
                                if (channels.TryGetValue(remoteEndPoint.ToString(), out var channelReference)
                                    && channelReference.TryGetTarget(out var channel))
                                {
                                    channel.AddReceivedMessage(received);
                                }
                                else
                                {
                                    channel = new UdpClientChannel(this, remoteEndPoint, received)
                                    {
                                        Logger = Logger
                                    };
                                    Logger?.Log(new ChannelOpenEventLog(channel));
                                    channels[channel.Description] = new WeakReference<UdpClientChannel>(channel);
                                    RaiseCreatedEvent(new ChannelCreatedEventArgs(channel));
                                }
                                foreach (var disposed in channels.Where(c => !c.Value.TryGetTarget(out var target)).Select(c => c.Key).ToArray())
                                    channels.Remove(disposed);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger?.Log(new ChannelErrorLog(this, ex));
                        }
                    }
                }, cancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// UDP 메시지 수신 정지
        /// </summary>
        public override void Stop()
        {
            lock (this)
            {
                cancellationTokenSource?.Cancel();
                udpClient.Close();

                foreach (var reference in channels.Values)
                {
                    if (reference.TryGetTarget(out var channel))
                    {
                        channel.Dispose();
                    }
                }
                channels.Clear();
            }
        }

        internal void RemoveChannel(string description)
        {
            lock (channels)
                channels?.Remove(description);
        }

        class UdpClientChannel : Channel
        {
            internal UdpClientChannel(UdpChannelProvider provider, IPEndPoint endPoint, byte[] received)
            {
                this.provider = provider;
                this.endPoint = endPoint;
                description = endPoint.ToString();

                AddReceivedMessage(received);
            }

            private readonly UdpChannelProvider provider;
            private readonly IPEndPoint endPoint;

            private readonly object writeLock = new object();
            private readonly object readLock = new object();
            private readonly Queue<byte> readBuffer = new Queue<byte>();
            private readonly EventWaitHandle readEventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            private readonly string description;

            internal void AddReceivedMessage(byte[] received)
            {
                foreach (var item in received)
                    readBuffer.Enqueue(item);
                readEventWaitHandle.Set();
            }

            public override string Description { get => description; }

            ~UdpClientChannel()
            {
                Dispose();
            }

            public override void Dispose()
            {
                if (!IsDisposed)
                {
                    provider?.RemoveChannel(Description);
                    IsDisposed = true;
                    Logger?.Log(new ChannelCloseEventLog(this));
                    readEventWaitHandle.Set();
                }
            }

            private byte? GetByte(int timeout)
            {
                lock (readBuffer)
                {
                    if (readBuffer.Count > 0)
                        return readBuffer.Dequeue();
                    else
                        readEventWaitHandle.Reset();
                }

                if (timeout == 0 ? readEventWaitHandle.WaitOne() : readEventWaitHandle.WaitOne(timeout))
                    return readBuffer.Count > 0 ? readBuffer.Dequeue() : (byte?)null;
                else
                    return null;
            }

            public override void Write(byte[] bytes)
            {
                lock (writeLock)
                {
                    try
                    {
                        provider?.udpClient?.Send(bytes, bytes.Length, endPoint);
                    }
                    catch
                    {
                        throw new TimeoutException();
                    }
                }
            }

            public override byte Read(int timeout)
            {
                lock (readLock)
                {
                    return GetByte(timeout) ?? throw new TimeoutException();
                }
            }

            public override IEnumerable<byte> Read(uint count, int timeout)
            {
                lock (readLock)
                {
                    for (int i = 0; i < count; i++)
                    {
                        yield return GetByte(timeout) ?? throw new TimeoutException();
                    }
                }
            }

            public override IEnumerable<byte> ReadAllRemain()
            {
                lock (readLock)
                {
                    while (readBuffer.Count > 0)
                        yield return readBuffer.Dequeue();
                }
            }

            public override uint BytesToRead
            {
                get
                {
                    return (uint)readBuffer.Count;
                }
            }

        }

    }
}
