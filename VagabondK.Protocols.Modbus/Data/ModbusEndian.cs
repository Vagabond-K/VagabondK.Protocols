using System;

namespace VagabondK.Protocols.Modbus.Data
{
    /// <summary>
    /// Modbus 엔디안
    /// </summary>
    public struct ModbusEndian
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="isBigEndian">전체 빅 엔디안 여부</param>
        public ModbusEndian(bool isBigEndian)
        {
            InnerBigEndian = isBigEndian;
            OuterBigEndian = isBigEndian;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="innerBigEndian">레지스터 내부 빅 엔디안 여부</param>
        /// <param name="outerBigEndian">레지스터 단위 빅 엔디안 여부</param>
        public ModbusEndian(bool innerBigEndian, bool outerBigEndian)
        {
            InnerBigEndian = innerBigEndian;
            OuterBigEndian = outerBigEndian;
        }

        /// <summary>
        /// 레지스터 내부 빅 엔디안 여부
        /// </summary>
        public bool InnerBigEndian { get; }

        /// <summary>
        /// 레지스터 단위 빅 엔디안 여부
        /// </summary>
        public bool OuterBigEndian { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString()
        {
            if (OuterBigEndian)
            {
                if (InnerBigEndian) return "BADC";
                else return "DCBA";
            }
            else
            {
                if (InnerBigEndian) return "ABCD";
                else return "CDAB";
            }
        }

        /// <summary>
        /// 전체 빅 엔디안
        /// </summary>
        public static readonly ModbusEndian AllBig = new ModbusEndian(true, true);

        /// <summary>
        /// 전체 리틀 엔디안
        /// </summary>
        public static readonly ModbusEndian AllLittle = new ModbusEndian(true, true);

        /// <summary>
        /// 엔디안 반전하기
        /// </summary>
        /// <returns>반전된 엔디안</returns>
        public ModbusEndian Reverse() => new ModbusEndian(!InnerBigEndian, !OuterBigEndian);

        /// <summary>
        /// 현재 엔디안으로 정렬
        /// </summary>
        /// <param name="bytes">바이트 배열</param>
        /// <returns>정렬된 바이트 배열</returns>
        public byte[] Sort(byte[] bytes)
        {
            if (bytes.Length % 2 == 1)
                Array.Resize(ref bytes, bytes.Length / 2 * 2);

            var count = bytes.Length / 2;
            byte temp;

            if (OuterBigEndian == BitConverter.IsLittleEndian)
            {
                if (InnerBigEndian == BitConverter.IsLittleEndian)
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
            else if (InnerBigEndian == BitConverter.IsLittleEndian)
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
    }
}
