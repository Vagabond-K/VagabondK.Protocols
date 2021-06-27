using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VagabondK.Protocols.LSElectric.Cnet
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 요청 메시지
    /// </summary>
    public abstract class CnetRequest : CnetMessage, IRequest<CnetCommErrorCode>, ICloneable
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="command">커맨드</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        protected CnetRequest(byte stationNumber, CnetCommand command, bool useBCC)
        {
            this.stationNumber = stationNumber;
            this.useBCC = useBCC;
            Command = command;
        }

        /// <summary>
        /// 요청 메시지 복제
        /// </summary>
        /// <returns>복제된 요청 메시지</returns>
        public abstract object Clone();

        private byte stationNumber;
        private bool useBCC;


        /// <summary>
        /// 프레임 시작 헤더
        /// </summary>
        public override byte Header { get => ENQ; }

        /// <summary>
        /// 국번
        /// </summary>
        public byte StationNumber { get => stationNumber; set => SetProperty(ref stationNumber, value); }

        /// <summary>
        /// BCC 사용 여부
        /// </summary>
        public bool UseBCC { get => useBCC; set => SetProperty(ref useBCC, value); }

        /// <summary>
        /// 커맨드
        /// </summary>
        public CnetCommand Command { get; }

        /// <summary>
        /// 프레임 종료 테일
        /// </summary>
        public override byte Tail { get => EOT; }

        /// <summary>
        /// 프레임 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        protected override void OnCreateFrame(List<byte> byteList, out bool useBCC)
        {
            byteList.AddRange(ToAsciiBytes(StationNumber));
            byteList.Add(UseBCC ? (byte)((byte)Command + 0x20) : (byte)Command);
            OnCreateFrameData(byteList);
            useBCC = this.useBCC;
        }

        /// <summary>
        /// 프레임 데이터 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        protected abstract void OnCreateFrameData(List<byte> byteList);
    }

    /// <summary>
    /// 연속 디바이스 변수 액세스 요청 인터페이스
    /// </summary>
    public interface ICnetContinuousAccessRequest
    {
        /// <summary>
        /// 시작 디바이스 변수
        /// </summary>
        DeviceVariable StartDeviceVariable { get; }

        /// <summary>
        /// 연속 액세스 개수
        /// </summary>
        int Count { get; }
    }

    /// <summary>
    /// 연속 디바이스 변수 액세스 요청에 대한 확장 메서드 모음
    /// </summary>
    public static class CnetContinuousRequestExtensions
    {
        /// <summary>
        /// 시작 디바이스 변수로부터 연속으로 읽을 변수들을 목록으로 변환
        /// </summary>
        /// <param name="request">연속 디바이스 변수 액세스 요청</param>
        /// <returns>디바이스 변수 목록</returns>
        public static IEnumerable<DeviceVariable> ToDeviceVariables(this ICnetContinuousAccessRequest request)
        {
            var deviceVariable = request.StartDeviceVariable;
            for (int i = 0; i < request.Count; i++)
            {
                yield return deviceVariable;
                deviceVariable = deviceVariable.Increase();
            }
        }
    }

    /// <summary>
    /// 커맨드 타입을 포함하는 요청, 커맨드 타입은 개별 디바이스 변수 액세스와 연속 디바이스 변수 액세스가 있음.
    /// </summary>
    public abstract class CnetIncludeCommandTypeRequest : CnetRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="command">커맨드</param>
        /// <param name="commandType">커맨드 타입</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        protected CnetIncludeCommandTypeRequest(byte stationNumber, CnetCommand command, CnetCommandType commandType, bool useBCC)
            : base(stationNumber, command, useBCC)
        {
            CommandType = commandType;
        }

        /// <summary>
        /// 커맨드 타입
        /// </summary>
        public CnetCommandType CommandType { get; }

        /// <summary>
        /// 프레임 데이터 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        protected override void OnCreateFrameData(List<byte> byteList)
        {
            byteList.Add((byte)((int)CommandType >> 8));
            byteList.Add((byte)((int)CommandType & 0xFF));
        }
    }

    /// <summary>
    /// 디바이스 읽기 요청
    /// </summary>
    public abstract class CnetReadRequest : CnetIncludeCommandTypeRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="commandType">커맨드</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        protected CnetReadRequest(byte stationNumber, CnetCommandType commandType, bool useBCC)
            : base(stationNumber, CnetCommand.Read, commandType, useBCC) { }
    }

    /// <summary>
    /// 개별 디바이스 변수 읽기 요청
    /// </summary>
    public class CnetReadIndividualRequest : CnetReadRequest, IList<DeviceVariable>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        public CnetReadIndividualRequest(byte stationNumber) : this(stationNumber, null, true) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        public CnetReadIndividualRequest(byte stationNumber, bool useBCC) : this(stationNumber, null, useBCC) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="deviceVariables">디바이스 변수 목록</param>
        public CnetReadIndividualRequest(byte stationNumber, IEnumerable<DeviceVariable> deviceVariables) : this(stationNumber, deviceVariables, true) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="deviceVariables">디바이스 변수 목록</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        public CnetReadIndividualRequest(byte stationNumber, IEnumerable<DeviceVariable> deviceVariables, bool useBCC)
            : base(stationNumber, CnetCommandType.Individual, useBCC)
        {
            if (deviceVariables == null)
                this.deviceVariables = new List<DeviceVariable>();
            else
                this.deviceVariables = new List<DeviceVariable>(deviceVariables);
        }

        /// <summary>
        /// 요청 메시지 복제
        /// </summary>
        /// <returns>복제된 요청 메시지</returns>
        public override object Clone() => new CnetReadIndividualRequest(StationNumber, deviceVariables, UseBCC);

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
        /// 프레임 데이터 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        protected override void OnCreateFrameData(List<byte> byteList)
        {
            base.OnCreateFrameData(byteList);

            byteList.AddRange(ToAsciiBytes(Count));

            foreach (var deviceVariable in this)
            {
                var deviceVariableBytes = deviceVariable.ToBytes();
                byteList.AddRange(ToAsciiBytes(deviceVariableBytes.Length));
                byteList.AddRange(deviceVariableBytes);
            }
        }
    }

    /// <summary>
    /// 연속 디바이스 변수 읽기 요청
    /// </summary>
    public class CnetReadContinuousRequest : CnetReadRequest, ICnetContinuousAccessRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="count">읽을 개수</param>
        public CnetReadContinuousRequest(byte stationNumber, DeviceVariable startDeviceVariable, int count) : this(stationNumber, startDeviceVariable, count, true) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="count">읽을 개수</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        public CnetReadContinuousRequest(byte stationNumber, DeviceVariable startDeviceVariable, int count, bool useBCC)
            : base(stationNumber, CnetCommandType.Continuous, useBCC)
        {
            this.startDeviceVariable = startDeviceVariable;
            this.count = count;
        }

        /// <summary>
        /// 요청 메시지 복제
        /// </summary>
        /// <returns>복제된 요청 메시지</returns>
        public override object Clone() => new CnetReadContinuousRequest(StationNumber, startDeviceVariable, count, UseBCC);

        private DeviceVariable startDeviceVariable;
        private int count;

        /// <summary>
        /// 시작 디바이스 변수
        /// </summary>
        public DeviceVariable StartDeviceVariable { get => startDeviceVariable; set => SetProperty(ref startDeviceVariable, value); }

        /// <summary>
        /// 읽을 개수
        /// </summary>
        public int Count { get => count; set => SetProperty(ref count, value); }

        /// <summary>
        /// 프레임 데이터 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        protected override void OnCreateFrameData(List<byte> byteList)
        {
            base.OnCreateFrameData(byteList);

            var deviceVariableBytes = startDeviceVariable.ToBytes();
            byteList.AddRange(ToAsciiBytes(deviceVariableBytes.Length));
            byteList.AddRange(deviceVariableBytes);
            byteList.AddRange(ToAsciiBytes(count));
        }
    }



    /// <summary>
    /// 디바이스 쓰기 요청
    /// </summary>
    public abstract class CnetWriteRequest : CnetIncludeCommandTypeRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="commandType">커맨드 타입</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        protected CnetWriteRequest(byte stationNumber, CnetCommandType commandType, bool useBCC)
            : base(stationNumber, CnetCommand.Write, commandType, useBCC)
        {
        }
    }

    /// <summary>
    /// 개별 디바이스 변수 쓰기 요청
    /// </summary>
    public class CnetWriteIndividualRequest : CnetWriteRequest, IDictionary<DeviceVariable, DeviceValue>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        public CnetWriteIndividualRequest(byte stationNumber) : this(stationNumber, null, true) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        public CnetWriteIndividualRequest(byte stationNumber, bool useBCC) : this(stationNumber, null, useBCC) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="values">디바이스 변수에 쓸 값들</param>
        public CnetWriteIndividualRequest(byte stationNumber, IEnumerable<KeyValuePair<DeviceVariable, DeviceValue>> values) : this(stationNumber, values, true) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="values">디바이스 변수에 쓸 값들</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        public CnetWriteIndividualRequest(byte stationNumber, IEnumerable<KeyValuePair<DeviceVariable, DeviceValue>> values, bool useBCC)
            : base(stationNumber, CnetCommandType.Individual, useBCC)
        {
            if (values != null)
                foreach (var value in values)
                    valueDictionary[value.Key] = value.Value;
        }

        /// <summary>
        /// 요청 메시지 복제
        /// </summary>
        /// <returns>복제된 요청 메시지</returns>
        public override object Clone() => new CnetWriteIndividualRequest(StationNumber, valueDictionary, UseBCC);

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
        /// 프레임 데이터 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        protected override void OnCreateFrameData(List<byte> byteList)
        {
            base.OnCreateFrameData(byteList);

            byteList.AddRange(ToAsciiBytes(Count));

            foreach (var deviceValuePair in this)
            {
                var deviceVariable = deviceValuePair.Key;
                var deviceVariableBytes = deviceVariable.ToBytes();
                byteList.AddRange(ToAsciiBytes(deviceVariableBytes.Length));
                byteList.AddRange(deviceVariableBytes);

                switch (deviceVariable.DataType)
                {
                    case DataType.Bit:
                        byteList.AddRange(ToAsciiBytes(deviceValuePair.Value.BitValue ? 1 : 0));
                        break;
                    case DataType.Byte:
                        byteList.AddRange(ToAsciiBytes(deviceValuePair.Value.ByteValue));
                        break;
                    case DataType.Word:
                        byteList.AddRange(ToAsciiBytes(deviceValuePair.Value.WordValue, 4));
                        break;
                    case DataType.DoubleWord:
                        byteList.AddRange(ToAsciiBytes(deviceValuePair.Value.DoubleWordValue, 8));
                        break;
                    case DataType.LongWord:
                        byteList.AddRange(ToAsciiBytes(deviceValuePair.Value.LongWordValue, 16));
                        break;
                }
            }
        }

    }

    /// <summary>
    /// 연속 디바이스 변수 쓰기 요청
    /// </summary>
    public class CnetWriteContinuousRequest : CnetWriteRequest, IList<DeviceValue>, ICnetContinuousAccessRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        public CnetWriteContinuousRequest(byte stationNumber, DeviceVariable startDeviceVariable) : this(stationNumber, startDeviceVariable, null, true) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        public CnetWriteContinuousRequest(byte stationNumber, DeviceVariable startDeviceVariable, bool useBCC) : this(stationNumber, startDeviceVariable, null, useBCC) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="deviceValues">쓰기 요청할 디바이스 값들</param>
        public CnetWriteContinuousRequest(byte stationNumber, DeviceVariable startDeviceVariable, IEnumerable<DeviceValue> deviceValues) : this(stationNumber, startDeviceVariable, deviceValues, true) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="deviceValues">쓰기 요청할 디바이스 값들</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        public CnetWriteContinuousRequest(byte stationNumber, DeviceVariable startDeviceVariable, IEnumerable<DeviceValue> deviceValues, bool useBCC)
            : base(stationNumber, CnetCommandType.Continuous, useBCC)
        {
            this.startDeviceVariable = startDeviceVariable;

            if (deviceValues == null)
                this.deviceValues = new List<DeviceValue>();
            else
                this.deviceValues = new List<DeviceValue>(deviceValues);
        }

        /// <summary>
        /// 요청 메시지 복제
        /// </summary>
        /// <returns>복제된 요청 메시지</returns>
        public override object Clone() => new CnetWriteContinuousRequest(StationNumber, startDeviceVariable, deviceValues, UseBCC);

        private readonly List<DeviceValue> deviceValues;

        private DeviceVariable startDeviceVariable;

        /// <summary>
        /// 시작 디바이스 변수
        /// </summary>
        public DeviceVariable StartDeviceVariable { get => startDeviceVariable; set => SetProperty(ref startDeviceVariable, value); }

        /// <summary>
        /// 쓰기 요청할 디바이스 값 개수
        /// </summary>
        public int Count => deviceValues.Count;

        /// <summary>
        /// 디바이스 값 컬렉션이 읽기 전용인지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool IsReadOnly => ((ICollection<DeviceValue>)deviceValues).IsReadOnly;

        /// <summary>
        /// 디바이스 값이 존재하는지 여부를 가져옵니다.
        /// </summary>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>디바이스 값 존재 여부</returns>
        public bool Contains(DeviceValue deviceValue) => deviceValues.Contains(deviceValue);

        /// <summary>
        /// 디바이스 값의 순서상 위치를 가져옵니다.
        /// </summary>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>디바이스 값의 순서상 위치</returns>
        public int IndexOf(DeviceValue deviceValue) => deviceValues.IndexOf(deviceValue);

        /// <summary>
        /// 디바이스 값을 반복하는 열거자를 반환합니다.
        /// </summary>
        /// <returns>디바이스 값을 반복하는 열거자</returns>
        public IEnumerator<DeviceValue> GetEnumerator() => deviceValues.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 대상 배열의 지정된 인덱스에서 시작하여 전체 디바이스 값들을 복사합니다.
        /// </summary>
        /// <param name="array">디바이스 값 배열</param>
        /// <param name="arrayIndex">시작 인덱스</param>
        public void CopyTo(DeviceValue[] array, int arrayIndex) => deviceValues.CopyTo(array, arrayIndex);


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
        public DeviceValue this[int index]
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
        public void Add(DeviceValue deviceValue)
        {
            deviceValues.Add(deviceValue);
            InvalidateFrameData();
        }

        /// <summary>
        /// 지정된 인덱스에 디바이스 값을 삽입합니다.
        /// </summary>
        /// <param name="index">인덱스</param>
        /// <param name="deviceValue">디바이스 값</param>
        public void Insert(int index, DeviceValue deviceValue)
        {
            deviceValues.Insert(index, deviceValue);
            InvalidateFrameData();
        }

        /// <summary>
        /// 맨 처음 발견되는 디바이스 값을 제거합니다.
        /// </summary>
        /// <param name="deviceValue">디바이스 값</param>
        /// <returns>제거 여부</returns>
        public bool Remove(DeviceValue deviceValue)
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
        /// 프레임 데이터 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        protected override void OnCreateFrameData(List<byte> byteList)
        {
            base.OnCreateFrameData(byteList);

            var deviceVariableBytes = startDeviceVariable.ToBytes();
            byteList.AddRange(ToAsciiBytes(deviceVariableBytes.Length));
            byteList.AddRange(deviceVariableBytes);
            byteList.AddRange(ToAsciiBytes(Count));

            switch (startDeviceVariable.DataType)
            {
                case DataType.Bit:
                    foreach (var deviceValue in this)
                        byteList.AddRange(ToAsciiBytes(deviceValue.BitValue ? 1 : 0));
                    break;
                case DataType.Byte:
                    foreach (var deviceValue in this)
                        byteList.AddRange(ToAsciiBytes(deviceValue.ByteValue));
                    break;
                case DataType.Word:
                    foreach (var deviceValue in this)
                        byteList.AddRange(ToAsciiBytes(deviceValue.WordValue, 4));
                    break;
                case DataType.DoubleWord:
                    foreach (var deviceValue in this)
                        byteList.AddRange(ToAsciiBytes(deviceValue.DoubleWordValue, 8));
                    break;
                case DataType.LongWord:
                    foreach (var deviceValue in this)
                        byteList.AddRange(ToAsciiBytes(deviceValue.LongWordValue, 16));
                    break;
            }
        }
    }






    /// <summary>
    /// 모니터 변수 등록 요청
    /// </summary>
    public abstract class CnetRegisterMonitorRequest : CnetIncludeCommandTypeRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="commandType">커맨드 타입</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        protected CnetRegisterMonitorRequest(byte stationNumber, byte monitorNumber, CnetCommandType commandType, bool useBCC)
            : base(stationNumber, CnetCommand.RegisterMonitor, commandType, useBCC)
        {
            this.monitorNumber = monitorNumber;
        }

        private byte monitorNumber;

        /// <summary>
        /// 모니터 번호
        /// </summary>
        public byte MonitorNumber { get => monitorNumber; set => SetProperty(ref monitorNumber, value); }

        /// <summary>
        /// 프레임 데이터 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        protected override void OnCreateFrameData(List<byte> byteList)
        {
            byteList.AddRange(ToAsciiBytes(MonitorNumber));
            byteList.Add(0x52);
            byteList.Add((byte)((int)CommandType >> 8));
            byteList.Add((byte)((int)CommandType & 0xFF));
        }
    }

    /// <summary>
    /// 개별 모니터 변수 등록 요청
    /// </summary>
    public class CnetRegisterMonitorIndividualRequest : CnetRegisterMonitorRequest, IList<DeviceVariable>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        public CnetRegisterMonitorIndividualRequest(byte stationNumber, byte monitorNumber) : this(stationNumber, monitorNumber, null, true) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        public CnetRegisterMonitorIndividualRequest(byte stationNumber, byte monitorNumber, bool useBCC) : this(stationNumber, monitorNumber, null, useBCC) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="deviceVariables">디바이스 변수 목록</param>
        public CnetRegisterMonitorIndividualRequest(byte stationNumber, byte monitorNumber, IEnumerable<DeviceVariable> deviceVariables) : this(stationNumber, monitorNumber, deviceVariables, true) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="deviceVariables">디바이스 변수 목록</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        public CnetRegisterMonitorIndividualRequest(byte stationNumber, byte monitorNumber, IEnumerable<DeviceVariable> deviceVariables, bool useBCC)
            : base(stationNumber, monitorNumber, CnetCommandType.Individual, useBCC)
        {
            if (deviceVariables == null)
                this.deviceVariables = new List<DeviceVariable>();
            else
                this.deviceVariables = new List<DeviceVariable>(deviceVariables);
        }

        /// <summary>
        /// 요청 메시지 복제
        /// </summary>
        /// <returns>복제된 요청 메시지</returns>
        public override object Clone() => new CnetRegisterMonitorIndividualRequest(StationNumber, MonitorNumber, deviceVariables, UseBCC);

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
        /// 프레임 데이터 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        protected override void OnCreateFrameData(List<byte> byteList)
        {
            base.OnCreateFrameData(byteList);

            byteList.AddRange(ToAsciiBytes(Count));

            foreach (var deviceVariable in this)
            {
                var deviceVariableBytes = deviceVariable.ToBytes();
                byteList.AddRange(ToAsciiBytes(deviceVariableBytes.Length));
                byteList.AddRange(deviceVariableBytes);
            }
        }
    }

    /// <summary>
    /// 연속 모니터 변수 등록 요청
    /// </summary>
    public class CnetRegisterMonitorContinuousRequest : CnetRegisterMonitorRequest, ICnetContinuousAccessRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="count">읽을 개수</param>
        public CnetRegisterMonitorContinuousRequest(byte stationNumber, byte monitorNumber, DeviceVariable startDeviceVariable, int count) : this(stationNumber, monitorNumber, startDeviceVariable, count, true) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="startDeviceVariable">시작 디바이스 변수</param>
        /// <param name="count">읽을 개수</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        public CnetRegisterMonitorContinuousRequest(byte stationNumber, byte monitorNumber, DeviceVariable startDeviceVariable, int count, bool useBCC)
            : base(stationNumber, monitorNumber, CnetCommandType.Continuous, useBCC)
        {
            this.startDeviceVariable = startDeviceVariable;
            this.count = count;
        }

        /// <summary>
        /// 요청 메시지 복제
        /// </summary>
        /// <returns>복제된 요청 메시지</returns>
        public override object Clone() => new CnetRegisterMonitorContinuousRequest(StationNumber, MonitorNumber, startDeviceVariable, count, UseBCC);

        private DeviceVariable startDeviceVariable;
        private int count;

        /// <summary>
        /// 시작 디바이스 변수
        /// </summary>
        public DeviceVariable StartDeviceVariable { get => startDeviceVariable; set => SetProperty(ref startDeviceVariable, value); }

        /// <summary>
        /// 읽을 개수
        /// </summary>
        public int Count { get => count; set => SetProperty(ref count, value); }

        /// <summary>
        /// 프레임 데이터 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        protected override void OnCreateFrameData(List<byte> byteList)
        {
            base.OnCreateFrameData(byteList);

            var deviceVariableBytes = startDeviceVariable.ToBytes();
            byteList.AddRange(ToAsciiBytes(deviceVariableBytes.Length));
            byteList.AddRange(deviceVariableBytes);
            byteList.AddRange(ToAsciiBytes(count));
        }
    }

    /// <summary>
    /// 모니터 실행 요청
    /// </summary>
    public class CnetExecuteMonitorRequest : CnetIncludeCommandTypeRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="commandType">커맨드 타입</param>
        public CnetExecuteMonitorRequest(byte stationNumber, byte monitorNumber, CnetCommandType commandType) : this(stationNumber, monitorNumber, commandType, true) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        /// <param name="monitorNumber">모니터 번호</param>
        /// <param name="commandType">커맨드 타입</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        public CnetExecuteMonitorRequest(byte stationNumber, byte monitorNumber, CnetCommandType commandType, bool useBCC)
            : base(stationNumber, CnetCommand.ExecuteMonitor, commandType, useBCC)
        {
            this.monitorNumber = monitorNumber;
        }

        /// <summary>
        /// 요청 메시지 복제
        /// </summary>
        /// <returns>복제된 요청 메시지</returns>
        public override object Clone() => new CnetExecuteMonitorRequest(StationNumber, MonitorNumber, CommandType, UseBCC);

        private byte monitorNumber;

        /// <summary>
        /// 모니터 번호
        /// </summary>
        public byte MonitorNumber { get => monitorNumber; set => SetProperty(ref monitorNumber, value); }

        /// <summary>
        /// 프레임 데이터 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        protected override void OnCreateFrameData(List<byte> byteList)
        {
            byteList.AddRange(ToAsciiBytes(MonitorNumber));
        }
    }

    class CnetExecuteMonitorIndividualRequest : CnetExecuteMonitorRequest, IReadOnlyList<DeviceVariable>
    {
        public CnetExecuteMonitorIndividualRequest(CnetRegisterMonitorIndividualRequest request) : this(request, true) { }

        public CnetExecuteMonitorIndividualRequest(CnetRegisterMonitorIndividualRequest request, bool useBCC)
            : base(request?.StationNumber ?? 0, request?.MonitorNumber ?? 0, CnetCommandType.Individual, useBCC)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            this.request = (CnetRegisterMonitorIndividualRequest)request.Clone();
        }

        /// <summary>
        /// 요청 메시지 복제
        /// </summary>
        /// <returns>복제된 요청 메시지</returns>
        public override object Clone() => new CnetExecuteMonitorIndividualRequest(request, UseBCC);

        private readonly CnetRegisterMonitorIndividualRequest request;

        int IReadOnlyCollection<DeviceVariable>.Count => request.Count;

        public DeviceVariable this[int index] => request[index];

        public IEnumerator<DeviceVariable> GetEnumerator() => request.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    class CnetExecuteMonitorContinuousRequest : CnetExecuteMonitorRequest, ICnetContinuousAccessRequest
    {
        public CnetExecuteMonitorContinuousRequest(CnetRegisterMonitorContinuousRequest request) : this(request, true) { }

        public CnetExecuteMonitorContinuousRequest(CnetRegisterMonitorContinuousRequest request, bool useBCC)
            : base(request?.StationNumber ?? 0, request?.MonitorNumber ?? 0, CnetCommandType.Continuous, useBCC)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            this.request = (CnetRegisterMonitorContinuousRequest)request.Clone();
        }

        /// <summary>
        /// 요청 메시지 복제
        /// </summary>
        /// <returns>복제된 요청 메시지</returns>
        public override object Clone() => new CnetExecuteMonitorContinuousRequest(request, UseBCC);

        private readonly CnetRegisterMonitorContinuousRequest request;

        public DeviceVariable StartDeviceVariable { get => request.StartDeviceVariable; }
        public int Count { get => request.Count; }
    }

}
