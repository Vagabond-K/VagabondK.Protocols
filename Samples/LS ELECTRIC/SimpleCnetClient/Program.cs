using System;
using System.Linq;
using System.Threading;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.LSElectric;
using VagabondK.Protocols.LSElectric.Cnet;

namespace SimpleCnetClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //var read = new CnetReadIndividualRequest(0x20) { "%MW100" };
            //var readBlock = new CnetReadContinuousRequest(0x10, "%MW100", 5);
            //var write = new CnetWriteIndividualRequest(0x20) { ["%MW100"] = 0x00e2 };
            //var writeBlock = new CnetWriteContinuousRequest(0x10, "%MW100") { 0x1111, 0x2222 };
            //var monitor = new CnetMonitorByIndividual(0x20, 0x09, new DeviceAddress[] { "%MW100" }).CreateRegisterRequest();
            //var monitorExe = new CnetMonitorByIndividual(0x20, 0x09, new DeviceAddress[] { "%MW100" }).CreateExecuteRequest();

            //System.IO.MemoryStream inputStream = new System.IO.MemoryStream();
            //System.IO.MemoryStream outputStream = new System.IO.MemoryStream();
            //inputStream.Write(new CnetReadResponse(new[] { new DeviceValue(5) }, read).Serialize().ToArray());
            //inputStream.Write(new CnetReadResponse(Enumerable.Range(1, 5).SelectMany(i => new byte[] { 0, (byte)i }), readBlock).Serialize().ToArray());
            //inputStream.Write(new CnetACKResponse(write).Serialize().ToArray());
            //inputStream.Write(new CnetACKResponse(writeBlock).Serialize().ToArray());
            //inputStream.Write(new CnetACKResponse(monitor).Serialize().ToArray());
            //inputStream.Write(new CnetReadResponse(new[] { new DeviceValue(6) }, monitorExe).Serialize().ToArray());
            //inputStream.Position = 0;

            //var logger = new ConsoleChannelLogger();
            //var client = new CnetClient(new StreamChannel(inputStream, outputStream) { Logger = logger });
            //var readResponse = client.Request(read);
            //var readBlockResponse = client.Request(readBlock);
            //var writeResponse = client.Request(write);
            //var writeBlockResponse = client.Request(writeBlock);
            //var monitorResponse = client.Request(monitor);
            //var monitorExeResponse = client.Request(monitorExe);

            //var readLog = BitConverter.ToString(read.Serialize().ToArray());
            //var readBlockLog = BitConverter.ToString(readBlock.Serialize().ToArray());
            //var writeLog = BitConverter.ToString(write.Serialize().ToArray());
            //var writeBlockLog = BitConverter.ToString(writeBlock.Serialize().ToArray());
            //var monitorLog = BitConverter.ToString(monitor.Serialize().ToArray());
            //var monitorExeLog = BitConverter.ToString(monitorExe.Serialize().ToArray());

            //var commands = $"{readLog}\r\n\r\n{readBlockLog}\r\n\r\n{writeLog}\r\n\r\n{writeBlockLog}\r\n\r\n{monitorLog}\r\n\r\n{monitorExeLog}";

            //Console.ReadKey();

            var logger = new ConsoleChannelLogger();

            IChannel channel = new TcpChannel("127.0.0.1", 1234) { Logger = logger };   //TCP Client
            //IChannel channel = new TcpChannelProvider(1234) { Logger = logger };        //TCP Server
            //IChannel channel = new UdpChannel("127.0.0.1", 1234) { Logger = logger };   //UDP

            var client = new CnetClient(channel);

            (channel as ChannelProvider)?.Start();


            var read = new CnetReadIndividualRequest(1) { "%MW100" };
            var readBlock = new CnetReadContinuousRequest(1, "%MW100", 5);
            var write = new CnetWriteIndividualRequest(1) { ["%MW102"] = 30 };
            var writeBlock = new CnetWriteContinuousRequest(1, "%MW100") { 10, 20 };
            var monitor = new CnetMonitorByIndividualAccess(1, 1, new DeviceVariable[] { "%MW100" }).CreateRegisterRequest();
            var monitorExe = new CnetMonitorByIndividualAccess(1, 1, new DeviceVariable[] { "%MW100" }).CreateExecuteRequest();
            var monitorBlock = new CnetMonitorByContinuousAccess(1, 2, "%MW100", 3).CreateRegisterRequest();
            var monitorBlockExe = new CnetMonitorByContinuousAccess(1, 2, "%MW100", 3).CreateExecuteRequest();

            while (true)
            {
                try
                {
                    var readResponse = client.Request(read);
                    var readBlockResponse = client.Request(readBlock);
                    var writeResponse = client.Request(write);
                    var writeBlockResponse = client.Request(writeBlock);
                    var monitorResponse = client.Request(monitor);
                    var monitorExeResponse = client.Request(monitorExe);
                    var monitorBlockResponse = client.Request(monitorBlock);
                    var monitorBlockExeResponse = client.Request(monitorBlockExe);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Catched exception: {ex.Message}");
                }
                Console.WriteLine();

                Thread.Sleep(1000);
            }
        }
    }
}
