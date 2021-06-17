using System;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Serialization;

namespace SimpleModbusSlave
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new ConsoleChannelLogger();

            var channelProvider = new TcpServerChannelProvider(502)
            //var channelProvider = new UdpServerChannelProvider(502)
            {
                Logger = logger
            };

            var modbusSlaveService = new ModbusSlaveService(channelProvider)
            {
                //Serializer = new ModbusRtuSerializer(),
                Serializer = new ModbusTcpSerializer(),
                //Serializer = new ModbusAsciiSerializer(),
                Logger = logger,
                [1] = new ModbusSlave()
            };

            modbusSlaveService[1].InputRegisters.SetValue(102, 4.56f);
            modbusSlaveService[1].InputRegisters.SetValue(100, 1.23f);

            channelProvider.Start();

            Console.ReadKey();
        }
    }
}
