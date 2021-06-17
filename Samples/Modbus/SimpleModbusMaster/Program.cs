using System;
using System.Threading;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Serialization;

namespace SimpleModbusMaster
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new ConsoleChannelLogger();

            var modbusMaster = new ModbusMaster
            {
                Channel = new TcpClientChannel("127.0.0.1", 502, 1000)
                //Channel = new UdpClientChannel("127.0.0.1", 502)
                {
                    Logger = logger
                },
                //Serializer = new ModbusRtuSerializer(),
                Serializer = new ModbusTcpSerializer(),
                //Serializer = new ModbusAsciiSerializer(),
                Logger = logger,
            };

            while (true)
            {
                Thread.Sleep(1000);

                try
                {
                    var request = new ModbusReadRequest(1, ModbusObjectType.InputRegister, 100, 4);
                    var resposne = modbusMaster.Request(request, 1000);

                    Console.WriteLine((resposne as ModbusReadRegisterResponse).GetSingle(100));
                    Console.WriteLine((resposne as ModbusReadRegisterResponse).GetSingle(102));
                }
                catch
                {
                }
            }
        }
    }
}
