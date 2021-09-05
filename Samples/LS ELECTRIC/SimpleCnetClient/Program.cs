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

            //IChannel channel = new SerialPortChannel("COM1", 9600, 8, System.IO.Ports.StopBits.One, System.IO.Ports.Parity.None);     //Serial Port
            IChannel channel = new TcpChannel("127.0.0.1", 1234) { Logger = logger };   //TCP Client
            //IChannel channel = new TcpChannelProvider(1234) { Logger = logger };        //TCP Server
            //IChannel channel = new UdpChannel("127.0.0.1", 1234) { Logger = logger };   //UDP

            var client = new CnetClient(channel);

            (channel as ChannelProvider)?.Start();


            var readIndividual = new CnetReadIndividualRequest(1) { "%MW100" };
            var readContinuous = new CnetReadContinuousRequest(1, "%MW100", 5);
            var writeIndividual = new CnetWriteIndividualRequest(1) { ["%MW102"] = 30 };
            var writeContinuous = new CnetWriteContinuousRequest(1, "%MW100") { 10, 20 };

            var monitorIndividual = new CnetMonitorByIndividualAccess(1, 1, "%MW100");
            var monitorRegisterIndividual = monitorIndividual.CreateRegisterRequest();
            var monitorExecuteIndividual = monitorIndividual.CreateExecuteRequest();

            var monitorContinuous = new CnetMonitorByContinuousAccess(1, 2, "%MW100", 3);
            var monitorRegisterContinuous = monitorContinuous.CreateRegisterRequest();
            var monitorExecuteContinuous = monitorContinuous.CreateExecuteRequest();

            while (true)
            {
                try
                {
                    var readIndividualResponse = client.Request(readIndividual);
                    var valuesIndividual = client.Read(1, "%MW100");

                    var readContinuousResponse = client.Request(readContinuous);
                    var valuesContinuous = client.Read(1, "%MW100", 5);

                    var writeIndividualResponse = client.Request(writeIndividual);
                    client.Write(1, ("%MW102", 30));

                    var writeContinuousResponse = client.Request(writeContinuous);
                    client.Write(1, "%MW100", 10, 20);


                    var monitorRegisterIndividualResponse = client.Request(monitorRegisterIndividual);
                    var monitorExecuteIndividual2 = client.RegisterMonitor(1, 1, "%MW100");

                    var monitorExeResponse = client.Request(monitorExecuteIndividual);
                    var valuesMonitorIndividual = client.Read(monitorExecuteIndividual2);


                    var monitorContinuousResponse = client.Request(monitorRegisterContinuous);
                    var monitorExecuteContinuous2 = client.RegisterMonitor(1, 2, "%MW100", 3);

                    var monitorContinuousExeResponse = client.Request(monitorExecuteContinuous);
                    var valuesMonitorContinuous = client.Read(monitorExecuteContinuous2);
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
