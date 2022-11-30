using System;
using System.IO;
using System.Text;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// 스트림 기반 통신 채널 Logger
    /// </summary>
    public class StreamChannelLogger : IChannelLogger, IDisposable
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stream">스트림</param>
        /// <param name="encoding">인코딩</param>
        public StreamChannelLogger(Stream stream, Encoding encoding = null)
        {
            InnerStream = stream ?? throw new ArgumentNullException(nameof(stream));
            streamWriter = new StreamWriter(InnerStream, encoding ?? Encoding.UTF8);
        }

        private readonly StreamWriter streamWriter;

        /// <summary>
        /// 내부 스트림
        /// </summary>
        public Stream InnerStream { get; }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            streamWriter.Dispose();
        }

        /// <summary>
        /// 통신 채널 Log 기록
        /// </summary>
        /// <param name="log">통신 채널 Log</param>
        public void Log(ChannelLog log)
        {
            WriteToStream(streamWriter, log);
        }

        /// <summary>
        /// 스트림에 통신 채널 Log 쓰기
        /// </summary>
        /// <param name="writer">StreamWriter</param>
        /// <param name="log">통신 채널 Log</param>
        protected virtual void WriteToStream(StreamWriter writer, ChannelLog log)
        {
            lock (InnerStream)
            {
                writer.Write($"({log.ChannelDescription}) {log}");
                writer.Flush();
            }
        }
    }
}
