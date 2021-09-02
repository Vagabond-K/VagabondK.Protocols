# Vagabond K Protocol Library [![License](https://img.shields.io/badge/license-LGPL--2.1-blue.svg)](https://licenses.nuget.org/LGPL-2.1-only)   
Modbus RTU/ASCII/TCP 프로토콜, LS ELECTRIC(구 LS산전)의 Cnet 프로토콜 등으로 장치와 통신하는 기능들을 구현했습니다.

### Documentation
https://vagabond-k.github.io/Documentation

## VagabondK.Protocols.Abstractions [![NuGet](https://img.shields.io/nuget/v/VagabondK.Protocols.Abstractions.svg)](https://www.nuget.org/packages/VagabondK.Protocols.Abstractions/)   
여러 프로토콜에 대한 추상 형식들과, 기본적인 로깅 관련 기능들을 제공합니다.

## VagabondK.Protocols.Channels [![NuGet](https://img.shields.io/nuget/v/VagabondK.Protocols.Channels.svg)](https://www.nuget.org/packages/VagabondK.Protocols.Channels/)   
여러 프로토콜들에 사용되는 통신 채널 관련 기능들을 제공합니다. 기본적인 TCP 및 UDP 소켓 기반 채널을 구현합니다.

## VagabondK.Protocols.Channels.SerialPort [![NuGet](https://img.shields.io/nuget/v/VagabondK.Protocols.Channels.SerialPort.svg)](https://www.nuget.org/packages/VagabondK.Protocols.Channels.SerialPort/)   
시리얼 포트 기반 통신 채널 관련 기능들을 제공합니다.   

참고로 UWP 앱에서 이 패키지를 사용할 경우에는 SerialDevice 클래스를 기반으로 구현된 기능을 사용할 수 있습니다.   
반드시 SerialPortChannel.StartSerialPortWatcher() 메서드를 호출한 후에 사용해야 합니다.

## VagabondK.Protocols.Modbus [![NuGet](https://img.shields.io/nuget/v/VagabondK.Protocols.Modbus.svg)](https://www.nuget.org/packages/VagabondK.Protocols.Modbus/)   
Modbus RTU, TCP, ASCII 등의 직렬화 기능을 제공하고, Modbus Master와 Modbus Slave를 구현할 수 있습니다.   

또한 Modbus Master와 Modbus Slave 모두 VagabondK.Protocols.Channels 패키지의 TCP Server/Clinet 채널을 사용할 수 있으며, UDP 소켓 채널도 사용 가능합니다.
그리고 VagabondK.Protocols.Channels.SerialPort 패키지를 이용하면, 시리얼 포트 기반으로도 통신이 가능해집니다.  
[Vagabond K의 .NET Modbus 라이브러리 사용법 [Part #1]](https://blog.naver.com/vagabond-k/222490531747)  
[Vagabond K의 .NET Modbus 라이브러리 사용법 [Part #2]](https://blog.naver.com/vagabond-k/222493009718)  

## VagabondK.Protocols.LSElectric [![NuGet](https://img.shields.io/nuget/v/VagabondK.Protocols.LSElectric.svg)](https://www.nuget.org/packages/VagabondK.Protocols.LSElectric/)   
LS ELECTRIC(구 LS산전)의 PLC 제품들과 Cnet 프로토콜 기반으로 통신하는 기능들을 제공합니다.   
Cnet I/F 모듈, MASTER-K PLC 등과 통신할 수 있습니다.   
통신 채널은 주로 시리얼 포트 채널을 사용하지만, Serial to Ethernet 컨버터 등의 장치를 이용해 통신할 경우를 대비하여 TCP 및 UDP 기반 채널로도 통신할 수 있습니다.
