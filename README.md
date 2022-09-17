# Vagabond K Protocol Library [![License](https://img.shields.io/badge/license-LGPL--2.1-blue.svg)](https://licenses.nuget.org/LGPL-2.1-only)  
Modbus RTU/ASCII/TCP 프로토콜, LS ELECTRIC(구 LS산전)의 Cnet, FEnet 프로토콜 등으로 장치와 통신하는 기능들을 구현했습니다.

- [Documentation](https://vagabond-k.github.io/Documentation)

[!["Buy me a soju"](https://vagabond-k.github.io/Images/buymeasoju131x36.png)](https://www.buymeacoffee.com/VagabondK)  

# VagabondK.Protocols.Abstractions [![NuGet](https://img.shields.io/nuget/v/VagabondK.Protocols.Abstractions.svg)](https://www.nuget.org/packages/VagabondK.Protocols.Abstractions/)   
여러 프로토콜에 대한 추상 형식들과, 기본적인 Logging 관련 기능들을 제공합니다.

#### Console 기반 Logger 설정 예시
```csharp
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.Channels;

...

TcpChannel channel = new TcpChannel("127.0.0.1", 502)
{
    Logger = new ConsoleChannelLogger()
};
```

# VagabondK.Protocols.Channels [![NuGet](https://img.shields.io/nuget/v/VagabondK.Protocols.Channels.svg)](https://www.nuget.org/packages/VagabondK.Protocols.Channels/)   
여러 프로토콜들에 사용되는 Communication Channel 관련 기능들을 제공합니다.  
기본적인 TCP 및 UDP Socket 기반 Channel을 구현합니다.

#### TCP Client Channel 생성 예시
```csharp
var tcpChannel = new TcpChannel("127.0.0.1", 502);
```
#### TCP Server Channel 생성 예시
```csharp
var tcpChannelProvider = new TcpChannelProvider(502);
```
#### UDP Channel 생성 예시
```csharp
var udpChannel = new UdpChannel("127.0.0.1", 502);
```
#### Local port가 포함된 UDP 수신 Channel 생성 예시
```csharp
var udpChannelProvider = new UdpChannelProvider(502);
```


# VagabondK.Protocols.Channels.SerialPort [![NuGet](https://img.shields.io/nuget/v/VagabondK.Protocols.Channels.SerialPort.svg)](https://www.nuget.org/packages/VagabondK.Protocols.Channels.SerialPort/)   
Serial Port 기반 Communication Channel 관련 기능들을 제공합니다.   

#### Serial Port Channel 생성 예시
```csharp
var serialPortChannel = new SerialPortChannel("COM3", 9600, 8, StopBits.One, Parity.None, Handshake.None);
```


# VagabondK.Protocols.Modbus [![NuGet](https://img.shields.io/nuget/v/VagabondK.Protocols.Modbus.svg)](https://www.nuget.org/packages/VagabondK.Protocols.Modbus/)   
- [Modbus 라이브러리 사용법 [Part #1]](https://blog.naver.com/vagabond-k/222490531747)  
- [Modbus 라이브러리 사용법 [Part #2]](https://blog.naver.com/vagabond-k/222493009718)  

Modbus RTU, TCP, ASCII 등의 직렬화 기능을 제공하고, Modbus Master와 Modbus Slave를 구현할 수 있습니다.   

또한 Modbus Master와 Modbus Slave 모두 VagabondK.Protocols.Channels 패키지의 TCP Server/Clinet Channel을 사용할 수 있으며, UDP Socket Channel도 사용 가능합니다.
그리고 VagabondK.Protocols.Channels.SerialPort 패키지를 이용하면, Serial Port 기반으로도 통신이 가능해집니다.  

#### Modbus Master 읽기 요청 예시
```csharp
using System;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Data;
using VagabondK.Protocols.Modbus.Serialization;

class Program
{
    static void Main(string[] args)
    {
        TcpChannel channel = new TcpChannel("127.0.0.1", 502);

        ModbusMaster modbusMaster = new ModbusMaster(channel, new ModbusTcpSerializer());

        var response = modbusMaster.ReadInputRegisters(1, 100, 8);

        int int_ABCD = response.GetInt32(100);
        int int_DCBA = response.GetInt32(102, ModbusEndian.AllLittle);
        int int_BADC = response.GetInt32(104, new ModbusEndian(true, false));
        int int_CDAB = response.GetInt32(106, new ModbusEndian(false, true));

        Console.WriteLine(int_ABCD);
        Console.WriteLine(int_DCBA);
        Console.WriteLine(int_BADC);
        Console.WriteLine(int_CDAB);
    }
}
```

#### Modbus Master 쓰기 요청 예시
```csharp
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Serialization;

class Program
{
    static void Main(string[] args)
    {
        TcpChannel channel = new TcpChannel("127.0.0.1", 502);

        ModbusMaster modbusMaster = new ModbusMaster(channel, new ModbusTcpSerializer());

        modbusMaster.Write(1, 100, (ushort)12345);
    }
}
```

#### Modbus Slave 구현 예시
```csharp
using System;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Serialization;

class Program
{
    static void Main(string[] args)
    {
        TcpChannelProvider channel = new TcpChannelProvider(502)
        {
            Logger = new ConsoleChannelLogger() //Logger에 콘솔 기반 Logger를 설정
        };

        var modbusSlaveService = new ModbusSlaveService(channel, new ModbusTcpSerializer());

        var modbusSlave = new ModbusSlave();
        modbusSlaveService[1] = modbusSlave;
        modbusSlave.InputRegisters[100] = 1234;  //Input Register 100에 1234 설정

        channel.Start();
        Console.ReadKey();
        channel.Stop();
    }
}
```
# VagabondK.Protocols.LSElectric [![NuGet](https://img.shields.io/nuget/v/VagabondK.Protocols.LSElectric.svg)](https://www.nuget.org/packages/VagabondK.Protocols.LSElectric/)   
- [LS ELECTRIC(구 LS산전) Cnet 프로토콜 라이브러리 사용법](https://blog.naver.com/vagabond-k/222498651714)
- [LS ELECTRIC(구 LS산전) FEnet 프로토콜 라이브러리 사용법](https://blog.naver.com/vagabond-k/222877084987)

LS ELECTRIC(구 LS산전)의 PLC 제품들과 Cnet, FEnet 프로토콜 기반으로 통신하는 기능들을 제공합니다.   
Cnet I/F 모듈, FEnet I/F 모듈, MASTER-K PLC 등과 통신할 수 있습니다.   

Cnet 프로토콜의 경우 통신 채널은 주로 Serial Port Channel을 사용하지만, Serial Device Server(Serial to Ethernet Converter) 등의 장치를 이용해 통신할 경우에는 TCP 및 UDP 기반 Channel로도 통신할 수 있습니다.   

FEnet 프로토콜은 TCP 및 UDP 기반 Channel로 통신 가능하며 기본적인 포트로 TCP는 2004, UDP는 2005를 사용합니다.

#### Cnet 프로토콜 기반 Read, Write, Monitor 사용 예시
```csharp
using System;
using System.IO.Ports;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.LSElectric.Cnet;

class Program
{
    static void Main(string[] args)
    {
        CnetClient client = new CnetClient(new SerialPortChannel("COM4", 9600, 8, StopBits.One, Parity.None, Handshake.None));

        foreach (var item in client.Read(1, "%MW100", "%MW200"))
            Console.WriteLine($"변수: {item.Key}, 값: {item.Value.WordValue}");
            
        foreach (var item in client.Read(1, "%MW100", 5))
            Console.WriteLine($"변수: {item.Key}, 값: {item.Value.WordValue}");
            
        client.Write(1, ("%MW102", 10), ("%MW202", 20));
        
        client.Write(1, "%MW300", 10, 20);
        
        var monitorExecute1 = client.RegisterMonitor(1, 1, "%MW100", "%MW200");
        foreach (var item in client.Read(monitorExecute1))
            Console.WriteLine($"변수: {item.Key}, 값: {item.Value.WordValue}");
            
        var monitorExecute2 = client.RegisterMonitor(1, 2, "%MW100", 5);
        foreach (var item in client.Read(monitorExecute2))
            Console.WriteLine($"변수: {item.Key}, 값: {item.Value.WordValue}");
    }
}
```

#### FEnet 프로토콜 기반 Read, Write 실행 예시
```csharp
using System;
using System.Linq;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.LSElectric;
using VagabondK.Protocols.LSElectric.FEnet;

class Program
{
    static void Main(string[] args)
    {
        FEnetClient client = new FEnetClient(new TcpChannel("127.0.0.1", 2004));

        foreach (var item in client.Read("%MW100", "%MW200"))
            Console.WriteLine($"변수: {item.Key}, 값: {item.Value.WordValue}");

        var bytes = client.Read(DeviceType.M, 200, 10).ToArray();
        for (int i = 0; i < bytes.Length; i+=2)
            Console.WriteLine($"변수: %MW{100 + i / 2}, 값: {BitConverter.ToInt16(bytes, i)}");

        client.Write(("%MW102", 10), ("%MW202", 20));

        client.Write(DeviceType.M, 600, 10, 0, 20, 0);
    }
}
```
