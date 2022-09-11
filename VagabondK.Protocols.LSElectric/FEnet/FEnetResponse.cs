using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VagabondK.Protocols.LSElectric.FEnet
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) FEnet 프로토콜 응답 메시지
    /// </summary>
    public abstract class FEnetResponse : FEnetMessage, IResponse
    {
        internal FEnetResponse(FEnetRequest request, ushort plcInfo, byte ethernetModuleInfo) : base(request.Command, request.DataType)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            InvokeID = request.InvokeID;
            PlcInfo = plcInfo;
            EthernetModuleSlot = (byte)(ethernetModuleInfo & 0xF);
            EthernetModuleBase = (byte)(ethernetModuleInfo >> 4);
        }

        /// <summary>
        /// LS ELECTRIC FEnet 프로토콜 요청 메시지
        /// </summary>
        public FEnetRequest Request { get; private set; }

        /// <summary>
        /// 통신 메시지의 소스. 서버(PLC): 0x11
        /// </summary>
        public override byte SourceOfFrame => 0x11;

        /// <summary>
        /// PLC의 CPU 타입 및 상태 정보. 요청 메시지에서는 의미가 없으며, 자세한 내용은 매뉴얼 참조 바람.
        /// </summary>
        public ushort PlcInfo { get; }

        /// <summary>
        /// 이더넷 모듈의 슬롯(Slot) 번호. 요청 메시지에서는 의미가 없음.
        /// </summary>
        public byte EthernetModuleSlot { get; }
        /// <summary>
        /// 이더넷 모듈의 베이스(Base) 번호. 요청 메시지에서는 의미가 없음.
        /// </summary>
        public byte EthernetModuleBase { get; }


        /// <summary>
        /// 데이터 프레임 생성
        /// </summary>
        /// <returns>데이터의 직렬화 된 바이트 열거</returns>
        protected override IEnumerable<byte> OnCreateDataFrame()
            => WordToLittleEndianBytes((ushort)((int)Command + 1)).Concat(WordToLittleEndianBytes((ushort)DataType)).Concat(zero);
    }

    /// <summary>
    /// 정상 처리 응답 메시지
    /// </summary>
    public abstract class FEnetACKResponse : FEnetResponse
    {
        internal FEnetACKResponse(FEnetRequest request, ushort plcInfo, byte ethernetModuleInfo) : base(request, plcInfo, ethernetModuleInfo) { }
    }

    /// <summary>
    /// 개별 디바이스 변수 읽기 응답
    /// </summary>
    public class FEnetReadIndividualResponse : FEnetACKResponse, IReadOnlyDictionary<DeviceVariable, DeviceValue>
    {
        internal FEnetReadIndividualResponse(IEnumerable<DeviceValue> deviceValues, FEnetReadIndividualRequest request, ushort plcInfo, byte ethernetModuleInfo) : base(request, plcInfo, ethernetModuleInfo)
        {
            SetData(deviceValues, request);
        }

        private void SetData(IEnumerable<DeviceValue> deviceValues, IEnumerable<DeviceVariable> deviceVariables)
        {
            var valueArray = deviceValues.ToArray();
            var deviceVariableArray = deviceVariables.ToArray();

            if (valueArray.Length != deviceVariableArray.Length) throw new ArgumentOutOfRangeException(nameof(deviceValues));

            deviceValueList = valueArray.Zip(deviceVariableArray, (v, a) => new KeyValuePair<DeviceVariable, DeviceValue>(a, v)).ToList();

            foreach (var item in deviceValueList)
                deviceValueDictionary[item.Key] = item.Value;
        }


        internal List<KeyValuePair<DeviceVariable, DeviceValue>> deviceValueList;
        private readonly Dictionary<DeviceVariable, DeviceValue> deviceValueDictionary = new Dictionary<DeviceVariable, DeviceValue>();

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
        /// 데이터 프레임 생성
        /// </summary>
        /// <returns>데이터의 직렬화 된 바이트 열거</returns>
        protected override IEnumerable<byte> OnCreateDataFrame()
        {
            foreach (var b in base.OnCreateDataFrame()) yield return b;
            yield return 0x00;  //에러 상태
            yield return 0x00;  //없음

            foreach (var b in WordToLittleEndianBytes((ushort)deviceValueList.Count)) yield return b;

            foreach (var item in deviceValueList)
            {
                switch (item.Key.DataType)
                {
                    case LSElectric.DataType.Bit:
                        foreach (var b in one) yield return b;
                        yield return (byte)(item.Value.BitValue ? 1 : 0);
                        break;
                    case LSElectric.DataType.Byte:
                        foreach (var b in one) yield return b;
                        yield return item.Value.ByteValue;
                        break;
                    case LSElectric.DataType.Word:
                        foreach (var b in WordToLittleEndianBytes(2)) yield return b;
                        foreach (var b in ValueToLittleEndianBytes(item.Value.WordValue)) yield return b;
                        break;
                    case LSElectric.DataType.DoubleWord:
                        foreach (var b in WordToLittleEndianBytes(4)) yield return b;
                        foreach (var b in ValueToLittleEndianBytes(item.Value.DoubleWordValue)) yield return b;
                        break;
                    case LSElectric.DataType.LongWord:
                        foreach (var b in WordToLittleEndianBytes(8)) yield return b;
                        foreach (var b in ValueToLittleEndianBytes(item.Value.LongWordValue)) yield return b;
                        break;
                }
            }
        }
    }

    /// <summary>
    /// 연속 디바이스 변수 읽기 응답
    /// </summary>
    public class FEnetReadContinuousResponse : FEnetACKResponse, IReadOnlyList<byte>
    {
        internal FEnetReadContinuousResponse(IEnumerable<byte> bytes, FEnetReadContinuousRequest request, ushort plcInfo, byte ethernetModuleInfo) : base(request, plcInfo, ethernetModuleInfo) 
            => this.bytes = bytes.ToArray();

        private readonly byte[] bytes;

        /// <summary>
        /// 바이트 값 개수
        /// </summary>
        public int Count => bytes.Length;


        /// <summary>
        /// 지정한 인덱스의 바이트 값을 가져옵니다.
        /// </summary>
        /// <param name="index">인덱스</param>
        /// <returns>바이트 값</returns>
        public byte this[int index] => bytes[index];

        /// <summary>
        /// 바이트 값을 반복하는 열거자을 반환합니다.
        /// </summary>
        /// <returns>바이트 값을 반복하는 열거자</returns>
        public IEnumerator<byte> GetEnumerator() => bytes.AsEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 데이터 프레임 생성
        /// </summary>
        /// <returns>데이터의 직렬화 된 바이트 열거</returns>
        protected override IEnumerable<byte> OnCreateDataFrame()
        {
            foreach (var b in base.OnCreateDataFrame()) yield return b;
            yield return 0x00;  //에러 상태
            yield return 0x00;  //없음

            foreach (var b in one) yield return b;

            foreach (var b in WordToLittleEndianBytes((ushort)bytes.Length)) yield return b;
            foreach (var b in bytes)
                yield return b;
        }
    }

    /// <summary>
    /// 디바이스 변수 쓰기 응답
    /// </summary>
    public class FEnetWriteResponse : FEnetACKResponse
    {
        internal FEnetWriteResponse(FEnetWriteRequest request, ushort plcInfo, byte ethernetModuleInfo) : base(request, plcInfo, ethernetModuleInfo) { }

        /// <summary>
        /// 데이터 프레임 생성
        /// </summary>
        /// <returns>데이터의 직렬화 된 바이트 열거</returns>
        protected override IEnumerable<byte> OnCreateDataFrame()
        {
            foreach (var b in base.OnCreateDataFrame()) yield return b;
            yield return 0x00;
            yield return 0x00;
            foreach (var b in WordToLittleEndianBytes(Request.BlockCount)) yield return b;
        }
    }

    /// <summary>
    /// 오류 응답
    /// </summary>
    public class FEnetNAKResponse : FEnetResponse
    {
        internal FEnetNAKResponse(ushort nakCode, FEnetRequest request, ushort plcInfo, byte ethernetModuleInfo) : base(request, plcInfo, ethernetModuleInfo)
        {
            NAKCodeValue = nakCode;
            if (Enum.IsDefined(typeof(FEnetNAKCode), nakCode))
                NAKCode = (FEnetNAKCode)nakCode;
        }

        internal FEnetNAKResponse(FEnetNAKCode nakCode, FEnetRequest request, ushort plcInfo, byte ethernetModuleInfo) : base(request, plcInfo, ethernetModuleInfo)
        {
            NAKCode = nakCode;
            NAKCodeValue = (ushort)nakCode;
        }

        internal FEnetNAKResponse(FEnetNAKCode nakCode, FEnetCommand command, FEnetDataType dataType, ushort plcInfo, byte ethernetModuleInfo)
            : base(new FEnetRequestError(command, dataType), plcInfo, ethernetModuleInfo)
        {
            NAKCode = nakCode;
            NAKCodeValue = (ushort)nakCode;
        }

        /// <summary>
        /// 오류 코드
        /// </summary>
        public FEnetNAKCode NAKCode { get; } = FEnetNAKCode.Unknown;

        /// <summary>
        /// 오류 코드 원본 값
        /// </summary>
        public ushort NAKCodeValue { get; }

        /// <summary>
        /// 데이터 프레임 생성
        /// </summary>
        /// <returns>데이터의 직렬화 된 바이트 열거</returns>
        protected override IEnumerable<byte> OnCreateDataFrame()
        {
            foreach (var b in base.OnCreateDataFrame()) yield return b;
            yield return 0xff;
            yield return 0xff;
            foreach (var b in WordToLittleEndianBytes(NAKCodeValue)) yield return b;
        }

        class FEnetRequestError : FEnetRequest
        {
            public FEnetRequestError(FEnetCommand command, FEnetDataType dataType) : base(command, dataType) { }

            public override ushort BlockCount => 0;

            public override object Clone() => new FEnetRequestError(Command, DataType);
        }
    }

}
