using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VagabondK.Protocols.Channels
{
    public class StreamChannel : Channel
    {
        public StreamChannel(Stream stream)
        {
            inputStream = stream ?? throw new ArgumentNullException(nameof(stream));
            outputStream = stream;
        }

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

        public override void Write(byte[] bytes)
        {
            lock (writeLock)
            {
                outputStream.Write(bytes, 0, bytes.Length);
            }
        }

        public override string Description { get => inputStream == outputStream ? inputStream.ToString() : $"{inputStream}/{outputStream}"; }
    }
}
