using UnityEngine.Assertions;

namespace UnityEngine.AddressableAssets.Util
{
    internal static class Hex
    {
        public static char Char(int value)
        {
            Assert.IsTrue(value is >= 0 and < 16, "value is out of range");
            return (char) (value < 10 ? (value + '0') : (value - 10 + 'a'));
        }

        private static readonly char[] _hexBuf = new char[6];

        public static string To2(byte value)
        {
            Assert.AreEqual(0, value & 0xFFFFFF00, "value is out of range");
            _hexBuf[0] = Char(value >> 4);
            _hexBuf[1] = Char(value & 0xF);
            return new string(_hexBuf, 0, 2);
        }

        public static string To6(uint value)
        {
            Assert.AreEqual(0, value & 0xFF000000, "value is out of range");
            _hexBuf[0] = Char((int) ((value >> 20) & 0xF));
            _hexBuf[1] = Char((int) ((value >> 16) & 0xF));
            _hexBuf[2] = Char((int) ((value >> 12) & 0xF));
            _hexBuf[3] = Char((int) ((value >> 8) & 0xF));
            _hexBuf[4] = Char((int) ((value >> 4) & 0xF));
            _hexBuf[5] = Char((int) (value & 0xF));
            return new string(_hexBuf, 0, 6);
        }
    }
}