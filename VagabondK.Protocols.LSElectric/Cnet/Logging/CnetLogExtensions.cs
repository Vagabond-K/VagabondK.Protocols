using System.Collections.Generic;
using System.Text;
using VagabondK.Protocols.LSElectric.Cnet;

namespace VagabondK.Protocols.Logging
{
    static class CnetLogExtensions
    {
        public static string CnetRawMessageToString(this IReadOnlyList<byte> bytes)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var b in bytes)
            {
                switch (b)
                {
                    case CnetMessage.ENQ:
                        stringBuilder.Append($"{{{nameof(CnetMessage.ENQ)}}}");
                        break;
                    case CnetMessage.EOT:
                        stringBuilder.Append($"{{{nameof(CnetMessage.EOT)}}}");
                        break;
                    case CnetMessage.ACK:
                        stringBuilder.Append($"{{{nameof(CnetMessage.ACK)}}}");
                        break;
                    case CnetMessage.NAK:
                        stringBuilder.Append($"{{{nameof(CnetMessage.NAK)}}}");
                        break;
                    case CnetMessage.ETX:
                        stringBuilder.Append($"{{{nameof(CnetMessage.ETX)}}}");
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
    }
}
