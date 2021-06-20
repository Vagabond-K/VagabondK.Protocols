using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VagabondK.Protocols.LSElectric.Cnet
{
    /// <summary>
    /// LS ELECTRIC Cnet 프로토콜 요청 메시지
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
        /// 프레임 시작 코드
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
        /// 프레임 종료 코드
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

    public interface ICnetAddressBlockRequest
    {
        DeviceAddress DeviceAddress { get; }
        int Count { get; }
    }

    public abstract class CnetIncludeCommandTypeRequest : CnetRequest
    {
        protected CnetIncludeCommandTypeRequest(byte stationNumber, CnetCommand command, CnetCommandType commandType, bool useBCC)
            : base(stationNumber, command, useBCC)
        {
            CommandType = commandType;
        }

        /// <summary>
        /// 커맨드 타입
        /// </summary>
        public CnetCommandType CommandType { get; }

        protected override void OnCreateFrameData(List<byte> byteList)
        {
            byteList.Add((byte)((int)CommandType >> 8));
            byteList.Add((byte)((int)CommandType & 0xFF));
        }
    }

    public abstract class CnetReadRequest : CnetIncludeCommandTypeRequest
    {
        protected CnetReadRequest(byte stationNumber, CnetCommandType commandType, bool useBCC)
            : base(stationNumber, CnetCommand.Read, commandType, useBCC) { }
    }

    public class CnetReadEachAddressRequest : CnetReadRequest, IList<DeviceAddress>
    {
        public CnetReadEachAddressRequest(byte stationNumber) : this(stationNumber, null, true) { }
        public CnetReadEachAddressRequest(byte stationNumber, bool useBCC) : this(stationNumber, null, useBCC) { }
        public CnetReadEachAddressRequest(byte stationNumber, IEnumerable<DeviceAddress> addresses) : this(stationNumber, addresses, true) { }

        public CnetReadEachAddressRequest(byte stationNumber, IEnumerable<DeviceAddress> addresses, bool useBCC)
            : base(stationNumber, CnetCommandType.Each, useBCC)
        {
            if (addresses == null)
                deviceAddresses = new List<DeviceAddress>();
            else
                deviceAddresses = new List<DeviceAddress>(addresses);
        }

        public override object Clone() => new CnetReadEachAddressRequest(StationNumber, deviceAddresses, UseBCC);

        private readonly List<DeviceAddress> deviceAddresses;

        public int Count => deviceAddresses.Count;

        public bool IsReadOnly => ((ICollection<DeviceAddress>)deviceAddresses).IsReadOnly;

        public bool Contains(DeviceAddress item) => deviceAddresses.Contains(item);

        public int IndexOf(DeviceAddress item) => deviceAddresses.IndexOf(item);

        public IEnumerator<DeviceAddress> GetEnumerator() => deviceAddresses.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void CopyTo(DeviceAddress[] array, int arrayIndex) => deviceAddresses.CopyTo(array, arrayIndex);


        public void Clear()
        {
            deviceAddresses.Clear();
            InvalidateFrameData();
        }

        public DeviceAddress this[int index]
        {
            get => deviceAddresses[index];
            set
            {
                deviceAddresses[index] = value;
                InvalidateFrameData();
            }
        }

        public void Add(DeviceAddress item)
        {
            deviceAddresses.Add(item);
            InvalidateFrameData();
        }

        public void Insert(int index, DeviceAddress item)
        {
            deviceAddresses.Insert(index, item);
            InvalidateFrameData();
        }

        public bool Remove(DeviceAddress item)
        {
            var result = deviceAddresses.Remove(item);
            if (result) InvalidateFrameData();
            return result;
        }

        public void RemoveAt(int index)
        {
            deviceAddresses.RemoveAt(index);
            InvalidateFrameData();
        }

        protected override void OnCreateFrameData(List<byte> byteList)
        {
            base.OnCreateFrameData(byteList);

            byteList.AddRange(ToAsciiBytes(Count));

            foreach (var deviceAddress in this)
            {
                var deviceAddressBytes = deviceAddress.ToBytes();
                byteList.AddRange(ToAsciiBytes(deviceAddressBytes.Length));
                byteList.AddRange(deviceAddressBytes);
            }
        }
    }

    public class CnetReadAddressBlockRequest : CnetReadRequest, ICnetAddressBlockRequest
    {
        public CnetReadAddressBlockRequest(byte stationNumber, DeviceAddress address, int count) : this(stationNumber, address, count, true) { }

        public CnetReadAddressBlockRequest(byte stationNumber, DeviceAddress address, int count, bool useBCC)
            : base(stationNumber, CnetCommandType.Block, useBCC)
        {
            deviceAddress = address;
            this.count = count;
        }

        public override object Clone() => new CnetReadAddressBlockRequest(StationNumber, deviceAddress, count, UseBCC);

        private DeviceAddress deviceAddress;
        private int count;

        public DeviceAddress DeviceAddress { get => deviceAddress; set => SetProperty(ref deviceAddress, value); }
        public int Count { get => count; set => SetProperty(ref count, value); }

        protected override void OnCreateFrameData(List<byte> byteList)
        {
            base.OnCreateFrameData(byteList);

            var deviceAddressBytes = deviceAddress.ToBytes();
            byteList.AddRange(ToAsciiBytes(deviceAddressBytes.Length));
            byteList.AddRange(deviceAddressBytes);
            byteList.AddRange(ToAsciiBytes(count));
        }
    }




    public abstract class CnetWriteRequest : CnetIncludeCommandTypeRequest
    {
        protected CnetWriteRequest(byte stationNumber, CnetCommandType commandType, bool useBCC)
            : base(stationNumber, CnetCommand.Write, commandType, useBCC)
        {
        }
    }

    public class CnetWriteEachAddressRequest : CnetWriteRequest, IDictionary<DeviceAddress, DeviceValue>
    {
        public CnetWriteEachAddressRequest(byte stationNumber) : this(stationNumber, null, true) { }
        public CnetWriteEachAddressRequest(byte stationNumber, bool useBCC) : this(stationNumber, null, useBCC) { }
        public CnetWriteEachAddressRequest(byte stationNumber, IEnumerable<KeyValuePair<DeviceAddress, DeviceValue>> values) : this(stationNumber, values, true) { }

        public CnetWriteEachAddressRequest(byte stationNumber, IEnumerable<KeyValuePair<DeviceAddress, DeviceValue>> values, bool useBCC)
            : base(stationNumber, CnetCommandType.Each, useBCC)
        {
            if (values != null)
                foreach (var value in values)
                    valueDictionary[value.Key] = value.Value;
        }

        public override object Clone() => new CnetWriteEachAddressRequest(StationNumber, valueDictionary, UseBCC);

        private readonly Dictionary<DeviceAddress, DeviceValue> valueDictionary = new Dictionary<DeviceAddress, DeviceValue>();

        public ICollection<DeviceAddress> Keys => valueDictionary.Keys;

        public ICollection<DeviceValue> Values => valueDictionary.Values;

        public int Count => valueDictionary.Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<DeviceAddress, DeviceValue>>)valueDictionary).IsReadOnly;

        public bool Contains(KeyValuePair<DeviceAddress, DeviceValue> item) => ((ICollection<KeyValuePair<DeviceAddress, DeviceValue>>)valueDictionary).Contains(item);

        public bool ContainsKey(DeviceAddress key) => valueDictionary.ContainsKey(key);

        public void CopyTo(KeyValuePair<DeviceAddress, DeviceValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<DeviceAddress, DeviceValue>>)valueDictionary).CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<DeviceAddress, DeviceValue>> GetEnumerator() => valueDictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool TryGetValue(DeviceAddress key, out DeviceValue value) => valueDictionary.TryGetValue(key, out value);

        public void Clear()
        {
            valueDictionary.Clear();
            InvalidateFrameData();
        }

        public DeviceValue this[DeviceAddress key]
        {
            get => valueDictionary[key];
            set
            {
                valueDictionary[key] = value;
                InvalidateFrameData();
            }
        }

        public void Add(DeviceAddress key, DeviceValue value)
        {
            valueDictionary.Add(key, value);
            InvalidateFrameData();
        }

        public void Add(KeyValuePair<DeviceAddress, DeviceValue> item)
        {
            ((ICollection<KeyValuePair<DeviceAddress, DeviceValue>>)valueDictionary).Add(item);
            InvalidateFrameData();
        }

        public bool Remove(DeviceAddress key)
        {
            var result = valueDictionary.Remove(key);
            if (result) InvalidateFrameData();
            return result;
        }

        public bool Remove(KeyValuePair<DeviceAddress, DeviceValue> item)
        {
            var result = ((ICollection<KeyValuePair<DeviceAddress, DeviceValue>>)valueDictionary).Remove(item);
            if (result) InvalidateFrameData();
            return result;
        }


        protected override void OnCreateFrameData(List<byte> byteList)
        {
            base.OnCreateFrameData(byteList);

            byteList.AddRange(ToAsciiBytes(Count));

            foreach (var deviceValuePair in this)
            {
                var deviceAddress = deviceValuePair.Key;
                var deviceAddressBytes = deviceAddress.ToBytes();
                byteList.AddRange(ToAsciiBytes(deviceAddressBytes.Length));
                byteList.AddRange(deviceAddressBytes);

                switch (deviceAddress.DataType)
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

    public class CnetWriteAddressBlockRequest : CnetWriteRequest, IList<DeviceValue>, ICnetAddressBlockRequest
    {
        public CnetWriteAddressBlockRequest(byte stationNumber, DeviceAddress address) : this(stationNumber, address, null, true) { }
        public CnetWriteAddressBlockRequest(byte stationNumber, DeviceAddress address, bool useBCC) : this(stationNumber, address, null, useBCC) { }
        public CnetWriteAddressBlockRequest(byte stationNumber, DeviceAddress address, IEnumerable<DeviceValue> values) : this(stationNumber, address, values, true) { }

        public CnetWriteAddressBlockRequest(byte stationNumber, DeviceAddress address, IEnumerable<DeviceValue> values, bool useBCC)
            : base(stationNumber, CnetCommandType.Block, useBCC)
        {
            deviceAddress = address;

            if (values == null)
                deviceValues = new List<DeviceValue>();
            else
                deviceValues = new List<DeviceValue>(values);
        }

        public override object Clone() => new CnetWriteAddressBlockRequest(StationNumber, deviceAddress, deviceValues, UseBCC);

        private readonly List<DeviceValue> deviceValues;

        private DeviceAddress deviceAddress;

        public DeviceAddress DeviceAddress { get => deviceAddress; set => SetProperty(ref deviceAddress, value); }

        public int Count => deviceValues.Count;

        public bool IsReadOnly => ((ICollection<DeviceValue>)deviceValues).IsReadOnly;

        public bool Contains(DeviceValue item) => deviceValues.Contains(item);

        public int IndexOf(DeviceValue item) => deviceValues.IndexOf(item);

        public IEnumerator<DeviceValue> GetEnumerator() => deviceValues.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void CopyTo(DeviceValue[] array, int arrayIndex) => deviceValues.CopyTo(array, arrayIndex);


        public void Clear()
        {
            deviceValues.Clear();
            InvalidateFrameData();
        }

        public DeviceValue this[int index]
        {
            get => deviceValues[index];
            set
            {
                deviceValues[index] = value;
                InvalidateFrameData();
            }
        }

        public void Add(DeviceValue item)
        {
            deviceValues.Add(item);
            InvalidateFrameData();
        }

        public void Insert(int index, DeviceValue item)
        {
            deviceValues.Insert(index, item);
            InvalidateFrameData();
        }

        public bool Remove(DeviceValue item)
        {
            var result = deviceValues.Remove(item);
            if (result) InvalidateFrameData();
            return result;
        }

        public void RemoveAt(int index)
        {
            deviceValues.RemoveAt(index);
            InvalidateFrameData();
        }


        protected override void OnCreateFrameData(List<byte> byteList)
        {
            base.OnCreateFrameData(byteList);

            var deviceAddressBytes = deviceAddress.ToBytes();
            byteList.AddRange(ToAsciiBytes(deviceAddressBytes.Length));
            byteList.AddRange(deviceAddressBytes);
            byteList.AddRange(ToAsciiBytes(Count));

            switch (deviceAddress.DataType)
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







    public abstract class CnetRegisterMonitorRequest : CnetIncludeCommandTypeRequest
    {
        protected CnetRegisterMonitorRequest(byte stationNumber, byte monitorNumber, CnetCommandType commandType, bool useBCC)
            : base(stationNumber, CnetCommand.RegisterMonitor, commandType, useBCC)
        {
            this.monitorNumber = monitorNumber;
        }

        private byte monitorNumber;

        public byte MonitorNumber { get => monitorNumber; set => SetProperty(ref monitorNumber, value); }

        protected override void OnCreateFrameData(List<byte> byteList)
        {
            byteList.AddRange(ToAsciiBytes(MonitorNumber));
            byteList.Add(0x52);
            byteList.Add((byte)((int)CommandType >> 8));
            byteList.Add((byte)((int)CommandType & 0xFF));
        }
    }

    public class CnetRegisterMonitorEachAddressRequest : CnetRegisterMonitorRequest, IList<DeviceAddress>
    {
        public CnetRegisterMonitorEachAddressRequest(byte stationNumber, byte monitorNumber) : this(stationNumber, monitorNumber, null, true) { }
        public CnetRegisterMonitorEachAddressRequest(byte stationNumber, byte monitorNumber, bool useBCC) : this(stationNumber, monitorNumber, null, useBCC) { }
        public CnetRegisterMonitorEachAddressRequest(byte stationNumber, byte monitorNumber, IEnumerable<DeviceAddress> addresses) : this(stationNumber, monitorNumber, addresses, true) { }

        public CnetRegisterMonitorEachAddressRequest(byte stationNumber, byte monitorNumber, IEnumerable<DeviceAddress> addresses, bool useBCC)
            : base(stationNumber, monitorNumber, CnetCommandType.Each, useBCC)
        {
            if (addresses == null)
                deviceAddresses = new List<DeviceAddress>();
            else
                deviceAddresses = new List<DeviceAddress>(addresses);
        }

        public override object Clone() => new CnetRegisterMonitorEachAddressRequest(StationNumber, MonitorNumber, deviceAddresses, UseBCC);

        private readonly List<DeviceAddress> deviceAddresses;

        public int Count => deviceAddresses.Count;

        public bool IsReadOnly => ((ICollection<DeviceAddress>)deviceAddresses).IsReadOnly;

        public bool Contains(DeviceAddress item) => deviceAddresses.Contains(item);

        public int IndexOf(DeviceAddress item) => deviceAddresses.IndexOf(item);

        public IEnumerator<DeviceAddress> GetEnumerator() => deviceAddresses.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void CopyTo(DeviceAddress[] array, int arrayIndex) => deviceAddresses.CopyTo(array, arrayIndex);


        public void Clear()
        {
            deviceAddresses.Clear();
            InvalidateFrameData();
        }

        public DeviceAddress this[int index]
        {
            get => deviceAddresses[index];
            set
            {
                deviceAddresses[index] = value;
                InvalidateFrameData();
            }
        }

        public void Add(DeviceAddress item)
        {
            deviceAddresses.Add(item);
            InvalidateFrameData();
        }

        public void Insert(int index, DeviceAddress item)
        {
            deviceAddresses.Insert(index, item);
            InvalidateFrameData();
        }

        public bool Remove(DeviceAddress item)
        {
            var result = deviceAddresses.Remove(item);
            if (result) InvalidateFrameData();
            return result;
        }

        public void RemoveAt(int index)
        {
            deviceAddresses.RemoveAt(index);
            InvalidateFrameData();
        }

        protected override void OnCreateFrameData(List<byte> byteList)
        {
            base.OnCreateFrameData(byteList);

            byteList.AddRange(ToAsciiBytes(Count));

            foreach (var deviceAddress in this)
            {
                var deviceAddressBytes = deviceAddress.ToBytes();
                byteList.AddRange(ToAsciiBytes(deviceAddressBytes.Length));
                byteList.AddRange(deviceAddressBytes);
            }
        }

        public CnetExecuteMonitorEachAddressRequest CreateExecuteMonitorRequest() => new CnetExecuteMonitorEachAddressRequest(this, UseBCC);
        public CnetExecuteMonitorEachAddressRequest CreateExecuteMonitorRequest(bool useBCC) => new CnetExecuteMonitorEachAddressRequest(this, useBCC);
    }

    public class CnetRegisterMonitorAddressBlockRequest : CnetRegisterMonitorRequest, ICnetAddressBlockRequest
    {
        public CnetRegisterMonitorAddressBlockRequest(byte stationNumber, byte monitorNumber, DeviceAddress address, int count) : this(stationNumber, monitorNumber, address, count, true) { }

        public CnetRegisterMonitorAddressBlockRequest(byte stationNumber, byte monitorNumber, DeviceAddress address, int count, bool useBCC)
            : base(stationNumber, monitorNumber, CnetCommandType.Block, useBCC)
        {
            deviceAddress = address;
            this.count = count;
        }

        public override object Clone() => new CnetRegisterMonitorAddressBlockRequest(StationNumber, MonitorNumber, deviceAddress, count, UseBCC);

        private DeviceAddress deviceAddress;
        private int count;

        public DeviceAddress DeviceAddress { get => deviceAddress; set => SetProperty(ref deviceAddress, value); }
        public int Count { get => count; set => SetProperty(ref count, value); }

        protected override void OnCreateFrameData(List<byte> byteList)
        {
            base.OnCreateFrameData(byteList);

            var deviceAddressBytes = deviceAddress.ToBytes();
            byteList.AddRange(ToAsciiBytes(deviceAddressBytes.Length));
            byteList.AddRange(deviceAddressBytes);
            byteList.AddRange(ToAsciiBytes(count));
        }

        public CnetExecuteMonitorAddressBlockRequest CreateExecuteMonitorRequest() => new CnetExecuteMonitorAddressBlockRequest(this, UseBCC);
        public CnetExecuteMonitorAddressBlockRequest CreateExecuteMonitorRequest(bool useBCC) => new CnetExecuteMonitorAddressBlockRequest(this, useBCC);
    }

    public class CnetExecuteMonitorRequest : CnetIncludeCommandTypeRequest
    {
        public CnetExecuteMonitorRequest(byte stationNumber, byte monitorNumber, CnetCommandType commandType) : this(stationNumber, monitorNumber, commandType, true) { }

        public CnetExecuteMonitorRequest(byte stationNumber, byte monitorNumber, CnetCommandType commandType, bool useBCC)
            : base(stationNumber, CnetCommand.ExecuteMonitor, commandType, useBCC)
        {
            this.monitorNumber = monitorNumber;
        }

        public override object Clone() => new CnetExecuteMonitorRequest(StationNumber, MonitorNumber, CommandType, UseBCC);

        private byte monitorNumber;

        public byte MonitorNumber { get => monitorNumber; set => SetProperty(ref monitorNumber, value); }

        protected override void OnCreateFrameData(List<byte> byteList)
        {
            byteList.AddRange(ToAsciiBytes(MonitorNumber));
        }
    }

    public class CnetExecuteMonitorEachAddressRequest : CnetExecuteMonitorRequest, IReadOnlyList<DeviceAddress>
    {
        public CnetExecuteMonitorEachAddressRequest(CnetRegisterMonitorEachAddressRequest request) : this(request, true) { }

        public CnetExecuteMonitorEachAddressRequest(CnetRegisterMonitorEachAddressRequest request, bool useBCC)
            : base(request?.StationNumber ?? 0, request?.MonitorNumber ?? 0, CnetCommandType.Each, useBCC)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            this.request = (CnetRegisterMonitorEachAddressRequest)request.Clone();
        }

        public override object Clone() => new CnetExecuteMonitorEachAddressRequest(request, UseBCC);

        private readonly CnetRegisterMonitorEachAddressRequest request;

        int IReadOnlyCollection<DeviceAddress>.Count => request.Count;

        public DeviceAddress this[int index] => request[index];

        public IEnumerator<DeviceAddress> GetEnumerator() => request.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class CnetExecuteMonitorAddressBlockRequest : CnetExecuteMonitorRequest, ICnetAddressBlockRequest
    {
        public CnetExecuteMonitorAddressBlockRequest(CnetRegisterMonitorAddressBlockRequest request) : this(request, true) { }

        public CnetExecuteMonitorAddressBlockRequest(CnetRegisterMonitorAddressBlockRequest request, bool useBCC)
            : base(request?.StationNumber ?? 0, request?.MonitorNumber ?? 0, CnetCommandType.Block, useBCC)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            this.request = (CnetRegisterMonitorAddressBlockRequest)request.Clone();
        }

        public override object Clone() => new CnetExecuteMonitorAddressBlockRequest(request, UseBCC);

        private readonly CnetRegisterMonitorAddressBlockRequest request;

        public DeviceAddress DeviceAddress { get => request.DeviceAddress; }
        public int Count { get => request.Count; }
    }

    
}
