using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VagabondK.Protocols.LSElectric
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) PLC 디바이스 변수
    /// </summary>
    public struct DeviceVariable
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="deviceType">LS ELECTRIC PLC 디바이스 영역</param>
        /// <param name="dataType">LS ELECTRIC PLC 데이터 형식</param>
        /// <param name="index">인덱스</param>
        /// <param name="subIndices">세부 인덱스 목록(U 영역의 .을 이용한 분리 인덱스)</param>
        public DeviceVariable(DeviceType deviceType, DataType dataType, uint index, params byte[] subIndices)
        {
            DeviceType = deviceType;
            DataType = dataType;
            Index = index;
            SubIndices = subIndices.ToArray();
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
        /// 인덱스
        /// </summary>
        public uint Index { get; }

        /// <summary>
        /// 세부 인덱스 목록
        /// </summary>
        public IReadOnlyList<byte> SubIndices { get; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString()
        {
            if (DeviceType == DeviceType.U && DataType == DataType.Bit)
            {
                StringBuilder stringBuilder = new StringBuilder($"%{(char)DeviceType}{(char)DataType}{Index:X}");

                foreach (var subIndex in SubIndices)
                {
                    stringBuilder.Append('.');
                    stringBuilder.Append(subIndex.ToString("X"));
                }
                return stringBuilder.ToString();
            }
            else
            {
                StringBuilder stringBuilder = new StringBuilder($"%{(char)DeviceType}{(char)DataType}{Index}");
                foreach (var subIndex in SubIndices)
                {
                    stringBuilder.Append('.');
                    stringBuilder.Append(subIndex);
                }
                return stringBuilder.ToString();
            }
        }

        /// <summary>
        /// 변수 문자열을 바이트 배열로 반환합니다.
        /// </summary>
        /// <returns>변수 문자열을 바이트 배열</returns>
        public byte[] ToBytes()
        {
            return Encoding.ASCII.GetBytes(ToString());
        }

        /// <summary>
        /// 문자열을 디바이스 변수로 해석합니다.
        /// </summary>
        /// <param name="text">문자열</param>
        /// <returns>디바이스 변수</returns>
        public static DeviceVariable Parse(string text)
        {
            var exception = TryParseCore(text, out DeviceVariable result);
            if (exception != null)
                throw exception;
            return result;
        }

        /// <summary>
        /// 문자열을 디바이스 변수로 해석 시도합니다.
        /// </summary>
        /// <param name="text">문자열</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <returns>성공 여부</returns>
        public static bool TryParse(string text, out DeviceVariable deviceVariable) => TryParseCore(text, out deviceVariable) == null;

        private static Exception TryParseCore(string text, out DeviceVariable deviceVariable)
        {
            if (text == null)
            {
                deviceVariable = new DeviceVariable();
                return new ArgumentNullException(nameof(text));
            }
            else if (text.Length < 4
                || text[0] != '%'
                || !Enum.IsDefined(typeof(DeviceType), (byte)text[1])
                || !Enum.IsDefined(typeof(DataType), (byte)text[2]))
            {
                deviceVariable = new DeviceVariable();
                return new FormatException();
            }
            else
            {
                DeviceType deviceType = (DeviceType)(byte)text[1];
                DataType dataType = (DataType)(byte)text[2];

                var indexTexts = text.Remove(0, 3).Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                List<uint> indices = new List<uint>();

                foreach (var indexText in indexTexts)
                {
                    if (deviceType == DeviceType.U && dataType == DataType.Bit)
                    {
                        if (!uint.TryParse(indexText, System.Globalization.NumberStyles.HexNumber, null, out var index))
                        {
                            deviceVariable = new DeviceVariable();
                            return new FormatException();
                        }
                        indices.Add(index);
                    }
                    else
                    {
                        if (!uint.TryParse(indexText, out var index))
                        {
                            deviceVariable = new DeviceVariable();
                            return new FormatException();
                        }
                        indices.Add(index);
                    }
                }

                if (indices.Count == 1)
                    deviceVariable = new DeviceVariable(deviceType, dataType, indices[0]);
                else if (indices.Count > 1)
                    deviceVariable = new DeviceVariable(deviceType, dataType, indices[0], indices.Skip(1).Select(i => (byte)i).ToArray());
                else
                {
                    deviceVariable = new DeviceVariable();
                    return new FormatException();
                }
                return null;
            }
        }

        /// <summary>
        /// 문자열에 대한 디바이스 변수 형 변환
        /// </summary>
        /// <param name="text">문자열</param>
        /// <returns>디바이스 변수</returns>
        public static implicit operator DeviceVariable(string text) => Parse(text);

        
        internal DeviceVariable Increase()
        {
            if (SubIndices.Count == 0)
            {
                return new DeviceVariable(DeviceType, DataType, Index + 1);
            }
            else
            {
                var subAddresses = SubIndices.ToArray();
                subAddresses[subAddresses.Length - 1] += 1;
                return new DeviceVariable(DeviceType, DataType, Index, subAddresses);
            }
        }
    }
}
