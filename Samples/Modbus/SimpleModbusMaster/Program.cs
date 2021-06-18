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

            IChannel channel = new TcpClientChannel("127.0.0.1", 502) { Logger = logger };   //TCP Client
            //IChannel channel = new TcpServerChannelProvider(502) { Logger = logger };        //TCP Server
            //IChannel channel = new UdpClientChannel("127.0.0.1", 502) { Logger = logger };   //UDP

            var modbusMaster = new ModbusMaster(channel)
            {
                //Serializer = new ModbusRtuSerializer(),
                Serializer = new ModbusTcpSerializer(),
                //Serializer = new ModbusAsciiSerializer(),
            };

            (channel as ChannelProvider)?.Start();

            while (true)
            {
                try
                {
                    var resposne = modbusMaster.Request(new ModbusReadRequest(1, ModbusObjectType.InputRegister, 100, 4));

                    var float100 = (resposne as ModbusReadRegisterResponse).GetSingle(100);
                    var float102 = (resposne as ModbusReadRegisterResponse).GetSingle(102);

                    Console.WriteLine($"Address 100 single float: {float100:F2}");
                    Console.WriteLine($"Address 102 single float: {float102:F2}");


                    resposne = modbusMaster.Request(new ModbusReadRequest(1, ModbusObjectType.InputRegister, 200, 2));  //SimpleModbusSlave 예제에 접속할 경우 Illegal Data Address 예외 발생
                    var float200 = (resposne as ModbusReadRegisterResponse).GetSingle(200);
                    Console.WriteLine(Math.Round(float200, 2));
                }
                catch (ModbusException ex)
                {
                    Console.WriteLine($"Catched exception: {ex.Message}");
                }
                Console.WriteLine();

                Thread.Sleep(1000);
            }
        }
    }
}
