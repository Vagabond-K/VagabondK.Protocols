using System;
using System.Linq;
using VagabondK.Protocols.LSIS;
using VagabondK.Protocols.LSIS.Cnet;

namespace SimpleCnetClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var read = BitConverter.ToString(new CnetReadEachAddressRequest(0x20) { "%MW100" }.Serialize().ToArray());
            var readBlock = BitConverter.ToString(new CnetReadAddressBlockRequest(0x10, "%MW100", 5).Serialize().ToArray());
            var write = BitConverter.ToString(new CnetWriteEachAddressRequest(0x20) { ["%MW100"] = 0x00e2 }.Serialize().ToArray());
            var writelock = BitConverter.ToString(new CnetWriteAddressBlockRequest(0x10, "%MW100") { 0x1111, 0x2222 }.Serialize().ToArray());
            var monitor = BitConverter.ToString(new CnetRegisterMonitorEachAddressRequest(0x20, 0x09) { "%MW100" }.Serialize().ToArray());
            var monitorExe = BitConverter.ToString(new CnetRegisterMonitorEachAddressRequest(0x20, 0x09) { "%MW100" }.CreateExecuteMonitorRequest().Serialize().ToArray());

            var commands = $"{read}\r\n\r\n{readBlock}\r\n\r\n{write}\r\n\r\n{writelock}\r\n\r\n{monitor}\r\n\r\n{monitorExe}";
        }
    }
}
