using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace VagabondK.Protocols.LSElectric
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) PLC 디바이스 값 
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct DeviceValue
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="bitValue">비트 값</param>
        public DeviceValue(bool bitValue) : this() => this.bitValue = bitValue;
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="byteValue">바이트 값</param>
        public DeviceValue(byte byteValue) : this() => this.byteValue = byteValue;
        /// <summary>
        /// 생서자
        /// </summary>
        /// <param name="wordValue">부호 있는 워드 값</param>
        public DeviceValue(short wordValue) : this() => this.wordValue = wordValue;
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="doubleWordValue">부호 있는 더블 워드 값</param>
        public DeviceValue(int doubleWordValue) : this() => this.doubleWordValue = doubleWordValue;
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="longWordValue">부호 있는 롱 워드 값</param>
        public DeviceValue(long longWordValue) : this() => this.longWordValue = longWordValue;
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="signedByteValue">부호 있는 바이트 값</param>
        public DeviceValue(sbyte signedByteValue) : this() => this.signedByteValue = signedByteValue;
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="unsignedWordValue">부호 없는 워드 값</param>
        public DeviceValue(ushort unsignedWordValue) : this() => this.unsignedWordValue = unsignedWordValue;
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="unsignedDoubleWordValue">부호 없는 더블 워드 값</param>
        public DeviceValue(uint unsignedDoubleWordValue) : this() => this.unsignedDoubleWordValue = unsignedDoubleWordValue;
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="unsignedLongWordValue">부호 없는 롱 워드 값</param>
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

        /// <summary>
        /// 비트 값
        /// </summary>
        public bool BitValue
        {
            get => bitValue;
            set { unsignedLongWordValue = 0; bitValue = value; }
        }

        /// <summary>
        /// 바이트 값
        /// </summary>
        public byte ByteValue
        {
            get => byteValue;
            set { unsignedLongWordValue = 0; byteValue = value; }
        }

        /// <summary>
        /// 부호 있는 워드 값
        /// </summary>
        public short WordValue
        {
            get => wordValue;
            set { unsignedLongWordValue = 0; wordValue = value; }
        }

        /// <summary>
        /// 부호 있는 더블 워드 값
        /// </summary>
        public int DoubleWordValue
        {
            get => doubleWordValue;
            set { unsignedLongWordValue = 0; doubleWordValue = value; }
        }

        /// <summary>
        /// 부호 있는 롱 워드 값
        /// </summary>
        public long LongWordValue
        {
            get => longWordValue;
            set { longWordValue = value; }
        }

        /// <summary>
        /// 부호 있는 바이트 값
        /// </summary>
        public sbyte SignedByteValue
        {
            get => signedByteValue;
            set { unsignedLongWordValue = 0; signedByteValue = value; }
        }

        /// <summary>
        /// 부호 없는 워드 값
        /// </summary>
        public ushort UnsignedWordValue
        {
            get => unsignedWordValue;
            set { unsignedLongWordValue = 0; unsignedWordValue = value; }
        }

        /// <summary>
        /// 부호 없는 더블 워드 값
        /// </summary>
        public uint UnsignedDoubleWordValue
        {
            get => unsignedDoubleWordValue;
            set { unsignedLongWordValue = 0; unsignedDoubleWordValue = value; }
        }

        /// <summary>
        /// 부호 없는 롱 워드 값
        /// </summary>
        public ulong UnsignedLongWordValue
        {
            get => unsignedLongWordValue;
            set { unsignedLongWordValue = value; }
        }

        /// <summary>
        /// 디바이스 값의 bool 형식으로의 변환
        /// </summary>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>bool 형식 값</returns>
        public static implicit operator bool(DeviceValue deviceValue) => deviceValue.bitValue;
        /// <summary>
        /// 디바이스 값의 byte 형식으로의 변환
        /// </summary>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>byte 형식 값</returns>
        public static implicit operator byte(DeviceValue deviceValue) => deviceValue.byteValue;
        /// <summary>
        /// 디바이스 값의 short 형식으로의 변환
        /// </summary>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>short 형식 값</returns>
        public static implicit operator short(DeviceValue deviceValue) => deviceValue.wordValue;
        /// <summary>
        /// 디바이스 값의 int 형식으로의 변환
        /// </summary>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>int 형식 값</returns>
        public static implicit operator int(DeviceValue deviceValue) => deviceValue.doubleWordValue;
        /// <summary>
        /// 디바이스 값의 long 형식으로의 변환
        /// </summary>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>long 형식 값</returns>
        public static implicit operator long(DeviceValue deviceValue) => deviceValue.longWordValue;
        /// <summary>
        /// 디바이스 값의 sbyte 형식으로의 변환
        /// </summary>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>sbyte 형식 값</returns>
        public static implicit operator sbyte(DeviceValue deviceValue) => deviceValue.signedByteValue;
        /// <summary>
        /// 디바이스 값의 ushort 형식으로의 변환
        /// </summary>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>ushort 형식 값</returns>
        public static implicit operator ushort(DeviceValue deviceValue) => deviceValue.unsignedWordValue;
        /// <summary>
        /// 디바이스 값의 uint 형식으로의 변환
        /// </summary>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>uint 형식 값</returns>
        public static implicit operator uint(DeviceValue deviceValue) => deviceValue.unsignedDoubleWordValue;
        /// <summary>
        /// 디바이스 값의 ulong 형식으로의 변환
        /// </summary>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>ulong 형식 값</returns>
        public static implicit operator ulong(DeviceValue deviceValue) => deviceValue.unsignedLongWordValue;

        /// <summary>
        /// bool 형식 값의 디바이스 값으로의 변환
        /// </summary>
        /// <param name="value">bool 형식 값</param>
        /// <returns>디바이스 값</returns>
        public static implicit operator DeviceValue(bool value) => new DeviceValue(value);
        /// <summary>
        /// byte 형식 값의 디바이스 값으로의 변환
        /// </summary>
        /// <param name="value">byte 형식 값</param>
        /// <returns>디바이스 값</returns>
        public static implicit operator DeviceValue(byte value) => new DeviceValue(value);
        /// <summary>
        /// short 형식 값의 디바이스 값으로의 변환
        /// </summary>
        /// <param name="value">short 형식 값</param>
        /// <returns>디바이스 값</returns>
        public static implicit operator DeviceValue(short value) => new DeviceValue(value);
        /// <summary>
        /// int 형식 값의 디바이스 값으로의 변환
        /// </summary>
        /// <param name="value">int 형식 값</param>
        /// <returns>디바이스 값</returns>
        public static implicit operator DeviceValue(int value) => new DeviceValue(value);
        /// <summary>
        /// long 형식 값의 디바이스 값으로의 변환
        /// </summary>
        /// <param name="value">long 형식 값</param>
        /// <returns>디바이스 값</returns>
        public static implicit operator DeviceValue(long value) => new DeviceValue(value);
        /// <summary>
        /// sbyte 형식 값의 디바이스 값으로의 변환
        /// </summary>
        /// <param name="value">sbyte 형식 값</param>
        /// <returns>디바이스 값</returns>
        public static implicit operator DeviceValue(sbyte value) => new DeviceValue(value);
        /// <summary>
        /// ushort 형식 값의 디바이스 값으로의 변환
        /// </summary>
        /// <param name="value">ushort 형식 값</param>
        /// <returns>디바이스 값</returns>
        public static implicit operator DeviceValue(ushort value) => new DeviceValue(value);
        /// <summary>
        /// uint 형식 값의 디바이스 값으로의 변환
        /// </summary>
        /// <param name="value">uint 형식 값</param>
        /// <returns>디바이스 값</returns>
        public static implicit operator DeviceValue(uint value) => new DeviceValue(value);
        /// <summary>
        /// ulong 형식 값의 디바이스 값으로의 변환
        /// </summary>
        /// <param name="value">ulong 형식 값</param>
        /// <returns>디바이스 값</returns>
        public static implicit operator DeviceValue(ulong value) => new DeviceValue(value);

        /// <summary>
        /// ModbusEndian의 지정된 두 인스턴스가 같은지를 확인합니다.
        /// </summary>
        /// <param name="value1">비교할 첫 번째 개체입니다.</param>
        /// <param name="value2">비교할 두 번째 개체입니다.</param>
        /// <returns>value1 및 value2가 동일하면 true이고, 그렇지 않으면 false입니다.</returns>
        public static bool operator ==(DeviceValue value1, DeviceValue value2) => value1.Equals(value2);

        /// <summary>
        /// ModbusEndian의 지정된 두 인스턴스가 다른지를 확인합니다.
        /// </summary>
        /// <param name="value1">비교할 첫 번째 개체입니다.</param>
        /// <param name="value2">비교할 두 번째 개체입니다.</param>
        /// <returns>value1 및 value2가 동일하지 않으면 true이고, 그렇지 않으면 false입니다.</returns>
        public static bool operator !=(DeviceValue value1, DeviceValue value2) => !value1.Equals(value2);

        /// <summary>
        /// 이 인스턴스와 지정된 개체가 같은지 여부를 나타냅니다.
        /// </summary>
        /// <param name="obj">현재 인스턴스와 비교할 개체입니다.</param>
        /// <returns>true와 이 인스턴스가 동일한 형식이고 동일한 값을 나타내면 obj이고, 그렇지 않으면 false입니다.</returns>
        public override bool Equals(object obj) => base.Equals(obj);

        /// <summary>
        /// 이 인스턴스의 해시 코드를 반환합니다.
        /// </summary>
        /// <returns>이 인스턴스의 해시 코드인 32비트 부호 있는 정수입니다.</returns>
        public override int GetHashCode() => base.GetHashCode();
    }
}
