using System;
using System.Collections.Generic;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.LSElectric;
using VagabondK.Protocols.LSElectric.Cnet.Simulation;

namespace SimpleCnetStationSimulation
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new ConsoleChannelLogger();

            IChannel channel = new TcpChannelProvider(1234) { Logger = logger };        //TCP Server
            //IChannel channel = new TcpChannel("127.0.0.1", 1234) { Logger = logger };   //TCP Client
            //IChannel channel = new UdpChannelProvider(1234) { Logger = logger };        //UDP

            var modbusSlaveService = new CnetSimulationService(channel);

            var simulationStation1 = modbusSlaveService[1] = new CnetSimulationStation();
            simulationStation1.RequestedRead += SimulationStation1_RequestedRead;
            simulationStation1.RequestedWrite += SimulationStation1_RequestedWrite;
            (channel as ChannelProvider)?.Start();

            Console.ReadKey();
        }

        private static Dictionary<DeviceVariable, DeviceValue> deviceValues = new Dictionary<DeviceVariable, DeviceValue>();

        private static void SimulationStation1_RequestedRead(object sender, RequestedReadEventArgs e)
        {
            foreach (var item in e.ResponseValues)
            {
                if (deviceValues.TryGetValue(item.DeviceVariable, out var deviceValue))
                    item.DeviceValue = deviceValue;
            }
        }

        private static void SimulationStation1_RequestedWrite(object sender, RequestedWriteEventArgs e)
        {
            foreach (var item in e.Values)
            {
                deviceValues[item.Key] = item.Value;
            }
        }
    }
}
