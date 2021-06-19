using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace VagabondK.Protocols.LSElectric
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct DeviceValue
    {
        public DeviceValue(bool bitValue) : this() => this.bitValue = bitValue;
        public DeviceValue(byte byteValue) : this() => this.byteValue = byteValue;
        public DeviceValue(short wordValue) : this() => this.wordValue = wordValue;
        public DeviceValue(int doubleWordValue) : this() => this.doubleWordValue = doubleWordValue;
        public DeviceValue(long longWordValue) : this() => this.longWordValue = longWordValue;
        public DeviceValue(sbyte signedByteValue) : this() => this.signedByteValue = signedByteValue;
        public DeviceValue(ushort unsignedWordValue) : this() => this.unsignedWordValue = unsignedWordValue;
        public DeviceValue(uint unsignedDoubleWordValue) : this() => this.unsignedDoubleWordValue = unsignedDoubleWordValue;
        public DeviceValue(ulong unsignedLongWordValue) : this() => this.unsignedLongWordValue = unsignedLongWordValue;

        [FieldOffset(0)] private bool bitValue;
        [FieldOffset(0)] private byte byteValue;
        [FieldOffset(0)] private short wordValue;
        [FieldOffset(0)] private int doubleWordValue;
        [FieldOffset(0)] private long longWordValue;
        [FieldOffset(0)] private sbyte signedByteValue;
        [FieldOffset(0)] private ushort unsignedWordValue;
        [FieldOffset(0)] private uint unsignedDoubleWordValue;
        [FieldOffset(0)] private ulong unsignedLongWordValue;

        public bool BitValue
        {
            get => bitValue;
            set { unsignedLongWordValue = 0; bitValue = value; }
        }

        public byte ByteValue
        {
            get => byteValue;
            set { unsignedLongWordValue = 0; byteValue = value; }
        }

        public short WordValue
        {
            get => wordValue;
            set { unsignedLongWordValue = 0; wordValue = value; }
        }

        public int DoubleWordValue
        {
            get => doubleWordValue;
            set { unsignedLongWordValue = 0; doubleWordValue = value; }
        }

        public long LongWordValue
        {
            get => longWordValue;
            set { longWordValue = value; }
        }

        public sbyte SignedByteValue
        {
            get => signedByteValue;
            set { unsignedLongWordValue = 0; signedByteValue = value; }
        }

        public ushort UnsignedWordValue
        {
            get => unsignedWordValue;
            set { unsignedLongWordValue = 0; unsignedWordValue = value; }
        }

        public uint UnsignedDoubleWordValue
        {
            get => unsignedDoubleWordValue;
            set { unsignedLongWordValue = 0; unsignedDoubleWordValue = value; }
        }

        public ulong UnsignedLongWordValue
        {
            get => unsignedLongWordValue;
            set { unsignedLongWordValue = value; }
        }

        public static implicit operator bool(DeviceValue value) => value.bitValue;
        public static implicit operator byte(DeviceValue value) => value.byteValue;
        public static implicit operator short(DeviceValue value) => value.wordValue;
        public static implicit operator int(DeviceValue value) => value.doubleWordValue;
        public static implicit operator long(DeviceValue value) => value.longWordValue;
        public static implicit operator sbyte(DeviceValue value) => value.signedByteValue;
        public static implicit operator ushort(DeviceValue value) => value.unsignedWordValue;
        public static implicit operator uint(DeviceValue value) => value.unsignedDoubleWordValue;
        public static implicit operator ulong(DeviceValue value) => value.unsignedLongWordValue;

        public static implicit operator DeviceValue(bool value) => new DeviceValue(value);
        public static implicit operator DeviceValue(byte value) => new DeviceValue(value);
        public static implicit operator DeviceValue(short value) => new DeviceValue(value);
        public static implicit operator DeviceValue(int value) => new DeviceValue(value);
        public static implicit operator DeviceValue(long value) => new DeviceValue(value);
        public static implicit operator DeviceValue(sbyte value) => new DeviceValue(value);
        public static implicit operator DeviceValue(ushort value) => new DeviceValue(value);
        public static implicit operator DeviceValue(uint value) => new DeviceValue(value);
        public static implicit operator DeviceValue(ulong value) => new DeviceValue(value);
    }
}
