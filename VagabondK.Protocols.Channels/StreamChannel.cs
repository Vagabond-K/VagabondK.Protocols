using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VagabondK.Protocols.Channels
{
    /// <summary>
    /// 스트림 기반 채널
    /// </summary>
    public class StreamChannel : Channel
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stream">입/출력에 사용할 스트림</param>
        public StreamChannel(Stream stream)
        {
            inputStream = stream ?? throw new ArgumentNullException(nameof(stream));
            outputStream = stream;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="inputStream">입력(읽기)에 사용할 스트림</param>
        /// <param name="outputStream">출력(쓰기)에 사용할 스트림</param>
        public StreamChannel(Stream inputStream, Stream outputStream)
        {
            this.inputStream = inputStream ?? throw new ArgumentNullException(nameof(inputStream));
            this.outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
        }

        private readonly Stream inputStream;
        private readonly Stream outputStream;

        private readonly object writeLock = new object();
        private readonly object readLock = new object();
        private readonly Queue<byte> readBuffer = new Queue<byte>();
        private readonly EventWaitHandle readEventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        private bool isRunningReceive = false;

        /// <summary>
        /// 소멸자
        /// </summary>
        ~StreamChannel()
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
                inputStream?.Dispose();
                if (inputStream != outputStream)
                    outputStream?.Dispose();

                IsDisposed = true;
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
                                byte[] buffer = new byte[8192];
                                while (true)
                                {
                                    int received = inputStream.Read(buffer, 0, buffer.Length);
                                    lock (readBuffer)
                                    {
                                        for (int i = 0; i < received; i++)
                                            readBuffer.Enqueue(buffer[i]);
                                        readEventWaitHandle.Set();
                                    }
                                    if (received == 0)
                                        break;
                                }
                            }
                            catch
                            {
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
            }
        }

        /// <summary>
        /// 바이트 배열 쓰기
        /// </summary>
        /// <param name="bytes">바이트 배열</param>
        public override void Write(byte[] bytes)
        {
            lock (writeLock)
            {
                outputStream.Write(bytes, 0, bytes.Length);
            }
        }

        /// <summary>
        /// 채널 설명
        /// </summary>
        public override string Description { get => inputStream == outputStream ? inputStream.ToString() : $"{inputStream}/{outputStream}"; }
    }
}
