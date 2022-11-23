using System;
using System.Threading;
using System.Threading.Tasks;
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

            IChannel channel = new TcpChannelProvider(502) { Logger = logger };        //TCP Server
            //IChannel channel = new TcpChannel("127.0.0.1", 502) { Logger = logger };   //TCP Client
            //IChannel channel = new UdpChannelProvider(502) { Logger = logger };        //UDP

            var modbusSlaveService = new ModbusSlaveService(channel)
            {
                //Serializer = new ModbusRtuSerializer(),
                Serializer = new ModbusTcpSerializer(),
                //Serializer = new ModbusAsciiSerializer(),
            };

            var modbusSlave1 = modbusSlaveService[1] = new ModbusSlave();

            var float100 = 1.23f;
            var float102 = 4.56f;
            int boolIndex = 0;
            (channel as ChannelProvider)?.Start();

            Task.Run(() =>
            {
                while (true)
                {
                    float100 += 0.01f;
                    float102 += 0.01f;

                    modbusSlave1.InputRegisters.SetValue(100, float100);
                    modbusSlave1.InputRegisters.SetValue(102, float102);

                    for (ushort i = 0; i < 10; i++)
                    {
                        modbusSlave1.DiscreteInputs[i] = i == boolIndex;
                        modbusSlave1.Coils[i] = i == boolIndex;
                    }
                    boolIndex = (boolIndex + 1) % 10;

                    Thread.Sleep(1000);
                }
            });

            Console.ReadKey();
            modbusSlaveService.Dispose();
        }
    }
}
