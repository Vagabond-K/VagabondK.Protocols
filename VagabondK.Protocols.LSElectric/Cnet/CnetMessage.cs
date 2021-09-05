using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VagabondK.Protocols.LSElectric.Cnet
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 메시지
    /// </summary>
    public abstract class CnetMessage : IProtocolMessage
    {
        /// <summary>
        /// 1바이트를 16진수 아스키 문자의 바이트로 열거합니다.
        /// </summary>
        /// <param name="value">1바이트 값</param>
        /// <param name="size">자릿수</param>
        /// <returns>16진수 아스키 문자의 바이트 목록</returns>
        public static IEnumerable<byte> ToAsciiBytes(long value, int size = 2) => Encoding.ASCII.GetBytes(value.ToString("X" + size));

        private byte[] frameData;

        /// <summary>
        /// 요청 프레임 시작 코드
        /// </summary>
        public const byte ENQ = 0x05;

        /// <summary>
        /// 요청 프레임 종료 코드
        /// </summary>
        public const byte EOT = 0x04;

        /// <summary>
        /// ACK 응답 프레임 시작 코드
        /// </summary>
        public const byte ACK = 0x06;

        /// <summary>
        /// NAK 응답 프레임 시작 코드
        /// </summary>
        public const byte NAK = 0x15;

        /// <summary>
        /// 응답 프레임 종료 코드
        /// </summary>
        public const byte ETX = 0x03;


        /// <summary>
        /// 프레임 시작 헤더
        /// </summary>
        public abstract byte Header { get; }

        /// <summary>
        /// 프레임 종료 테일
        /// </summary>
        public abstract byte Tail { get; }

        /// <summary>
        /// 내부 프레임 데이터 무효화
        /// </summary>
        protected void InvalidateFrameData()
        {
            lock (this)
            {
                frameData = null;
            }
        }

        internal static bool TryParseByte(IList<byte> bytes, int index, out byte value)
            => byte.TryParse($"{(char)bytes[index]}{(char)bytes[index + 1]}", System.Globalization.NumberStyles.HexNumber, null, out value);
        internal static bool TryParseUint16(IList<byte> bytes, int index, out ushort value)
            => ushort.TryParse($"{(char)bytes[index]}{(char)bytes[index + 1]}{(char)bytes[index + 2]}{(char)bytes[index + 3]}", System.Globalization.NumberStyles.HexNumber, null, out value);
        internal static bool TryParseUint32(IList<byte> bytes, int index, out uint value)
            => uint.TryParse($"{(char)bytes[index]}{(char)bytes[index + 1]}{(char)bytes[index + 2]}{(char)bytes[index + 3]}{(char)bytes[index + 4]}{(char)bytes[index + 5]}{(char)bytes[index + 6]}{(char)bytes[index + 7]}", System.Globalization.NumberStyles.HexNumber, null, out value);
        internal static bool TryParseUint64(IList<byte> bytes, int index, out ulong value)
            => ulong.TryParse($"{(char)bytes[index]}{(char)bytes[index + 1]}{(char)bytes[index + 2]}{(char)bytes[index + 3]}{(char)bytes[index + 4]}{(char)bytes[index + 5]}{(char)bytes[index + 6]}{(char)bytes[index + 7]}{(char)bytes[index + 8]}{(char)bytes[index + 9]}{(char)bytes[index + 10]}{(char)bytes[index + 11]}{(char)bytes[index + 12]}{(char)bytes[index + 13]}{(char)bytes[index + 14]}{(char)bytes[index + 15]}", System.Globalization.NumberStyles.HexNumber, null, out value);

        /// <summary>
        /// 속성 값 설정
        /// </summary>
        /// <typeparam name="TProperty">속성 형식</typeparam>
        /// <param name="target">설정할 멤버 변수</param>
        /// <param name="value">새로 설정하는 값</param>
        /// <returns>설정 여부</returns>
        protected bool SetProperty<TProperty>(ref TProperty target, TProperty value)
        {
            lock (this)
            {
                if (!EqualityComparer<TProperty>.Default.Equals(target, value))
                {
                    target = value;
                    frameData = null;
                    return true;
                }
                return false;
            }
        }


        /// <summary>
        /// 프레임 생성
        /// </summary>
        /// <param name="byteList">프레임 데이터를 추가할 바이트 리스트</param>
        /// <param name="useBCC">BCC 사용 여부</param>
        protected abstract void OnCreateFrame(List<byte> byteList, bool useBCC);

        /// <summary>
        /// 직렬화
        /// </summary>
        /// <param name="useBCC">BCC 사용 여부</param>
        /// <returns>직렬화 된 바이트 열거</returns>
        public virtual IEnumerable<byte> Serialize(bool useBCC)
        {
            lock (this)
            {
                if (frameData == null)
                {
                    List<byte> byteList = new List<byte> { Header };

                    OnCreateFrame(byteList, useBCC);

                    byteList.Add(Tail);

                    if (useBCC)
                        byteList.AddRange(Encoding.ASCII.GetBytes((byteList.Sum(b => b) % 256).ToString("X2")));

                    frameData = byteList.ToArray();
                }
                return frameData;
            }
        }
    }
}
