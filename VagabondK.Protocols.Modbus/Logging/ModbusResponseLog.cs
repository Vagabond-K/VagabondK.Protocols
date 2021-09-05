using System;
using System.Collections.Generic;
using System.Text;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Serialization;

namespace VagabondK.Protocols.Logging
{
    /// <summary>
    /// 통신 채널을 통해 주고 받은 Modbus 응답 메시지에 대한 Log
    /// </summary>
    public class ModbusResponseLog : ChannelResponseLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="response">응답 메시지 인스턴스</param>
        /// <param name="rawMessage">원본 메시지</param>
        /// <param name="requestLog">관련 요청 메시지에 대한 Log</param>
        /// <param name="serializer">Modbus Serializer</param>
        public ModbusResponseLog(IChannel channel, ModbusResponse response, byte[] rawMessage, ModbusRequestLog requestLog, ModbusSerializer serializer) : base(channel, response, rawMessage, requestLog)
        {
            this.serializer = serializer;
            ModbusResponse = response;
        }

        private readonly ModbusSerializer serializer;

        /// <summary>
        /// Modbus 응답 메시지
        /// </summary>
        public ModbusResponse ModbusResponse { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString() => $"Response: {RawMessage.ModbusRawMessageToString(serializer)}";
    }
}
