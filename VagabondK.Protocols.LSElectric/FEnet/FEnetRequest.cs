using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VagabondK.Protocols.LSElectric.FEnet
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) FEnet 프로토콜 요청 메시지
    /// </summary>
    public abstract class FEnetRequest : FEnetMessage, IRequest<FEnetCommErrorCode>, ICloneable
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="command">커맨드</param>
        /// <param name="dataType">커맨드 데이터 타입</param>
        protected FEnetRequest(FEnetCommand command, FEnetDataType dataType) : base(command, dataType) { }

        private bool? useHexBitIndex;

        /// <summary>
        /// 통신 메시지의 소스. 클라이언트(HMI): 0x33
        /// </summary>
        public override byte SourceOfFrame => 0x33;

        /// <summary>
        /// 블록 수
        /// </summary>
        public abstract ushort BlockCount { get; }

        /// <summary>
        /// 비트 변수의 인덱스를 16진수로 통신할지 여부를 결정합니다.
        /// P, M, L, K, F 이면서 Bit일 경우 16진수로 전송합니다.
        /// 그 외에는 인덱스가 .으로 나누어져있고 Bit일 경우 마지막 자리만 16진수로 전송합니다.
        /// 이 속성을 null로 설정하면 FEnetClient의 UseHexBitIndex 값을 따릅니다.
        /// XGB PLC에서 비트를 읽거나 쓸 때 엉뚱한 비트가 읽히거나 쓰인다면 true로 설정해서 테스트 해보시기 바랍니다.
        /// '라이스'님의 제보로 추가한 옵션입니다. 감사합니다.
        /// </summary>
        public bool? UseHexBitIndex { get => useHexBitIndex; set => SetProperty(ref useHexBitIndex, value); }

        /// <summary>
        /// 요청 메시지 복제
        /// </summary>
        /// <returns>복제된 요청 메시지</returns>
        public abstract object Clone();

        /// <summary>
        /// 데이터 프레임 생성
        /// </summary>
        /// <returns>데이터의 직렬화 된 바이트 열거</returns>
        protected override IEnumerable<byte> OnCreateDataFrame()
            => WordToLittleEndianBytes((ushort)Command).Concat(WordToLittleEndianBytes((ushort)DataType)).Concat(zero).Concat(WordToLittleEndianBytes(BlockCount));
    }

    /// <summary>
    /// 디바이스 읽기 요청
    /// </summary>
    public abstract class FEnetReadRequest : FEnetRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="dataType">커맨드 데이터 타입</param>
        protected FEnetReadRequest(FEnetDataType dataType) : base(FEnetCommand.Read, dataType) { }
    }

    /// <summary>
    /// 개별 디바이스 변수 읽기 요청
    /// </summary>
    public class FEnetReadIndividualRequest : FEnetReadRequest, IList<DeviceVariable>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="dataType">커맨드 데이터 타입</param>
        public FEnetReadIndividualRequest(DataType dataType) : this(dataType, null as IEnumerable<DeviceVariable>) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="dataType">커맨드 데이터 타입</param>
        /// <param name="deviceVariables">디바이스 변수 목록</param>
        public FEnetReadIndividualRequest(DataType dataType, IEnumerable<DeviceVariable> deviceVariables) : base(ToFEnetDataType(dataType))
        {
            if (deviceVariables == null)
                this.deviceVariables = new List<DeviceVariable>();
            else
                this.deviceVariables = new List<DeviceVariable>(deviceVariables);
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="dataType">커맨드 데이터 타입</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="moreDeviceVariables">추가 디바이스 변수 목록</param>
        public FEnetReadIndividualRequest(DataType dataType, DeviceVariable deviceVariable, params DeviceVariable[] moreDeviceVariables)
            : this(dataType, new DeviceVariable[] { deviceVariable }.Concat(moreDeviceVariables)) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="moreDeviceVariables">추가 디바이스 변수 목록</param>
        public FEnetReadIndividualRequest(DeviceVariable deviceVariable, params DeviceVariable[] moreDeviceVariables)
            : this(deviceVariable.DataType, new DeviceVariable[] { deviceVariable }.Concat(moreDeviceVariables)) { }

        /// <summary>
        /// 블록 수
        /// </summary>
        public override ushort BlockCount => (ushort)deviceVariables.Count;

        /// <summary>
        /// 요청 메시지 복제
        /// </summary>
        /// <returns>복제된 요청 메시지</returns>
        public override object Clone() => new FEnetReadIndividualRequest(ToDataType(DataType), deviceVariables) { InvokeID = InvokeID, UseHexBitIndex = UseHexBitIndex };

        private readonly List<DeviceVariable> deviceVariables;

        /// <summary>
        /// 디바이스 변수 개수
        /// </summary>
        public int Count => deviceVariables.Count;

        /// <summary>
        /// 디바이스 변수 컬렉션이 읽기 전용인지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool IsReadOnly => ((ICollection<DeviceVariable>)deviceVariables).IsReadOnly;

        /// <summary>
        /// 디바이스 변수가 존재하는지 여부를 가져옵니다.
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <returns>디바이스 변수 존재 여부</returns>
        public bool Contains(DeviceVariable deviceVariable) => deviceVariables.Contains(deviceVariable);

        /// <summary>
        /// 디바이스 변수의 순서상 위치를 가져옵니다.
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <returns>디바이스 변수의 순서상 위치</returns>
        public int IndexOf(DeviceVariable deviceVariable) => deviceVariables.IndexOf(deviceVariable);

        /// <summary>
        /// 디바이스 변수를 반복하는 열거자를 반환합니다.
        /// </summary>
        /// <returns>디바이스 변수를 반복하는 열거자</returns>
        public IEnumerator<DeviceVariable> GetEnumerator() => deviceVariables.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 대상 배열의 지정된 인덱스에서 시작하여 전체 디바이스 변수들을 복사합니다.
        /// </summary>
        /// <param name="array">디바이스 변수 배열</param>
        /// <param name="arrayIndex">시작 인덱스</param>
        public void CopyTo(DeviceVariable[] array, int arrayIndex) => deviceVariables.CopyTo(array, arrayIndex);

        /// <summary>
        /// 모든 디바이스 변수들을 제거합니다.
        /// </summary>
        public void Clear()
        {
            deviceVariables.Clear();
            InvalidateFrameData();
        }

        /// <summary>
        /// 디바이스 변수를 지정된 인덱스에 지정하거나 제거합니다.
        /// </summary>
        /// <param name="index">인덱스</param>
        /// <returns>디바이스 변수</returns>
        public DeviceVariable this[int index]
        {
            get => deviceVariables[index];
            set
            {
                deviceVariables[index] = value;
                InvalidateFrameData();
            }
        }

        /// <summary>
        /// 디바이스 변수를 추가합니다.
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        public void Add(DeviceVariable deviceVariable)
        {
            deviceVariables.Add(deviceVariable);
            InvalidateFrameData();
        }

        /// <summary>
        /// 지정된 인덱스에 디바이스 변수를 삽입합니다.
        /// </summary>
        /// <param name="index">인덱스</param>
        /// <param name="deviceVariable">디바이스 변수</param>
        public void Insert(int index, DeviceVariable deviceVariable)
        {
            deviceVariables.Insert(index, deviceVariable);
            InvalidateFrameData();
        }

        /// <summary>
        /// 맨 처음 발견되는 디바이스 변수를 제거합니다.
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <returns>제거 여부</returns>
        public bool Remove(DeviceVariable deviceVariable)
        {
            var result = deviceVariables.Remove(deviceVariable);
            if (result) InvalidateFrameData();
            return result;
        }

        /// <summary>
        /// 지정된 인덱스에 있는 디바이스 변수를 제거합니다.
        /// </summary>
        /// <param name="index">인덱스</param>
        public void RemoveAt(int index)
        {
            deviceVariables.RemoveAt(index);
            InvalidateFrameData();
        }

        /// <summary>
        /// 데이터 프레임 생성
        /// </summary>
        /// <returns>데이터의 직렬화 된 바이트 열거</returns>
        protected override IEnumerable<byte> OnCreateDataFrame()
        {
            return base.OnCreateDataFrame()
                .Concat(deviceVariables.SelectMany(deviceVariable =>
                {
                    var deviceVariableBytes = deviceVariable.ToBytes(UseHexBitIndex ?? false);
                    return WordToLittleEndianBytes((ushort)deviceVariableBytes.Length).Concat(deviceVariableBytes);
                }));
        }
    }

    /// <summary>
    /// 연속 디바이스 변수 읽기 요청
    /// </summary>
    public class FEnetReadContinuousRequest : FEnetReadRequest, IContinuousAccessRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="deviceType">읽기 요청할 디바이스 영역</param>
        /// <param name="index">읽기 요청 시작 디바이스 인덱스</param>
        /// <param name="count">읽을 개수</param>
        public FEnetReadContinuousRequest(DeviceType deviceType, uint index, int count) : base(FEnetDataType.Continuous)
        {
            startDeviceVariable = new DeviceVariable(deviceType, LSElectric.DataType.Byte, index);
            this.count = count;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="deviceVariable">연속 읽기 요청 시작 디바이스 변수, Bit 형식일 경우 주소와 개수는 8의 배수여야만 함.</param>
        /// <param name="count">읽을 개수</param>
        public FEnetReadContinuousRequest(DeviceVariable deviceVariable, int count) : base(FEnetDataType.Continuous)
        {
            if (deviceVariable.DataType == LSElectric.DataType.Unknown)
                throw new ArgumentException(nameof(deviceVariable));

            if (deviceVariable.DataType == LSElectric.DataType.Bit)
            {
                if (deviceVariable.Index % 8 != 0) throw new ArgumentException($"{nameof(deviceVariable)}.{nameof(deviceVariable.Index)}");
                if (count % 8 != 0) throw new ArgumentException(nameof(count));
            }

            startDeviceVariable = deviceVariable;
            this.count = count;
        }

        /// <summary>
        /// 블록 수
        /// </summary>
        public override ushort BlockCount => 1;

        /// <summary>
        /// 요청 메시지 복제
        /// </summary>
        /// <returns>복제된 요청 메시지</returns>
        public override object Clone() => new FEnetReadContinuousRequest(startDeviceVariable, count) { InvokeID = InvokeID, UseHexBitIndex = UseHexBitIndex };

        private DeviceVariable startDeviceVariable;
        private int count;

        /// <summary>
        /// 읽기 요청 시작 디바이스 변수
        /// </summary>
        public DeviceVariable StartDeviceVariable{ get => startDeviceVariable; set => SetProperty(ref startDeviceVariable, value); }


        /// <summary>
        /// 읽을 개수
        /// </summary>
        public int Count { get => count; set => SetProperty(ref count, value); }

        /// <summary>
        /// 데이터 프레임 생성
        /// </summary>
        /// <returns>데이터의 직렬화 된 바이트 열거</returns>
        protected override IEnumerable<byte> OnCreateDataFrame()
        {
            DeviceVariable deviceVariable;
            int count;
            switch (startDeviceVariable.DataType)
            {
                case LSElectric.DataType.Bit:
                    deviceVariable = new DeviceVariable(startDeviceVariable.DeviceType, LSElectric.DataType.Byte, startDeviceVariable.Index / 8);
                    count = this.count / 8;
                    break;
                case LSElectric.DataType.Byte:
                    deviceVariable = startDeviceVariable;
                    count = this.count;
                    break;
                case LSElectric.DataType.Word:
                    deviceVariable = new DeviceVariable(startDeviceVariable.DeviceType, LSElectric.DataType.Byte, startDeviceVariable.Index * 2);
                    count = this.count * 2;
                    break;
                case LSElectric.DataType.DoubleWord:
                    deviceVariable = new DeviceVariable(startDeviceVariable.DeviceType, LSElectric.DataType.Byte, startDeviceVariable.Index * 4);
                    count = this.count * 4;
                    break;
                case LSElectric.DataType.LongWord:
                    deviceVariable = new DeviceVariable(startDeviceVariable.DeviceType, LSElectric.DataType.Byte, startDeviceVariable.Index * 8);
                    count = this.count * 8;
                    break;
                default:
                    throw new ArgumentException(nameof(startDeviceVariable));
            }

            var deviceVariableBytes = deviceVariable.ToBytes(UseHexBitIndex ?? false);

            return base.OnCreateDataFrame()
                .Concat(WordToLittleEndianBytes((ushort)deviceVariableBytes.Length))
                .Concat(deviceVariableBytes)
                .Concat(WordToLittleEndianBytes((ushort)count));
        }
    }



    /// <summary>
    /// 디바이스 쓰기 요청
    /// </summary>
    public abstract class FEnetWriteRequest : FEnetRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="dataType">커맨드 데이터 타입</param>
        protected FEnetWriteRequest(FEnetDataType dataType) : base(FEnetCommand.Write, dataType) { }
    }

    /// <summary>
    /// 개별 디바이스 변수 쓰기 요청
    /// </summary>
    public class FEnetWriteIndividualRequest : FEnetWriteRequest, IDictionary<DeviceVariable, DeviceValue>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="dataType">커맨드 데이터 타입</param>
        public FEnetWriteIndividualRequest(DataType dataType) : this(dataType, null) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="dataType">커맨드 데이터 타입</param>
        /// <param name="values">디바이스 변수에 쓸 값들</param>
        public FEnetWriteIndividualRequest(DataType dataType, IEnumerable<KeyValuePair<DeviceVariable, DeviceValue>> values) : base(ToFEnetDataType(dataType))
        {
            if (values != null)
                foreach (var value in values)
                    valueDictionary[value.Key] = value.Value;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="dataType">커맨드 데이터 타입</param>
        /// <param name="valueTuple">디바이스 변수에 쓸 값</param>
        /// <param name="moreValueTuples">추가 디바이스 변수에 쓸 값들</param>
        public FEnetWriteIndividualRequest(DataType dataType, (DeviceVariable, DeviceValue) valueTuple, params (DeviceVariable, DeviceValue)[] moreValueTuples)
            : this(dataType, new (DeviceVariable, DeviceValue)[] { valueTuple }.Concat(moreValueTuples).Select(item => new KeyValuePair<DeviceVariable, DeviceValue>(item.Item1, item.Item2))) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="valueTuple">디바이스 변수에 쓸 값</param>
        /// <param name="moreValueTuples">추가 디바이스 변수에 쓸 값들</param>
        public FEnetWriteIndividualRequest((DeviceVariable, DeviceValue) valueTuple, params (DeviceVariable, DeviceValue)[] moreValueTuples)
            : this(valueTuple.Item1.DataType, new (DeviceVariable, DeviceValue)[] { valueTuple }.Concat(moreValueTuples).Select(item => new KeyValuePair<DeviceVariable, DeviceValue>(item.Item1, item.Item2))) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="deviceValue">디바이스 변수에 쓸 값</param>
        public FEnetWriteIndividualRequest(DeviceVariable deviceVariable, DeviceValue deviceValue)
            : this(deviceVariable.DataType, new KeyValuePair<DeviceVariable, DeviceValue>[] { new KeyValuePair<DeviceVariable, DeviceValue>(deviceVariable, deviceValue) }) { }

        /// <summary>
        /// 블록 수
        /// </summary>
        public override ushort BlockCount => (ushort)valueDictionary.Count;

        /// <summary>
        /// 요청 메시지 복제
        /// </summary>
        /// <returns>복제된 요청 메시지</returns>
        public override object Clone() => new FEnetWriteIndividualRequest(ToDataType(DataType), valueDictionary) { InvokeID = InvokeID, UseHexBitIndex = UseHexBitIndex };

        private readonly Dictionary<DeviceVariable, DeviceValue> valueDictionary = new Dictionary<DeviceVariable, DeviceValue>();

        /// <summary>
        /// 키로 사용되는 디바이스 변수들
        /// </summary>
        public ICollection<DeviceVariable> Keys => valueDictionary.Keys;

        /// <summary>
        /// 쓰기 요청할 디바이스 변수 값들
        /// </summary>
        public ICollection<DeviceValue> Values => valueDictionary.Values;

        /// <summary>
        /// 쓰기 요청할 디바이스 값 개수
        /// </summary>
        public int Count => valueDictionary.Count;

        /// <summary>
        /// 디바이스 변수/디바이스 값 쌍 컬렉션이 읽기 전용인지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool IsReadOnly => ((ICollection<KeyValuePair<DeviceVariable, DeviceValue>>)valueDictionary).IsReadOnly;

        /// <summary>
        /// 디바이스 변수/디바이스 값 쌍이 존재하는지 여부를 가져옵니다.
        /// </summary>
        /// <param name="item">디바이스 변수</param>
        /// <returns>디바이스 변수 존재 여부</returns>
        public bool Contains(KeyValuePair<DeviceVariable, DeviceValue> item) => ((ICollection<KeyValuePair<DeviceVariable, DeviceValue>>)valueDictionary).Contains(item);

        /// <summary>
        /// 지정한 디바이스 변수가 포함하는지 여부를 가져옵니다.
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <returns>디바이스 변수 포함 여부</returns>
        public bool ContainsKey(DeviceVariable deviceVariable) => valueDictionary.ContainsKey(deviceVariable);

        /// <summary>
        /// 대상 배열의 지정된 인덱스에서 시작하여 전체 항목들을 복사합니다.
        /// </summary>
        /// <param name="array">디바이스 변수/디바이스 값 쌍 배열</param>
        /// <param name="arrayIndex">시작 인덱스</param>
        public void CopyTo(KeyValuePair<DeviceVariable, DeviceValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<DeviceVariable, DeviceValue>>)valueDictionary).CopyTo(array, arrayIndex);

        /// <summary>
        /// 디바이스 변수/디바이스 값 쌍을 반복하는 열거자을 반환합니다.
        /// </summary>
        /// <returns>디바이스 변수/디바이스 값 쌍을 반복하는 열거자</returns>
        public IEnumerator<KeyValuePair<DeviceVariable, DeviceValue>> GetEnumerator() => valueDictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 지정한 디바이스 변수의 값을 가져옵니다.
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>디바이스 변수 포함 여부</returns>
        public bool TryGetValue(DeviceVariable deviceVariable, out DeviceValue deviceValue) => valueDictionary.TryGetValue(deviceVariable, out deviceValue);

        /// <summary>
        /// 모든 디바이스 변수 값들을 제거합니다.
        /// </summary>
        public void Clear()
        {
            valueDictionary.Clear();
            InvalidateFrameData();
        }

        /// <summary>
        /// 지정한 디바이스 변수의 값을 가져오거나 설정합니다.
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <returns>디바이스 값</returns>
        public DeviceValue this[DeviceVariable deviceVariable]
        {
            get => valueDictionary[deviceVariable];
            set
            {
                valueDictionary[deviceVariable] = value;
                InvalidateFrameData();
            }
        }

        /// <summary>
        /// 디바이스 변수 값을 추가합니다.
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="deviceValue">디바이스 값</param>
        public void Add(DeviceVariable deviceVariable, DeviceValue deviceValue)
        {
            valueDictionary.Add(deviceVariable, deviceValue);
            InvalidateFrameData();
        }

        /// <summary>
        /// 디바이스 변수 값을 추가합니다.
        /// </summary>
        /// <param name="item">디바이스 변수/디바이스 값 쌍</param>
        public void Add(KeyValuePair<DeviceVariable, DeviceValue> item)
        {
            ((ICollection<KeyValuePair<DeviceVariable, DeviceValue>>)valueDictionary).Add(item);
            InvalidateFrameData();
        }

        /// <summary>
        /// 디바이스 변수 및 값을 제거합니다.
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <returns>제거 여부</returns>
        public bool Remove(DeviceVariable deviceVariable)
        {
            var result = valueDictionary.Remove(deviceVariable);
            if (result) InvalidateFrameData();
            return result;
        }

        /// <summary>
        /// 디바이스 변수/디바이스 값 쌍을 제거합니다.
        /// </summary>
        /// <param name="item">디바이스 변수/디바이스 값 쌍</param>
        /// <returns>제거 여부</returns>
        public bool Remove(KeyValuePair<DeviceVariable, DeviceValue> item)
        {
            var result = ((ICollection<KeyValuePair<DeviceVariable, DeviceValue>>)valueDictionary).Remove(item);
            if (result) InvalidateFrameData();
            return result;
        }

        /// <summary>
        /// 데이터 프레임 생성
        /// </summary>
        /// <returns>데이터의 직렬화 된 바이트 열거</returns>
        protected override IEnumerable<byte> OnCreateDataFrame()
        {
            foreach (var b in base.OnCreateDataFrame()
                .Concat(valueDictionary.SelectMany(keyValuePair =>
                {
                    var deviceVariableBytes = keyValuePair.Key.ToBytes(UseHexBitIndex ?? false);
                    return WordToLittleEndianBytes((ushort)deviceVariableBytes.Length).Concat(deviceVariableBytes);
                })))
                yield return b;

            foreach (var keyValuePair in valueDictionary)
            {
                switch (keyValuePair.Key.DataType)
                {
                    case LSElectric.DataType.Bit:
                        foreach (var b in WordToLittleEndianBytes(1)) yield return b;
                        yield return (byte)(keyValuePair.Value.BitValue ? 1 : 0);
                        break;
                    case LSElectric.DataType.Byte:
                        foreach (var b in WordToLittleEndianBytes(1)) yield return b;
                        yield return keyValuePair.Value.ByteValue;
                        break;
                    case LSElectric.DataType.Word:
                        foreach (var b in WordToLittleEndianBytes(2)) yield return b;
                        foreach (var b in ValueToLittleEndianBytes(keyValuePair.Value.WordValue)) yield return b;
                        break;
                    case LSElectric.DataType.DoubleWord:
                        foreach (var b in WordToLittleEndianBytes(4)) yield return b;
                        foreach (var b in ValueToLittleEndianBytes(keyValuePair.Value.DoubleWordValue)) yield return b;
                        break;
                    case LSElectric.DataType.LongWord:
                        foreach (var b in WordToLittleEndianBytes(8)) yield return b;
                        foreach (var b in ValueToLittleEndianBytes(keyValuePair.Value.LongWordValue)) yield return b;
                        break;
                }
            }
        }
    }

    /// <summary>
    /// 연속 디바이스 변수 쓰기 요청
    /// </summary>
    public class FEnetWriteContinuousRequest : FEnetWriteRequest, IList<byte>, IContinuousAccessRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="deviceType">읽기 요청 시작 디바이스</param>
        /// <param name="index">읽기 요청 시작 디바이스 인덱스</param>
        public FEnetWriteContinuousRequest(DeviceType deviceType, uint index) : this(deviceType, index, null) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="deviceType">읽기 요청 시작 디바이스</param>
        /// <param name="index">읽기 요청 시작 디바이스 인덱스</param>
        /// <param name="deviceValues">쓰기 요청할 디바이스 값들</param>
        public FEnetWriteContinuousRequest(DeviceType deviceType, uint index, IEnumerable<byte> deviceValues) : base(FEnetDataType.Continuous)
        {
            startDeviceVariable = new DeviceVariable(deviceType, LSElectric.DataType.Byte, index);

            if (deviceValues == null)
                this.deviceValues = new List<byte>();
            else
                this.deviceValues = new List<byte>(deviceValues);
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="deviceType">읽기 요청 시작 디바이스</param>
        /// <param name="index">읽기 요청 시작 디바이스 인덱스</param>
        /// <param name="value">쓰기 요청할 바이트 값</param>
        /// <param name="moreValues">추가 쓰기 요청할 바이트 값들</param>
        public FEnetWriteContinuousRequest(DeviceType deviceType, uint index, byte value, params byte[] moreValues)
            : this(deviceType, index, new byte[] { value }.Concat(moreValues)) { }

        /// <summary>
        /// 블록 수
        /// </summary>
        public override ushort BlockCount => 1;

        /// <summary>
        /// 요청 메시지 복제
        /// </summary>
        /// <returns>복제된 요청 메시지</returns>
        public override object Clone() => new FEnetWriteContinuousRequest(startDeviceVariable.DeviceType, startDeviceVariable.Index, deviceValues) { InvokeID = InvokeID, UseHexBitIndex = UseHexBitIndex };

        private readonly List<byte> deviceValues;

        private DeviceVariable startDeviceVariable;

        /// <summary>
        /// 쓰기 요청 시작 디바이스 변수
        /// </summary>
        public DeviceVariable StartDeviceVariable { get => startDeviceVariable; }

        /// <summary>
        /// 쓰기 요청할 디바이스 영역
        /// </summary>
        public DeviceType StartDeviceType
        {
            get => startDeviceVariable.DeviceType;
            set
            {
                if (startDeviceVariable.DeviceType != value)
                {
                    startDeviceVariable = new DeviceVariable(value, startDeviceVariable.DataType, startDeviceVariable.Index, startDeviceVariable.SubIndices.ToArray());
                    InvalidateFrameData();
                }
            }
        }

        /// <summary>
        /// 쓰기 요청 시작 디바이스 인덱스
        /// </summary>
        public uint StartDeviceIndex
        {
            get => startDeviceVariable.Index;
            set
            {
                if (startDeviceVariable.Index != value)
                {
                    startDeviceVariable = new DeviceVariable(startDeviceVariable.DeviceType, startDeviceVariable.DataType, value, startDeviceVariable.SubIndices.ToArray());
                    InvalidateFrameData();
                }
            }
        }

        /// <summary>
        /// 쓰기 요청할 디바이스 값 개수
        /// </summary>
        public int Count => deviceValues.Count;

        /// <summary>
        /// 디바이스 값 컬렉션이 읽기 전용인지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool IsReadOnly => ((ICollection<byte>)deviceValues).IsReadOnly;

        /// <summary>
        /// 디바이스 값이 존재하는지 여부를 가져옵니다.
        /// </summary>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>디바이스 값 존재 여부</returns>
        public bool Contains(byte deviceValue) => deviceValues.Contains(deviceValue);

        /// <summary>
        /// 디바이스 값의 순서상 위치를 가져옵니다.
        /// </summary>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>디바이스 값의 순서상 위치</returns>
        public int IndexOf(byte deviceValue) => deviceValues.IndexOf(deviceValue);

        /// <summary>
        /// 디바이스 값을 반복하는 열거자를 반환합니다.
        /// </summary>
        /// <returns>디바이스 값을 반복하는 열거자</returns>
        public IEnumerator<byte> GetEnumerator() => deviceValues.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 대상 배열의 지정된 인덱스에서 시작하여 전체 디바이스 값들을 복사합니다.
        /// </summary>
        /// <param name="array">디바이스 값 배열</param>
        /// <param name="arrayIndex">시작 인덱스</param>
        public void CopyTo(byte[] array, int arrayIndex) => deviceValues.CopyTo(array, arrayIndex);


        /// <summary>
        /// 모든 디바이스 값들을 제거합니다.
        /// </summary>
        public void Clear()
        {
            deviceValues.Clear();
            InvalidateFrameData();
        }

        /// <summary>
        /// 디바이스 값을 지정된 인덱스에 지정하거나 제거합니다.
        /// </summary>
        /// <param name="index">인덱스</param>
        /// <returns>디바이스 값</returns>
        public byte this[int index]
        {
            get => deviceValues[index];
            set
            {
                deviceValues[index] = value;
                InvalidateFrameData();
            }
        }

        /// <summary>
        /// 디바이스 값을 추가합니다.
        /// </summary>
        /// <param name="deviceValue">디바이스 값</param>
        public void Add(byte deviceValue)
        {
            deviceValues.Add(deviceValue);
            InvalidateFrameData();
        }

        /// <summary>
        /// 지정된 인덱스에 디바이스 값을 삽입합니다.
        /// </summary>
        /// <param name="index">인덱스</param>
        /// <param name="deviceValue">디바이스 값</param>
        public void Insert(int index, byte deviceValue)
        {
            deviceValues.Insert(index, deviceValue);
            InvalidateFrameData();
        }

        /// <summary>
        /// 맨 처음 발견되는 디바이스 값을 제거합니다.
        /// </summary>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>제거 여부</returns>
        public bool Remove(byte deviceValue)
        {
            var result = deviceValues.Remove(deviceValue);
            if (result) InvalidateFrameData();
            return result;
        }

        /// <summary>
        /// 지정된 인덱스에 있는 디바이스 값을 제거합니다.
        /// </summary>
        /// <param name="index">인덱스</param>
        public void RemoveAt(int index)
        {
            deviceValues.RemoveAt(index);
            InvalidateFrameData();
        }

        /// <summary>
        /// 데이터 프레임 생성
        /// </summary>
        /// <returns>데이터의 직렬화 된 바이트 열거</returns>
        protected override IEnumerable<byte> OnCreateDataFrame()
        {
            var deviceVariableBytes = startDeviceVariable.ToBytes(UseHexBitIndex ?? false);

            foreach (var b in base.OnCreateDataFrame()
                .Concat(WordToLittleEndianBytes((ushort)deviceVariableBytes.Length))
                .Concat(deviceVariableBytes)
                .Concat(WordToLittleEndianBytes((ushort)(Count))))
                yield return b;

            foreach (var value in deviceValues)
            {
                yield return value;
            }
        }
    }


}
