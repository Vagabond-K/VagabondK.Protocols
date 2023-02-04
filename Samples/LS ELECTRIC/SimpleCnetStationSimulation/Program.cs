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

            IChannel channel = new SerialPortChannel("COM4", 9600, 8, System.IO.Ports.StopBits.One, System.IO.Ports.Parity.None, System.IO.Ports.Handshake.None) { Logger = logger };     //Serial Port
            //IChannel channel = new TcpChannelProvider(1234) { Logger = logger };        //TCP Server
            //IChannel channel = new TcpChannel("127.0.0.1", 1234) { Logger = logger };   //TCP Client
            //IChannel channel = new UdpChannelProvider(1234) { Logger = logger };        //UDP
            BitConverter.GetBytes(123.456).CopyTo(deviceMemories[DeviceType.M], 0);

            var cnetSimulationService = new CnetSimulationService(channel);

            var simulationStation1 = cnetSimulationService[1] = new CnetSimulationStation();
            simulationStation1.RequestedRead += SimulationStation1_RequestedRead;
            simulationStation1.RequestedWrite += SimulationStation1_RequestedWrite;
            (channel as ChannelProvider)?.Start();

            Console.ReadKey();
        }

        private static Dictionary<DeviceType, byte[]> deviceMemories = new Dictionary<DeviceType, byte[]>
        {
            [DeviceType.P] = new byte[10000],
            [DeviceType.M] = new byte[10000],
            [DeviceType.L] = new byte[10000],
            [DeviceType.K] = new byte[10000],
            [DeviceType.F] = new byte[10000],
            [DeviceType.T] = new byte[10000],
            [DeviceType.C] = new byte[10000],
            [DeviceType.D] = new byte[10000],
            [DeviceType.S] = new byte[10000],
        };

        private static void SimulationStation1_RequestedRead(object sender, CnetRequestedReadEventArgs e)
        {
            foreach (var item in e.ResponseValues)
                if (deviceMemories.TryGetValue(item.DeviceVariable.DeviceType, out var deviceMemory))
                    item.DeviceValue = item.DeviceVariable.DataType switch
                    {
                        DataType.Bit => (deviceMemory[item.DeviceVariable.Index / 8] >> ((int)item.DeviceVariable.Index % 8)) & 1,
                        DataType.Byte => deviceMemory[item.DeviceVariable.Index],
                        DataType.Word => BitConverter.ToUInt16(deviceMemory, (int)item.DeviceVariable.Index * 2),
                        DataType.DoubleWord => BitConverter.ToUInt32(deviceMemory, (int)item.DeviceVariable.Index * 4),
                        DataType.LongWord => BitConverter.ToUInt64(deviceMemory, (int)item.DeviceVariable.Index * 8),
                        _ => 0
                    };
        }

        private static void SimulationStation1_RequestedWrite(object sender, CnetRequestedWriteEventArgs e)
        {
            foreach (var item in e.Values)
            {
                if (deviceMemories.TryGetValue(item.Key.DeviceType, out var deviceMemory))
                {
                    if (item.Key.DataType == DataType.Bit)
                    {
                        var byteIndex = item.Key.Index / 8;
                        deviceMemory[byteIndex] = (byte)(item.Value.BitValue
                            ? deviceMemory[byteIndex] | (1 << (int)(item.Key.Index % 8))
                            : deviceMemory[byteIndex] & ~(1 << (int)(item.Key.Index % 8)));
                    }
                    else
                    {
                        var bytes = item.Value.GetBytes(item.Key.DataType);
                        bytes.CopyTo(deviceMemory, item.Key.Index * bytes.Length);
                    }
                }
            }
        }
    }
}
