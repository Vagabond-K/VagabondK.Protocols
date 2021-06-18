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
    /// TCP 서버 기반 통신 채널 공급자
    /// </summary>
    public class TcpServerChannelProvider : ChannelProvider
    {
        /// <summary>
        /// 생성자
        /// </summary>
        public TcpServerChannelProvider() : this(502) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="port">TCP 연결 수신 포트</param>
        public TcpServerChannelProvider(int port) : this(IPAddress.Any, port) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="ipAddress">로컬 IP 주소</param>
        /// <param name="port">TCP 연결 수신 포트</param>
        public TcpServerChannelProvider(IPAddress ipAddress, int port)
        {
            IPAddress = ipAddress;
            Port = port;
            tcpListener = new TcpListener(IPAddress, Port);
        }

        /// <summary>
        /// 로컬 IP 주소
        /// </summary>
        public IPAddress IPAddress { get; }

        /// <summary>
        /// TCP 연결 수신 포트
        /// </summary>
        public int Port { get; }

        private readonly TcpListener tcpListener;
        internal readonly Dictionary<Guid, WeakReference<TcpClientChannel>> channels = new Dictionary<Guid, WeakReference<TcpClientChannel>>();
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// 연결 요청 들어온 TCP 클라이언트 채널 목록
        /// </summary>
        public override IReadOnlyList<Channel> Channels { get => channels.Values.Select(w => w.TryGetTarget(out var channel) ? channel : null).Where(c => c != null).ToList(); }

        /// <summary>
        /// 채널 공급자 설명
        /// </summary>
        public override string Description { get => tcpListener?.LocalEndpoint?.ToString(); }

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
        /// TCP 서버 수신 시작
        /// </summary>
        public override void Start()
        {
            lock (this)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(TcpServerChannelProvider));

                cancellationTokenSource = new CancellationTokenSource();
                tcpListener.Start();
                Task.Run(() =>
                {
                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        var tcpClient = tcpListener.AcceptTcpClient();

                        var channel = new TcpClientChannel(this, tcpClient)
                        {
                            Logger = Logger
                        };
                        Logger?.Log(new ChannelOpenEventLog(channel));
                        channels[channel.Guid] = new WeakReference<TcpClientChannel>(channel);
                        RaiseCreatedEvent(new ChannelCreatedEventArgs(channel));
                        foreach (var disposed in channels.Where(c => !c.Value.TryGetTarget(out var target)).Select(c => c.Key).ToArray())
                            channels.Remove(disposed);
                    }
                }, cancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// TCP 서버 수신 정지
        /// </summary>
        public override void Stop()
        {
            lock (this)
            {
                cancellationTokenSource?.Cancel();
                tcpListener?.Stop();
            }
        }
    }
}
