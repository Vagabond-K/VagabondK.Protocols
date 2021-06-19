using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VagabondK.Protocols.LSElectric
{
    /// <summary>
    /// LS ELECTRIC PLC 디바이스 주소
    /// </summary>
    public struct DeviceAddress
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="deviceType">LS ELECTRIC PLC 디바이스 영역</param>
        /// <param name="dataType">LS ELECTRIC PLC 데이터 형식</param>
        /// <param name="address">주소</param>
        /// <param name="subAddresses">세부 주소 목록(U 영역의 .을 이용한 분리 주소)</param>
        public DeviceAddress(DeviceType deviceType, DataType dataType, uint address, params byte[] subAddresses)
        {
            DeviceType = deviceType;
            DataType = dataType;
            Address = address;
            SubAddresses = subAddresses.ToArray();
        }

        /// <summary>
        /// LS ELECTRIC PLC 디바이스 영역
        /// </summary>
        public DeviceType DeviceType { get; }

        /// <summary>
        /// LS ELECTRIC PLC 데이터 형식
        /// </summary>
        public DataType DataType { get; }

        /// <summary>
        /// 주소
        /// </summary>
        public uint Address { get; }

        /// <summary>
        /// 세부 주소 목록
        /// </summary>
        public IReadOnlyList<byte> SubAddresses { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString()
        {
            //TODO: U영역에 대한 고려 추가해야 함.
            return $"%{(char)DeviceType}{(char)DataType}{Address}";
        }

        /// <summary>
        /// 주소 문자열을 바이트 배열로 반환합니다.
        /// </summary>
        /// <returns>주소 문자열을 바이트 배열</returns>
        public byte[] ToBytes()
        {
            return Encoding.ASCII.GetBytes(ToString());
        }

        public static DeviceAddress Parse(string s)
        {
            var exception = TryParseCore(s, out DeviceAddress result);
            if (exception != null)
                throw exception;
            return result;
        }

        public static bool TryParse(string s, out DeviceAddress deviceAddress) => TryParseCore(s, out deviceAddress) == null;

        public static Exception TryParseCore(string s, out DeviceAddress deviceAddress)
        {
            //TODO: U영역에 대한 고려 추가해야 함.
            if (s == null)
            {
                deviceAddress = new DeviceAddress();
                return new ArgumentNullException(nameof(s));
            }
            else if (s.Length < 4
                || s[0] != '%'
                || !Enum.IsDefined(typeof(DeviceType), (byte)s[1])
                || !Enum.IsDefined(typeof(DataType), (byte)s[2])
                || !uint.TryParse(s.Remove(0, 3), out var address))
            {
                deviceAddress = new DeviceAddress();
                return new FormatException();
            }
            else
            {
                deviceAddress = new DeviceAddress((DeviceType)(byte)s[1], (DataType)(byte)s[2], address);
                return null;
            }
        }

        public static implicit operator DeviceAddress(string value) => Parse(value);

        public DeviceAddress Increase()
        {
            if (SubAddresses.Count == 0)
            {
                return new DeviceAddress(DeviceType, DataType, Address + 1);
            }
            else
            {
                var subAddresses = SubAddresses.ToArray();
                subAddresses[subAddresses.Length - 1] += 1;
                return new DeviceAddress(DeviceType, DataType, Address, subAddresses);
            }
        }
    }
}
