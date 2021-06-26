using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VagabondK.Protocols.LSElectric.Cnet
{
    /// <summary>
    /// LS ELECTRIC Cnet 프로토콜 응답 메시지
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
        /// 프레임 종료 코드
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



    public class CnetACKResponse : CnetResponse
    {
        public CnetACKResponse(CnetRequest request) : base(request) { }
        public override byte Header { get => ACK; }
        protected override void OnCreateFrameData(List<byte> byteList) { }
    }

    public class CnetReadResponse : CnetACKResponse, IReadOnlyDictionary<DeviceAddress, DeviceValue>
    {
        public CnetReadResponse(IEnumerable<DeviceValue> values, CnetReadRequest request) : base(request)
        {
            switch (request.CommandType)
            {
                case CnetCommandType.Each:
                    SetData(values, (CnetReadEachAddressRequest)request);
                    break;
                case CnetCommandType.Block:
                    SetData(values, ((CnetReadAddressBlockRequest)request).ToDeviceAddresses());
                    break;
            }
        }

        public CnetReadResponse(IEnumerable<DeviceValue> values, CnetExecuteMonitorRequest request) : base(request)
        {
            switch (request.CommandType)
            {
                case CnetCommandType.Each:
                    SetData(values, (CnetExecuteMonitorEachAddressRequest)request);
                    break;
                case CnetCommandType.Block:
                    SetData(values, ((CnetExecuteMonitorAddressBlockRequest)request).ToDeviceAddresses());
                    break;
            }
        }

        public CnetReadResponse(IEnumerable<byte> bytes, CnetReadAddressBlockRequest request) : base(request) => SetData(bytes, request?.DeviceAddress ?? new DeviceAddress(), request?.Count ?? 0);
        public CnetReadResponse(IEnumerable<byte> bytes, CnetExecuteMonitorAddressBlockRequest request) : base(request) => SetData(bytes, request?.DeviceAddress ?? new DeviceAddress(), request?.Count ?? 0);

        private void SetData(IEnumerable<DeviceValue> values, IEnumerable<DeviceAddress> deviceAddresses)
        {
            var valueArray = values.ToArray();
            var deviceAddressArray = deviceAddresses.ToArray();

            if (valueArray.Length != deviceAddressArray.Length) throw new ArgumentOutOfRangeException(nameof(values));

            deviceValueList = valueArray.Zip(deviceAddressArray, (v, a) => new KeyValuePair<DeviceAddress, DeviceValue>(a, v)).ToList();
            deviceValueDictionary = deviceValueList.ToDictionary(item => item.Key, item =>item.Value);
        }

        private void SetData(IEnumerable<byte> bytes, DeviceAddress deviceAddress, int count)
        {
            var byteArray = bytes.ToArray();

            int valueUnit = 1;

            Func<int, DeviceValue> getValue = null;

            switch (deviceAddress.DataType)
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

            dataType = deviceAddress.DataType;

            if (byteArray.Length != count * valueUnit) throw new ArgumentOutOfRangeException(nameof(bytes));

            deviceValueList = new List<KeyValuePair<DeviceAddress, DeviceValue>>();
            for (int i = 0; i < count; i++)
            {
                deviceValueList.Add(new KeyValuePair<DeviceAddress, DeviceValue>(deviceAddress, getValue(i)));
                deviceAddress = deviceAddress.Increase();
            }
            deviceValueDictionary = deviceValueList.ToDictionary(item => item.Key, item => item.Value);
        }


        private DataType? dataType;
        private List<KeyValuePair<DeviceAddress, DeviceValue>> deviceValueList;
        private Dictionary<DeviceAddress, DeviceValue> deviceValueDictionary;

        public IEnumerable<DeviceAddress> Keys => deviceValueDictionary.Keys;

        public IEnumerable<DeviceValue> Values => deviceValueDictionary.Values;

        public int Count => deviceValueDictionary.Count;

        public DeviceValue this[DeviceAddress key] => deviceValueDictionary[key];

        public bool ContainsKey(DeviceAddress key) => deviceValueDictionary.ContainsKey(key);

        public bool TryGetValue(DeviceAddress key, out DeviceValue value) => deviceValueDictionary.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<DeviceAddress, DeviceValue>> GetEnumerator() => deviceValueList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected override void OnCreateFrameData(List<byte> byteList)
        {
            if (dataType == null)
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
            else
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
        }
    }

    public class CnetNAKResponse : CnetResponse
    {
        public CnetNAKResponse(ushort nakCode, CnetRequest request) : base(request)
        {
            NAKCodeValue = nakCode;
            if (Enum.IsDefined(typeof(CnetNAKCode), nakCode))
                NAKCode = (CnetNAKCode)nakCode;
        }

        public CnetNAKResponse(CnetNAKCode nakCode, CnetRequest request) : base(request)
        {
            NAKCode = nakCode;
            NAKCodeValue = (ushort)nakCode;
        }

        public override byte Header { get => NAK; }

        public CnetNAKCode NAKCode { get; } = CnetNAKCode.Unknown;
        public ushort NAKCodeValue { get; }

        protected override void OnCreateFrameData(List<byte> byteList)
        {
            byteList.AddRange(ToAsciiBytes(NAKCodeValue, 4));
        }
    }
}
