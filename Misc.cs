using System;

namespace Posnet
{
    /// <summary>
    /// Checksum class.
    /// </summary>
    internal class Crc16Ccitt
    {
        static ushort poly = 4129;
        static ushort[] table = new ushort[256];
        static ushort initialValue = 0;

        /// <summary>
        /// Calculates checksum.
        /// </summary>
        /// <param name="bytes">Bytes to calculate checksum from.</param>
        /// <returns></returns>
        internal static ushort ComputeChecksum(byte[] bytes)
        {
            ushort crc = initialValue;
            for (int i = 0; i < bytes.Length; ++i)
            {
                crc = (ushort)((crc << 8) ^ table[((crc >> 8) ^ (0xff & bytes[i]))]);
            }
            return crc;
        }

        static Crc16Ccitt()
        {
            initialValue = 0;
            ushort temp, a;
            for (int i = 0; i < table.Length; ++i)
            {
                temp = 0;
                a = (ushort)(i << 8);
                for (int j = 0; j < 8; ++j)
                {
                    if (((temp ^ a) & 0x8000) != 0)
                    {
                        temp = (ushort)((temp << 1) ^ poly);
                    }
                    else
                    {
                        temp <<= 1;
                    }
                    a <<= 1;
                }
                table[i] = temp;
            }
        }
    }

    public partial class Field
    {
        static byte[] ToBCD(long value)
        {

            if (value >= 0L && value < 500000000000L)
            {
                // do nothing
            }
            else if (value < 0L && value >= -500000000000L)
            {
                value = 1000000000000L + value;
            }
            else
            {
                throw new Exception("BCD value [" + value + "] out of range -500000000000 - 499999999999.");
            }

            byte[] result = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                result[i] = (byte)(value % 10);
                value /= 10;
                result[i] |= (byte)((value % 10) << 4);
                value /= 10;
            }
            return result;
        }

        static long FromBCD(byte[] value)
        {
            string buf = "";
            for (int i = value.Length - 1; i >= 0; i--)
            {
                buf += value[i].ToString("x2");
            }

            long tmp = long.Parse(buf);

            if (tmp >= 500000000000L)
            {
                tmp = tmp - 1000000000000;
            }

            return tmp;
        }
    }
}

