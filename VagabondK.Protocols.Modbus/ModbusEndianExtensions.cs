using System;
using System.Collections.Generic;
using System.Text;

namespace VagabondK.Protocols.Modbus
{
    /// <summary>
    /// Modbus 엔디안 확장 메서드 모음
    /// </summary>
    public static class ModbusEndianExtensions
    {
        /// <summary>
        /// 엔디안으로 정렬
        /// </summary>
        /// <param name="modbusEndian">Modbus 엔디안</param>
        /// <param name="bytes">Byte 배열</param>
        /// <param name="useBitConverter">정렬 시 BitConverter.IsLittleEndian을 고려할 지 여부</param>
        /// <returns>정렬된 Byte 배열</returns>
        public static byte[] Sort(this ModbusEndian modbusEndian, byte[] bytes, bool useBitConverter = true)
        {
            var outerBigEndian = modbusEndian.HasFlag(ModbusEndian.OuterBig);
            var innerBigEndian = modbusEndian.HasFlag(ModbusEndian.InnerBig);

            outerBigEndian = useBitConverter ? outerBigEndian != BitConverter.IsLittleEndian : outerBigEndian;
            innerBigEndian = useBitConverter ? innerBigEndian != BitConverter.IsLittleEndian : innerBigEndian;

            var count = bytes.Length / 2;
            byte temp;

            if (!outerBigEndian)
            {
                if (!innerBigEndian)
                {
                    for (int i = 0; i < count; i++)
                    {
                        temp = bytes[i];
                        bytes[i] = bytes[bytes.Length - 1 - i];
                        bytes[bytes.Length - 1 - i] = temp;
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        temp = bytes[i];
                        if (i % 2 == 0)
                        {
                            bytes[i] = bytes[bytes.Length - 2 - i];
                            bytes[bytes.Length - 2 - i] = temp;
                        }
                        else
                        {
                            bytes[i] = bytes[bytes.Length - i];
                            bytes[bytes.Length - i] = temp;
                        }
                    }
                }
            }
            else if (!innerBigEndian)
            {
                for (int i = 0; i < count; i++)
                {
                    temp = bytes[i * 2];
                    bytes[i * 2] = bytes[i * 2 + 1];
                    bytes[i * 2 + 1] = temp;
                }
            }

            return bytes;
        }

        /// <summary>
        /// Modbus 엔디안의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <param name="modbusEndian">Modbus 엔디안</param>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public static string ToString(this ModbusEndian modbusEndian)
        {
            var outerBigEndian = modbusEndian.HasFlag(ModbusEndian.OuterBig);
            var innerBigEndian = modbusEndian.HasFlag(ModbusEndian.InnerBig);

            return outerBigEndian ? innerBigEndian ? "ABCD" : "BADC" : innerBigEndian ? "CDAB" : "DCBA";
        }

    }
}
