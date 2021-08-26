using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VagabondK.Protocols.Logging;

namespace VagabondK.Protocols.Channels
{
    /// <summary>
    /// TCP 클라이언트 기반 통신 채널
    /// </summary>
    public class TcpChannel : Channel
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="host">호스트</param>
        /// <param name="port">포트</param>
        public TcpChannel(string host, int port) : this(host, port, 10000) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="host">호스트</param>
        /// <param name="port">포트</param>
        /// <param name="connectTimeout">연결 제한시간(밀리초)</param>
        public TcpChannel(string host, int port, int connectTimeout)
        {
            Host = host;
            Port = port;
            ConnectTimeout = connectTimeout;
        }

        internal TcpChannel(TcpChannelProvider provider, TcpClient tcpClient)
        {
            Guid = Guid.NewGuid();

            this.provider = provider;
            this.tcpClient = tcpClient;
            description = tcpClient.Client.RemoteEndPoint.ToString();
        }

        /// <summary>
        /// 호스트
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// 포트
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// 연결 제한시간(밀리초)
        /// </summary>
        public int ConnectTimeout { get; }

        internal Guid Guid { get; }
        private readonly TcpChannelProvider provider;

        private TcpClient tcpClient = null;
        private readonly object connectLock = new object();
        private readonly object writeLock = new object();
        private readonly object readLock = new object();
        private readonly Queue<byte> readBuffer = new Queue<byte>();
        private readonly EventWaitHandle readEventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        private bool isRunningReceive = false;
        private string description;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// 채널 설명
        /// </summary>
        public override string Description { get => description; }

        /// <summary>
        /// 소멸자
        /// </summary>
        ~TcpChannel()
        {
            Dispose();
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public override void Dispose()
        {
            if (!IsDisposed)
            {
                provider?.channels?.Remove(Guid);
                IsDisposed = true;

                Close();
            }
        }

        private void Close()
        {
            try
            {
                cancellationTokenSource?.Cancel();
            }
            catch { }
            cancellationTokenSource = new CancellationTokenSource();
            lock (connectLock)
            {
                if (tcpClient != null)
                {
                    Logger?.Log(new ChannelCloseEventLog(this));
                    tcpClient.Close();
                    tcpClient = null;
                }
            }
        }

        private void CheckConnection()
        {
            if (provider != null) return;

            lock (connectLock)
            {
                if (!IsDisposed && tcpClient == null)
                {
                    tcpClient = new TcpClient();
                    try
                    {
                        Task task = tcpClient.ConnectAsync(Host ?? string.Empty, Port);
                        if (!task.Wait(ConnectTimeout, cancellationTokenSource.Token))
                            throw new SocketException(10060);

                        description = tcpClient.Client.RemoteEndPoint.ToString();
                        Logger?.Log(new ChannelOpenEventLog(this));
                    }
                    catch (Exception ex)
                    {
                        tcpClient?.Client?.Dispose();
                        tcpClient = null;
                        Logger?.Log(new ChannelErrorLog(this, ex));
                        throw ex;
                    }
                }
            }
        }

        private byte? GetByte(int timeout)
        {
            lock (readBuffer)
            {
                if (readBuffer.Count == 0)
                {
                    readEventWaitHandle.Reset();

                    Task.Run(() =>
                    {
                        if (!isRunningReceive)
                        {
                            isRunningReceive = true;
                            try
                            {
                                CheckConnection();
                                if (tcpClient != null)
                                {
                                    byte[] buffer = new byte[8192];
                                    while (true)
                                    {
                                        int received = tcpClient.Client.Receive(buffer);
                                        lock (readBuffer)
                                        {
                                            for (int i = 0; i < received; i++)
                                                readBuffer.Enqueue(buffer[i]);
                                            readEventWaitHandle.Set();
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                Close();
                            }
                            readEventWaitHandle.Set();
                            isRunningReceive = false;
                        }
                    }, cancellationTokenSource.Token);
                }
                else return readBuffer.Dequeue();
            }

            if (timeout == 0 ? readEventWaitHandle.WaitOne() : readEventWaitHandle.WaitOne(timeout))
                return readBuffer.Count > 0 ? readBuffer.Dequeue() : (byte?)null;
            else
                return null;
        }

        /// <summary>
        /// 바이트 배열 쓰기
        /// </summary>
        /// <param name="bytes">바이트 배열</param>
        public override void Write(byte[] bytes)
        {
            CheckConnection();
            lock (writeLock)
            {
                try
                {
                    if (tcpClient?.Client?.Connected == true)
                        tcpClient?.Client?.Send(bytes);
                }
                catch (Exception ex)
                {
                    Close();
                    throw ex;
                }
            }
        }

        /// <summary>
        /// 1 바이트 읽기
        /// </summary>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>읽은 바이트</returns>
        public override byte Read(int timeout)
        {
            lock (readLock)
            {
                return GetByte(timeout) ?? throw new TimeoutException();
            }
        }

        /// <summary>
        /// 여러 개의 바이트 읽기
        /// </summary>
        /// <param name="count">읽을 개수</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>읽은 바이트 열거</returns>
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

        /// <summary>
        /// 채널에 남아있는 모든 바이트 읽기
        /// </summary>
        /// <returns>읽은 바이트 열거</returns>
        public override IEnumerable<byte> ReadAllRemain()
        {
            lock (readLock)
            {
                while (readBuffer.Count > 0)
                    yield return readBuffer.Dequeue();

                if (tcpClient == null)
                    yield break;

                byte[] receivedBuffer = new byte[4096];
                int available = 0;

                try
                {
                    available = tcpClient.Client.Available;
                }
                catch { }

                while (available > 0)
                {
                    int received = 0;
                    try
                    {
                        received = tcpClient.Client.Receive(receivedBuffer);
                    }
                    catch { }
                    for (int i = 0; i < received; i++)
                        yield return receivedBuffer[i];

                try
                {
                    available = tcpClient.Client.Available;
                }
                catch { }
                }
            }
        }

        /// <summary>
        /// 수신 버퍼에 있는 데이터의 바이트 수입니다.
        /// </summary>
        public override uint BytesToRead
        {
            get
            {
                uint available = 0;

                try
                {
                    available = (uint)tcpClient.Client.Available;
                }
                catch { }
                return (uint)readBuffer.Count + available;
            }
        }
    }
}
