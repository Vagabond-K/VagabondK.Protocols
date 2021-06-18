using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VagabondK.Protocols.Logging;

namespace VagabondK.Protocols.Channels
{
    /// <summary>
    /// UCP 소켓 기반 통신 채널
    /// </summary>
    public class UdpClientChannel : Channel
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="host">호스트</param>
        /// <param name="remotePort">원격 포트</param>
        public UdpClientChannel(string host, int remotePort)
        {
            Host = host;
            RemotePort = remotePort;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="host">호스트</param>
        /// <param name="remotePort">원격 포트</param>
        /// <param name="localPort">로컬 포트</param>
        public UdpClientChannel(string host, int remotePort, int localPort)
        {
            Host = host;
            RemotePort = remotePort;
            LocalPort = localPort;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="localPort">로컬 포트</param>
        public UdpClientChannel(int localPort)
        {
            LocalPort = localPort;
        }

        /// <summary>
        /// 호스트
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// 원격 포트
        /// </summary>
        public int RemotePort { get; }

        /// <summary>
        /// 로컬 포트
        /// </summary>
        public int? LocalPort { get; }

        private UdpClient udpClient = null;
        private readonly object connectLock = new object();
        private readonly object writeLock = new object();
        private readonly object readLock = new object();
        private readonly Queue<byte> readBuffer = new Queue<byte>();
        private readonly EventWaitHandle readEventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        private bool isRunningReceive = false;
        private string description;

        private IPEndPoint remoteEndPoint;

        /// <summary>
        /// 채널 설명
        /// </summary>
        public override string Description { get => description; }

        /// <summary>
        /// 소멸자
        /// </summary>
        ~UdpClientChannel()
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
                IsDisposed = true;

                Close();
            }
        }

        private void Close()
        {
            lock (connectLock)
            {
                if (udpClient != null)
                {
                    Logger?.Log(new ChannelCloseEventLog(this));
                    udpClient?.Close();
                    udpClient = null;
                }
            }
        }

        private void CheckConnection()
        {
            lock (connectLock)
            {
                if (!IsDisposed && udpClient == null)
                {
                    if (LocalPort != null)
                        udpClient = new UdpClient(LocalPort.Value);
                    else
                        udpClient = new UdpClient();

                    if (RemotePort != 0)
                    {
                        udpClient.Connect(Host ?? string.Empty, RemotePort);
                        description = udpClient.Client.RemoteEndPoint.ToString();
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
                                if (udpClient != null)
                                {
                                    if (RemotePort == 0)
                                    {
                                        var buffer = udpClient.Receive(ref remoteEndPoint);
                                        lock (readBuffer)
                                        {
                                            for (int i = 0; i < buffer.Length; i++)
                                                readBuffer.Enqueue(buffer[i]);
                                            readEventWaitHandle.Set();
                                        }
                                        description = remoteEndPoint.ToString();
                                    }
                                    else
                                    {
                                        byte[] buffer = new byte[8192];
                                        while (true)
                                        {
                                            int received = udpClient.Client.Receive(buffer);
                                            lock (readBuffer)
                                            {
                                                for (int i = 0; i < received; i++)
                                                    readBuffer.Enqueue(buffer[i]);
                                                readEventWaitHandle.Set();
                                            }
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
                    });
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
                    if (remoteEndPoint != null)
                    {
                        udpClient.Send(bytes, bytes.Length, remoteEndPoint);
                    }
                    else if (udpClient?.Client?.Connected == true)
                        udpClient?.Client?.Send(bytes);
                }
                catch
                {
                    Close();
                    throw new TimeoutException();
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

                if (udpClient == null)
                    yield break;

                byte[] receivedBuffer = new byte[4096];
                int available = 0;

                try
                {
                    available = udpClient.Client.Available;
                }
                catch { }

                while (available > 0)
                {
                    int received = 0;
                    try
                    {
                        received = udpClient.Client.Receive(receivedBuffer);
                    }
                    catch { }
                    for (int i = 0; i < received; i++)
                        yield return receivedBuffer[i];

                try
                {
                    available = udpClient.Client.Available;
                }
                catch { }
                }
            }
        }
    }
}
