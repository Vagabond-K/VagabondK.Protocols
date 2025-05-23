using System;
using System.Collections.Generic;
#if NETSTANDARD2_0
using RJCP.IO.Ports;
#else
using System.IO.Ports;
#endif
using System.Threading;
using System.Threading.Tasks;
using VagabondK.Protocols.Logging;

namespace VagabondK.Protocols.Channels
{
    /// <summary>
    /// Serial 포트 통신 채널
    /// </summary>
    public class SerialPortChannel : Channel
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="portName">포트 이름</param>
        /// <param name="baudRate">Baud Rate</param>
        /// <param name="dataBits">Data Bits</param>
        /// <param name="stopBits">Stop Bits</param>
        /// <param name="parity">Parity</param>
        /// <param name="handshake">Handshake</param>
        public SerialPortChannel(string portName, int baudRate, int dataBits, StopBits stopBits, Parity parity, Handshake handshake)
        {
            description = portName;
            SerialPort =
#if NETSTANDARD2_0
                new SerialPortStream(portName, baudRate, dataBits, parity, stopBits);
#else
                new SerialPort(portName, baudRate, parity, dataBits, stopBits);
#endif
            SerialPort.Handshake = handshake;
        }

        private readonly object openLock = new object();
        private readonly object writeLock = new object();
        private readonly object readLock = new object();
        private readonly Queue<byte> readBuffer = new Queue<byte>();
        private readonly EventWaitHandle readEventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        private readonly string description;
        private bool isRunningReceive = false;

        /// <summary>
        /// Serial 포트
        /// </summary>
#if NETSTANDARD2_0
        public SerialPortStream SerialPort { get; }
#else
        public SerialPort SerialPort { get; }
#endif

        /// <summary>
        /// 포트 이름
        /// </summary>
        public string PortName { get => SerialPort.PortName; }

        /// <summary>
        /// Baud Rate
        /// </summary>
        public int BaudRate { get => SerialPort.BaudRate; }

        /// <summary>
        /// Data Bits
        /// </summary>
        public int DataBits { get => SerialPort.DataBits; }

        /// <summary>
        /// Stop Bits
        /// </summary>
        public StopBits StopBits { get => SerialPort.StopBits; }

        /// <summary>
        /// Parity
        /// </summary>
        public Parity Parity { get => SerialPort.Parity; }

        /// <summary>
        /// Handshake
        /// </summary>
        public Handshake Handshake { get => SerialPort.Handshake; }

        /// <summary>
        /// DTR 활성화 여부
        /// </summary>
        public bool DtrEnable { get => SerialPort.DtrEnable; set => SerialPort.DtrEnable = value; }

        /// <summary>
        /// RTS 활성화 여부
        /// </summary>
        public bool RtsEnable { get => SerialPort.RtsEnable; set => SerialPort.RtsEnable = value; }

        /// <summary>
        /// 채널 설명
        /// </summary>
        public override string Description { get => description; }

        /// <summary>
        /// 소멸자
        /// </summary>
        ~SerialPortChannel()
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

        /// <inheritdoc/>
        public override void Close()
        {
            lock (openLock)
            {
                if (SerialPort.IsOpen)
                {
                    Logger?.Log(new ChannelCloseEventLog(this));
                    SerialPort?.Close();
                }
            }
        }

        private void CheckPort(bool isWriting)
        {
            lock (openLock)
            {
                if (!IsDisposed)
                {
                    try
                    {
                        if (!SerialPort.IsOpen)
                        {
                            SerialPort.Open();
                            ReadAllRemain();
                            Logger?.Log(new ChannelOpenEventLog(this));
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!isWriting)
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

                    Task.Factory.StartNew(() =>
                    {
                        if (!isRunningReceive)
                        {
                            isRunningReceive = true;
                            try
                            {
                                CheckPort(false);
                                if (SerialPort.IsOpen)
                                {
                                    byte[] buffer = new byte[8192];
                                    while (true)
                                    {
                                        if (SerialPort.BytesToRead > 0)
                                        {
#if NETSTANDARD2_0
                                            int received = SerialPort.Read(buffer, 0, buffer.Length);
#else
                                            int received = SerialPort.Read(buffer, 0, buffer.Length);
#endif
                                            lock (readBuffer)
                                            {
                                                for (int i = 0; i < received; i++)
                                                    readBuffer.Enqueue(buffer[i]);
                                                readEventWaitHandle.Set();
                                            }
                                        }
#if NETSTANDARD2_0
                                        else Thread.Sleep(1);
#endif
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
                    }, TaskCreationOptions.LongRunning);
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
            CheckPort(true);
            lock (writeLock)
            {
                try
                {
                    if (SerialPort.IsOpen)
                    {
                        SerialPort.Write(bytes, 0, bytes.Length);
#if NETSTANDARD2_0
                        SerialPort.Flush();
#endif
                    }
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

                if (!SerialPort.IsOpen)
                    yield break;

                try
                {
                    SerialPort.DiscardInBuffer();
                }
                catch { }
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
                    available = (uint)SerialPort.BytesToRead;
                }
                catch { }
                return (uint)readBuffer.Count + available;
            }
        }

    }
}
