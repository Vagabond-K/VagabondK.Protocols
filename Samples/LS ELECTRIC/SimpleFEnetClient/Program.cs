using System;
using System.Linq;
using System.Threading;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.LSElectric;
using VagabondK.Protocols.LSElectric.FEnet;

namespace SimpleFEnetClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var logger = new ConsoleChannelLogger();

            IChannel channel = new TcpChannel("127.0.0.1", 2004) { Logger = logger };   //TCP Client
            //IChannel channel = new UdpChannel("127.0.0.1", 2005) { Logger = logger };   //UDP

            var client = new FEnetClient(channel);

            var readIndividual = new FEnetReadIndividualRequest(DataType.Word, "%MW100");
            var readContinuous = new FEnetReadContinuousRequest(DeviceType.M, 200, 2);
            var writeIndividual = new FEnetWriteIndividualRequest(DataType.Word) { ["%MW102"] = 30 };
            var writeContinuous = new FEnetWriteContinuousRequest(DeviceType.M, 200, new byte[] { 10, 20, 30 });

            while (true)
            {
                try
                {
                    var readIndividualResponse = client.Request(readIndividual);
                    var valuesIndividual = client.Read("%MW100", "%MW100", "%MW100", "%MW100", "%MW100", "%MW100", "%MW100", "%MW100", "%MW100", "%MW100", "%MW100", "%MW100", "%MW100", "%MW100", "%MW100", "%MW100", "%MW100");

                    var readContinuousResponse = client.Request(readContinuous);
                    var valuesContinuous = client.Read(DeviceType.M, 200, 2);

                    var writeIndividualResponse = client.Request(writeIndividual);
                    client.Write("%MW100", 30);

                    var writeContinuousResponse = client.Request(writeContinuous);
                    client.Write(DeviceType.M, 200, 10, 20);
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
