using System;
using System.Collections.Generic;
using System.Text;
using VagabondK.Protocols.Modbus.Serialization;

namespace VagabondK.Protocols.Logging
{
    static class ModbusLogExtensions
    {
        internal static string ModbusRawMessageToString(this IReadOnlyList<byte> bytes, ModbusSerializer serializer)
        {
            if (serializer is ModbusAsciiSerializer)
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (var b in bytes)
                {
                    switch (b)
                    {
                        case 0x0D:
                            stringBuilder.Append("\\r");
                            break;
                        case 0x0A:
                            stringBuilder.Append("\\n");
                            break;
                        default:
                            if (b >= 33 && b <= 126)
                                stringBuilder.Append((char)b);
                            else
                                stringBuilder.Append(b.ToString("X2"));
                            break;
                    }
                }
                return stringBuilder.ToString();
            }

            return BitConverter.ToString(bytes as byte[]).Replace('-', ' ');
        }
    }
}
