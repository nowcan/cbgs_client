using System;
using System.Collections.Generic;
using System.Text;

namespace Crypto
{
    class HexConvert
    {
        public static string bytes_to_hex_string(byte[] bytes, int start_idx = 0)
        {
            string hex_string = string.Empty;
            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();
                for (int i = start_idx; i < bytes.Length; i++)
                {
                    strB.Append(bytes[i].ToString("X2"));
                }

                hex_string = strB.ToString();
            }

            return hex_string;
        }

        public static byte[] hex_string_to_bytes(string str)
        {
            str = str.Replace(" ", "");
            str = str.Replace(",", "");
            if ((str.Length % 2) != 0)
            {
                str += " ";
            }

            byte[] bytes = new byte[str.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
            }

            return bytes;
        }

    }
}
