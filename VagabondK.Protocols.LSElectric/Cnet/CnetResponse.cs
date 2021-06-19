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
    public abstract class CnetResponse : CnetMessage
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="request">요청 메시지</param>
        internal CnetResponse(CnetRequest request)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
        }

        private byte[] frameData;

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
        internal CnetACKResponse(CnetRequest request) : base(request) { }
        public override byte Header { get => ACK; }
        protected override void OnCreateFrameData(List<byte> byteList) { }
    }

    public abstract class CnetReadResponse : CnetACKResponse
    {
        internal CnetReadResponse(CnetReadRequest request) : base(request) { }
    }

    public class CnetReadEachAdressResponse : CnetACKResponse, IReadOnlyDictionary<DeviceAddress, DeviceValue>
    {
        internal CnetReadEachAdressResponse(IEnumerable<DeviceValue> values, CnetReadEachAddressRequest request) : base(request) => SetData(true, values, request);
        internal CnetReadEachAdressResponse(IEnumerable<DeviceValue> values, CnetExecuteMonitorEachAddressRequest request) : base(request) => SetData(true, values, request);

        internal CnetReadEachAdressResponse(IEnumerable<byte> bytes, CnetReadAddressBlockRequest request) : base(request) => SetData(false, bytes, request?.DeviceAddress ?? new DeviceAddress(), request?.Count ?? 0);
        internal CnetReadEachAdressResponse(IEnumerable<byte> bytes, CnetExecuteMonitorAddressBlockRequest request) : base(request) => SetData(false, bytes, request?.DeviceAddress ?? new DeviceAddress(), request?.Count ?? 0);

        private void SetData(bool isEachAddresskRequest, IEnumerable<DeviceValue> values, IEnumerable<DeviceAddress> deviceAddresses)
        {
            this.isEachAddresskRequest = isEachAddresskRequest;

            var valueArray = values.ToArray();
            var deviceAddressArray = deviceAddresses.ToArray();

            if (valueArray.Length != deviceAddressArray.Length) throw new ArgumentOutOfRangeException(nameof(values));

            deviceValueList = valueArray.Zip(deviceAddressArray, (v, a) => new KeyValuePair<DeviceAddress, DeviceValue>(a, v)).ToList();
            deviceValueDictionary = deviceValueList.ToDictionary(item => item.Key, item =>item.Value);
        }

        private void SetData(bool isEachAddresskRequest, IEnumerable<byte> bytes, DeviceAddress deviceAddress, byte count)
        {
            this.isEachAddresskRequest = isEachAddresskRequest;

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

            if (byteArray.Length != count * valueUnit) throw new ArgumentOutOfRangeException(nameof(bytes));

            deviceValueList = Enumerable.Range(0, count).Select(i =>
            {
                return new KeyValuePair<DeviceAddress, DeviceValue>(deviceAddress.Increase(), getValue(i));
            }).ToList();
            deviceValueDictionary = deviceValueList.ToDictionary(item => item.Key, item => item.Value);
        }

        private bool isEachAddresskRequest;
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
            if (isEachAddresskRequest)
            {

            }
            else
            {

            }
        }
    }

    public class CnetNAKResponse : CnetResponse
    {
        internal CnetNAKResponse(ushort errorCode, CnetRequest request) : base(request)
        {
            ErrorCodeValue = errorCode;
            if (Enum.IsDefined(typeof(CnetErrorCode), errorCode))
                ErrorCode = (CnetErrorCode)errorCode;
        }

        public override byte Header { get => NAK; }

        public CnetErrorCode ErrorCode { get; } = CnetErrorCode.Unknown;
        public ushort ErrorCodeValue { get; }

        protected override void OnCreateFrameData(List<byte> byteList)
        {
            byteList.AddRange(ToAsciiBytes(ErrorCodeValue, 4));
        }
    }
}
