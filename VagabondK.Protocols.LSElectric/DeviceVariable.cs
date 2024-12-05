using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
        public override string ToString() => ToString(false);

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <param name="useHexBitIndex">비트 변수의 인덱스를 16진수로 표시할지 여부를 결정합니다.</param>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public string ToString(bool useHexBitIndex)
        {
            StringBuilder stringBuilder;

            if (DataType != DataType.Bit || !useHexBitIndex)
            {
                if (DeviceType == DeviceType.U && DataType == DataType.Bit)
                {
                    stringBuilder = new StringBuilder($"%{(char)DeviceType}{(char)DataType}{Index:X}");

                    foreach (var subIndex in SubIndices)
                    {
                        stringBuilder.Append('.');
                        stringBuilder.Append(subIndex.ToString("X"));
                    }
                    return stringBuilder.ToString();
                }
                else
                {
                    stringBuilder = new StringBuilder($"%{(char)DeviceType}{(char)DataType}{Index}");
                    foreach (var subIndex in SubIndices)
                    {
                        stringBuilder.Append('.');
                        stringBuilder.Append(subIndex);
                    }
                    return stringBuilder.ToString();
                }
            }
            else
            {
                //P, M, L, K, F 이면서 Bit일 경우 16진수
                //그 외에는 인덱스가 .으로 나누어져있고 Bit일 경우 마지막 자리만 16진수
                switch (DeviceType)
                {
                    case DeviceType.P:
                    case DeviceType.M:
                    case DeviceType.L:
                    case DeviceType.K:
                    case DeviceType.F:
                        return $"%{(char)DeviceType}{(char)DataType}{Index:X}";
                    case DeviceType.U:
                        stringBuilder = new StringBuilder($"%{(char)DeviceType}{(char)DataType}{Index:X}");
                        foreach (var subIndex in SubIndices)
                        {
                            stringBuilder.Append('.');
                            stringBuilder.Append(subIndex.ToString("X"));
                        }
                        return stringBuilder.ToString();
                    default:
                        stringBuilder = new StringBuilder($"%{(char)DeviceType}{(char)DataType}{Index}");
                        for (int i = 0; i < SubIndices.Count; i++)
                        {
                            stringBuilder.Append('.');
                            stringBuilder.Append(i == SubIndices.Count - 1 ? SubIndices[i].ToString("X") : SubIndices[i].ToString());
                        }
                        return stringBuilder.ToString();
                }
            }
        }


        /// <summary>
        /// 변수 문자열을 바이트 배열로 반환합니다.
        /// </summary>
        /// <returns>변수 문자열을 바이트 배열</returns>
        public byte[] ToBytes() => ToBytes(false);

        /// <summary>
        /// 변수 문자열을 바이트 배열로 반환합니다.
        /// </summary>
        /// <param name="useHexBitIndex">비트 변수의 인덱스를 16진수로 표시할지 여부를 결정합니다.</param>
        /// <returns>변수 문자열을 바이트 배열</returns>
        public byte[] ToBytes(bool useHexBitIndex) => Encoding.ASCII.GetBytes(ToString(useHexBitIndex));

        /// <summary>
        /// 문자열을 디바이스 변수로 해석합니다.
        /// </summary>
        /// <param name="text">문자열</param>
        /// <returns>디바이스 변수</returns>
        public static DeviceVariable Parse(string text) => Parse(text, false);

        /// <summary>
        /// 문자열을 디바이스 변수로 해석합니다.
        /// </summary>
        /// <param name="text">문자열</param>
        /// <param name="useHexBitIndex">비트 변수의 인덱스를 16진수로 인식할지 여부를 결정합니다.</param>
        /// <returns>디바이스 변수</returns>
        public static DeviceVariable Parse(string text, bool useHexBitIndex)
        {
            var exception = TryParseCore(text, useHexBitIndex, out DeviceVariable result);
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
        public static bool TryParse(string text, out DeviceVariable deviceVariable) => TryParseCore(text, false, out deviceVariable) == null;

        /// <summary>
        /// 문자열을 디바이스 변수로 해석 시도합니다.
        /// </summary>
        /// <param name="text">문자열</param>
        /// <param name="useHexBitIndex">비트 변수의 인덱스를 16진수로 인식할지 여부를 결정합니다.</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <returns>성공 여부</returns>
        public static bool TryParse(string text, bool useHexBitIndex, out DeviceVariable deviceVariable) => TryParseCore(text, useHexBitIndex, out deviceVariable) == null;

        private static Exception TryParseCore(string text, bool useHexBitIndex, out DeviceVariable deviceVariable)
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

                for (int i = 0; i < indexTexts.Length; i++)
                {
                    var indexText = indexTexts[i];
                    uint index = 0;
                    
                    switch (deviceType)
                    {
                        case DeviceType.P:
                        case DeviceType.M:
                        case DeviceType.L:
                        case DeviceType.K:
                        case DeviceType.F:
                            if (!useHexBitIndex && indexText.Any(c => !char.IsDigit(c))
                                || !uint.TryParse(indexText, useHexBitIndex && dataType == DataType.Bit ? NumberStyles.HexNumber : NumberStyles.Number, null, out index))
                            {
                                deviceVariable = new DeviceVariable();
                                return new FormatException();
                            }
                            indices.Add(index);
                            break;
                        case DeviceType.U:
                            if (!uint.TryParse(indexText, NumberStyles.HexNumber, null, out index))
                            {
                                deviceVariable = new DeviceVariable();
                                return new FormatException();
                            }
                            indices.Add(index);
                            break;
                        default:
                            if (!useHexBitIndex && indexText.Any(c => !char.IsDigit(c))
                                || !uint.TryParse(indexText, i == indexTexts.Length - 1 && useHexBitIndex && dataType == DataType.Bit ? NumberStyles.HexNumber : NumberStyles.Number, null, out index))
                            {
                                deviceVariable = new DeviceVariable();
                                return new FormatException();
                            }
                            indices.Add(index);
                            break;
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

        /// <summary>
        /// ModbusEndian의 지정된 두 인스턴스가 같은지를 확인합니다.
        /// </summary>
        /// <param name="variable1">비교할 첫 번째 개체입니다.</param>
        /// <param name="variable2">비교할 두 번째 개체입니다.</param>
        /// <returns>variable1 및 variable2가 동일하면 true이고, 그렇지 않으면 false입니다.</returns>
        public static bool operator ==(DeviceVariable variable1, DeviceVariable variable2)
            => variable1.DeviceType == variable2.DeviceType
            && variable1.DataType == variable2.DataType
            && variable1.Index == variable2.Index
            && variable1.SubIndices.SequenceEqual(variable2.SubIndices);

        /// <summary>
        /// ModbusEndian의 지정된 두 인스턴스가 다른지를 확인합니다.
        /// </summary>
        /// <param name="variable1">비교할 첫 번째 개체입니다.</param>
        /// <param name="variable2">비교할 두 번째 개체입니다.</param>
        /// <returns>variable1 및 variable2가 동일하지 않으면 true이고, 그렇지 않으면 false입니다.</returns>
        public static bool operator !=(DeviceVariable variable1, DeviceVariable variable2) => !(variable1 == variable2);

        /// <summary>
        /// 이 인스턴스와 지정된 개체가 같은지 여부를 나타냅니다.
        /// </summary>
        /// <param name="obj">현재 인스턴스와 비교할 개체입니다.</param>
        /// <returns>true와 이 인스턴스가 동일한 형식이고 동일한 값을 나타내면 obj이고, 그렇지 않으면 false입니다.</returns>
        public override bool Equals(object obj) => obj is DeviceVariable variable && variable == this;

        /// <summary>
        /// 이 인스턴스의 해시 코드를 반환합니다.
        /// </summary>
        /// <returns>이 인스턴스의 해시 코드인 32비트 부호 있는 정수입니다.</returns>
        public override int GetHashCode() => ToString().GetHashCode();

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
