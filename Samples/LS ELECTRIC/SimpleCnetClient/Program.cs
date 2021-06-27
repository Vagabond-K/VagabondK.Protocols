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
