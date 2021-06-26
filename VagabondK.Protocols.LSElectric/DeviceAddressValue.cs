using System;
using System.Collections.Generic;
using System.Text;

namespace VagabondK.Protocols.LSElectric
{
    public class DeviceAddressValue
    {
        public DeviceAddressValue(DeviceAddress deviceAddress)
        {
            DeviceAddress = deviceAddress;
        }

        public DeviceAddress DeviceAddress { get; }
        public DeviceValue DeviceValue { get; }
    }
}
