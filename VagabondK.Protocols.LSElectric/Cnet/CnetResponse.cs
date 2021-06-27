using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VagabondK.Protocols.LSElectric.Cnet
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 응답 메시지
    /// </summary>
    public abstract class CnetResponse : CnetMessage, IResponse
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="request">요청 메시지</param>
        internal CnetResponse(CnetRequest request)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
        }

        /// <summary>
        /// LS ELECTRIC Cnet 프로토콜 요청 메시지
        /// </summary>
        public CnetRequest Request { get; private set; }


        /// <summary>
        /// 커맨드
        /// </summary>
        public CnetCommand Command { get; }

        /// <summary>
        /// 프레임 종료 테일
        /// </summary>
        public override byte Tail { get => ETX; }

        /// <summary>
        /// 프레임 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        protected override void OnCreateFrame(List<byte> byteList, out bool useBCC)
        {
            byteList.AddRange(Request.Serialize().Skip(1).Take(5));
            OnCreateFrameData(byteList);
            useBCC = Request.UseBCC;
        }

        /// <summary>
        /// 프레임 데이터 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        protected abstract void OnCreateFrameData(List<byte> byteList);
    }


    /// <summary>
    /// 정상 처리 응답 메시지
    /// </summary>
    public class CnetACKResponse : CnetResponse
    {
        internal CnetACKResponse(CnetRequest request) : base(request) { }

        /// <summary>
        /// 프레임 시작 헤더
        /// </summary>
        public override byte Header { get => ACK; }

        /// <summary>
        /// 프레임 데이터 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        protected override void OnCreateFrameData(List<byte> byteList) { }
    }

    /// <summary>
    /// 디바이스 읽기 응답
    /// </summary>
    public class CnetReadResponse : CnetACKResponse, IReadOnlyDictionary<DeviceVariable, DeviceValue>
    {
        internal CnetReadResponse(IEnumerable<DeviceValue> deviceValues, CnetReadRequest request) : base(request)
        {
            switch (request.CommandType)
            {
                case CnetCommandType.Individual:
                    SetData(deviceValues, (CnetReadIndividualRequest)request);
                    break;
                case CnetCommandType.Continuous:
                    SetData(deviceValues, ((CnetReadContinuousRequest)request).ToDeviceVariables());
                    break;
            }
        }

        internal CnetReadResponse(IEnumerable<DeviceValue> deviceValues, CnetExecuteMonitorRequest request) : base(request)
        {
            switch (request.CommandType)
            {
                case CnetCommandType.Individual:
                    SetData(deviceValues, (CnetExecuteMonitorIndividualRequest)request);
                    break;
                case CnetCommandType.Continuous:
                    SetData(deviceValues, ((CnetExecuteMonitorContinuousRequest)request).ToDeviceVariables());
                    break;
            }
        }

        internal CnetReadResponse(IEnumerable<byte> bytes, CnetReadContinuousRequest request) : base(request) => SetData(bytes, request?.StartDeviceVariable ?? new DeviceVariable(), request?.Count ?? 0);
        internal CnetReadResponse(IEnumerable<byte> bytes, CnetExecuteMonitorContinuousRequest request) : base(request) => SetData(bytes, request?.StartDeviceVariable ?? new DeviceVariable(), request?.Count ?? 0);

        private void SetData(IEnumerable<DeviceValue> deviceValues, IEnumerable<DeviceVariable> deviceVariables)
        {
            var valueArray = deviceValues.ToArray();
            var deviceVariableArray = deviceVariables.ToArray();

            if (valueArray.Length != deviceVariableArray.Length) throw new ArgumentOutOfRangeException(nameof(deviceValues));

            deviceValueList = valueArray.Zip(deviceVariableArray, (v, a) => new KeyValuePair<DeviceVariable, DeviceValue>(a, v)).ToList();
            deviceValueDictionary = deviceValueList.ToDictionary(item => item.Key, item =>item.Value);

            if (deviceValueList.Count > 0)
                dataType = deviceValueList[0].Key.DataType;
        }

        private void SetData(IEnumerable<byte> bytes, DeviceVariable deviceVariable, int count)
        {
            var byteArray = bytes.ToArray();

            int valueUnit = 1;

            Func<int, DeviceValue> getValue = null;

            switch (deviceVariable.DataType)
            {
                case DataType.Bit:
                    valueUnit = 1;
                    getValue = (i) => new DeviceValue(byteArray[i * valueUnit] == 0);
                    break;
                case DataType.Byte:
                    valueUnit = 1;
                    getValue = (i) => new DeviceValue(byteArray[i * valueUnit]);
                    break;
                case DataType.Word:
                    valueUnit = 2;
                    getValue = (i) => new DeviceValue(BitConverter.IsLittleEndian 
                        ? BitConverter.ToUInt16(byteArray.Skip(i * valueUnit).Take(valueUnit).Reverse().ToArray(), 0) 
                        : BitConverter.ToUInt16(byteArray, i * valueUnit));
                    break;
                case DataType.DoubleWord:
                    valueUnit = 4;
                    getValue = (i) => new DeviceValue(BitConverter.IsLittleEndian
                        ? BitConverter.ToUInt32(byteArray.Skip(i * valueUnit).Take(valueUnit).Reverse().ToArray(), 0)
                        : BitConverter.ToUInt32(byteArray, i * valueUnit));
                    break;
                case DataType.LongWord:
                    valueUnit = 8;
                    getValue = (i) => new DeviceValue(BitConverter.IsLittleEndian
                        ? BitConverter.ToUInt64(byteArray.Skip(i * valueUnit).Take(valueUnit).Reverse().ToArray(), 0)
                        : BitConverter.ToUInt64(byteArray, i * valueUnit));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(DataType));
            }

            dataType = deviceVariable.DataType;

            if (byteArray.Length != count * valueUnit) throw new ArgumentOutOfRangeException(nameof(bytes));

            deviceValueList = new List<KeyValuePair<DeviceVariable, DeviceValue>>();
            for (int i = 0; i < count; i++)
            {
                deviceValueList.Add(new KeyValuePair<DeviceVariable, DeviceValue>(deviceVariable, getValue(i)));
                deviceVariable = deviceVariable.Increase();
            }
            deviceValueDictionary = deviceValueList.ToDictionary(item => item.Key, item => item.Value);
        }


        private DataType? dataType;
        private List<KeyValuePair<DeviceVariable, DeviceValue>> deviceValueList;
        private Dictionary<DeviceVariable, DeviceValue> deviceValueDictionary;

        /// <summary>
        /// 키로 사용되는 디바이스 변수들
        /// </summary>
        public IEnumerable<DeviceVariable> Keys => deviceValueDictionary.Keys;

        /// <summary>
        /// 읽은 디바이스 변수 값들
        /// </summary>
        public IEnumerable<DeviceValue> Values => deviceValueDictionary.Values;

        /// <summary>
        /// 디바이스 변수 및 값 개수
        /// </summary>
        public int Count => deviceValueDictionary.Count;

        /// <summary>
        /// 지정한 디바이스 변수의 값을 가져옵니다.
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <returns>디바이스 값</returns>
        public DeviceValue this[DeviceVariable deviceVariable] => deviceValueDictionary[deviceVariable];

        /// <summary>
        /// 지정한 디바이스 변수가 포함하는지 여부를 가져옵니다.
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <returns>디바이스 변수 포함 여부</returns>
        public bool ContainsKey(DeviceVariable deviceVariable) => deviceValueDictionary.ContainsKey(deviceVariable);

        /// <summary>
        /// 지정한 디바이스 변수의 값을 가져옵니다.
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>디바이스 변수 포함 여부</returns>
        public bool TryGetValue(DeviceVariable deviceVariable, out DeviceValue deviceValue) => deviceValueDictionary.TryGetValue(deviceVariable, out deviceValue);

        /// <summary>
        /// 디바이스 변수/디바이스 값 쌍을 반복하는 열거자을 반환합니다.
        /// </summary>
        /// <returns>디바이스 변수/디바이스 값 쌍을 반복하는 열거자</returns>
        public IEnumerator<KeyValuePair<DeviceVariable, DeviceValue>> GetEnumerator() => deviceValueList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 프레임 데이터 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        protected override void OnCreateFrameData(List<byte> byteList)
        {
            if (Request is ICnetContinuousAccessRequest)
            {
                switch (dataType.Value)
                {
                    case DataType.Bit:
                        byteList.AddRange(ToAsciiBytes(deviceValueList.Count));
                        foreach (var item in deviceValueList)
                            byteList.AddRange(ToAsciiBytes(item.Value.BitValue ? 1 : 0));
                        break;
                    case DataType.Byte:
                        byteList.AddRange(ToAsciiBytes(deviceValueList.Count));
                        foreach (var item in deviceValueList)
                            byteList.AddRange(ToAsciiBytes(item.Value.ByteValue));
                        break;
                    case DataType.Word:
                        byteList.AddRange(ToAsciiBytes(2 * deviceValueList.Count));
                        foreach (var item in deviceValueList)
                            byteList.AddRange(ToAsciiBytes(item.Value.WordValue, 4));
                        break;
                    case DataType.DoubleWord:
                        byteList.AddRange(ToAsciiBytes(4 * deviceValueList.Count));
                        foreach (var item in deviceValueList)
                            byteList.AddRange(ToAsciiBytes(item.Value.DoubleWordValue, 8));
                        break;
                    case DataType.LongWord:
                        byteList.AddRange(ToAsciiBytes(8 * deviceValueList.Count));
                        foreach (var item in deviceValueList)
                            byteList.AddRange(ToAsciiBytes(item.Value.LongWordValue, 16));
                        break;
                }
            }
            else
            {
                byteList.AddRange(ToAsciiBytes(deviceValueList.Count));
                foreach (var item in deviceValueList)
                {
                    switch (item.Key.DataType)
                    {
                        case DataType.Bit:
                            byteList.AddRange(ToAsciiBytes(1));
                            byteList.AddRange(ToAsciiBytes(item.Value.BitValue ? 1 : 0));
                            break;
                        case DataType.Byte:
                            byteList.AddRange(ToAsciiBytes(1));
                            byteList.AddRange(ToAsciiBytes(item.Value.ByteValue));
                            break;
                        case DataType.Word:
                            byteList.AddRange(ToAsciiBytes(2));
                            byteList.AddRange(ToAsciiBytes(item.Value.WordValue, 4));
                            break;
                        case DataType.DoubleWord:
                            byteList.AddRange(ToAsciiBytes(4));
                            byteList.AddRange(ToAsciiBytes(item.Value.DoubleWordValue, 8));
                            break;
                        case DataType.LongWord:
                            byteList.AddRange(ToAsciiBytes(8));
                            byteList.AddRange(ToAsciiBytes(item.Value.LongWordValue, 16));
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 오류 응답
    /// </summary>
    public class CnetNAKResponse : CnetResponse
    {
        internal CnetNAKResponse(ushort nakCode, CnetRequest request) : base(request)
        {
            NAKCodeValue = nakCode;
            if (Enum.IsDefined(typeof(CnetNAKCode), nakCode))
                NAKCode = (CnetNAKCode)nakCode;
        }

        internal CnetNAKResponse(CnetNAKCode nakCode, CnetRequest request) : base(request)
        {
            NAKCode = nakCode;
            NAKCodeValue = (ushort)nakCode;
        }

        internal CnetNAKResponse(CnetNAKCode nakCode, byte stationNumber, CnetCommand command, ushort commandType, bool useBCC) 
            : base(new CnetRequestError(stationNumber, command, commandType, useBCC))
        {
            NAKCode = nakCode;
            NAKCodeValue = (ushort)nakCode;
        }

        /// <summary>
        /// 프레임 시작 헤더
        /// </summary>
        public override byte Header { get => NAK; }

        /// <summary>
        /// 오류 코드
        /// </summary>
        public CnetNAKCode NAKCode { get; } = CnetNAKCode.Unknown;

        /// <summary>
        /// 오류 코드 원본 값
        /// </summary>
        public ushort NAKCodeValue { get; }

        /// <summary>
        /// 프레임 데이터 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        protected override void OnCreateFrameData(List<byte> byteList)
        {
            byteList.AddRange(ToAsciiBytes(NAKCodeValue, 4));
        }

        class CnetRequestError : CnetRequest
        {
            public CnetRequestError(byte stationNumber, CnetCommand command, ushort commandType, bool useBCC) : base(stationNumber, command, useBCC)
            {
                CommandType = commandType;
            }

            /// <summary>
            /// 요청 메시지 복제
            /// </summary>
            /// <returns>복제된 요청 메시지</returns>
            public override object Clone() => new CnetRequestError(StationNumber, Command, CommandType, UseBCC);

            public ushort CommandType { get; }

            /// <summary>
            /// 프레임 데이터 생성
            /// </summary>
            /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
            protected override void OnCreateFrameData(List<byte> byteList)
            {
                byteList.Add((byte)(CommandType >> 8));
                byteList.Add((byte)(CommandType & 0xFF));
            }
        }
    }
}
