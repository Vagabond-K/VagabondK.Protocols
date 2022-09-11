using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.LSElectric;
using VagabondK.Protocols.LSElectric.FEnet;
using VagabondK.Protocols.LSElectric.FEnet.Simulation;

namespace SimpleFEnetServerSimulation
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var logger = new ConsoleChannelLogger();

            IChannel channel = new TcpChannelProvider(2004) { Logger = logger };        //TCP Server
            //IChannel channel = new UdpChannelProvider(2005) { Logger = logger };        //UDP

            var fenetSimulationService = new FEnetSimulationService(channel);

            fenetSimulationService.RequestedReadIndividual += FenetSimulationService_RequestedReadIndividual;
            fenetSimulationService.RequestedReadContinuous += FenetSimulationService_RequestedReadContinuous;

            fenetSimulationService.RequestedWriteIndividual += FenetSimulationService_RequestedWriteIndividual;
            fenetSimulationService.RequestedWriteContinuous += FenetSimulationService_RequestedWriteContinuous;

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

        private static void FenetSimulationService_RequestedReadIndividual(object sender, FEnetRequestedReadIndividualEventArgs e)
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

        private static void FenetSimulationService_RequestedReadContinuous(object sender, FEnetRequestedReadContinuousEventArgs e)
        {
            if (deviceMemories.TryGetValue(e.StartDeviceVariable.DeviceType, out var deviceMemory))
                e.ResponseValues = deviceMemory.Skip((int)e.StartDeviceVariable.Index).Take(e.Count);
        }

        private static void FenetSimulationService_RequestedWriteIndividual(object sender, FEnetRequestedWriteIndividualEventArgs e)
        {
            foreach (var item in e.Values)
            {
                if (deviceMemories.TryGetValue(item.Key.DeviceType, out var deviceMemory))
                {
                    if (item.Key.DataType == DataType.Bit)
                    {
                        var byteIndex = item.Key.Index / 8;
                        deviceMemory[byteIndex] = (byte)(item.Value.BitValue
                            ? deviceMemory[byteIndex] | (1 >> (int)(item.Key.Index % 8))
                            : deviceMemory[byteIndex] & ~(1 >> (int)(item.Key.Index % 8)));
                    }
                    else
                    {
                        var bytes = item.Value.GetBytes(item.Key.DataType);
                        bytes.CopyTo(deviceMemory, item.Key.Index * bytes.Length);
                    }
                }
            }
        }

        private static void FenetSimulationService_RequestedWriteContinuous(object sender, FEnetRequestedWriteContinuousEventArgs e)
        {
            if (deviceMemories.TryGetValue(e.StartDeviceVariable.DeviceType, out var deviceMemory))
                e.Values.ToArray().CopyTo(deviceMemory, e.StartDeviceVariable.Index);
        }
    }
}
