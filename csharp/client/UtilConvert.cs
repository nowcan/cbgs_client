using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UtilConvert
{
    //字符串转字节数组，var strToBytes4 = System.Text.Encoding.ASCII.GetBytes(str4);
    //字节数组转字符串，var byteToString4 = System.Text.Encoding.ASCII.GetString(strToBytes4);

    class NumberConvert
    {
        public static string bytes_to_hex_string(byte[] bytes)
        {
            string str = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    str += bytes[i].ToString("X2");
                }
            }

            return str;
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

        public static string bytes_to_binary_string(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < bytes.Length; i++)
            {
                string s = Convert.ToString(bytes[i], 2);
                for (int j = 8; j > s.Length; j--)
                {
                    sb.Append("0");
                }

                sb.Append(s);
            }

            return sb.ToString();
        }
    }

    class DateTimeConvert
    {
        public static DateTime ConvertIntDatetime(double utc)
        {
            try
            {
                DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
                startTime = startTime.AddSeconds(utc);
                return startTime;
            }
            catch (Exception)
            {
                return DateTime.Parse("1979-01-01 00:00:00");
            }
        }
    }

    class MarshalConvert
    {
        public static byte[] StructToBytes(object obj)
        {
            int rawsize = Marshal.SizeOf(obj);
            IntPtr buffer = Marshal.AllocHGlobal(rawsize);
            Marshal.StructureToPtr(obj, buffer, false);
            byte[] rawdatas = new byte[rawsize];
            Marshal.Copy(buffer, rawdatas, 0, rawsize);
            Marshal.FreeHGlobal(buffer);
            return rawdatas;
        }

        public static void StructToBytes(object obj, byte[] buf, int offset)
        {
            int rawsize = Marshal.SizeOf(obj);
            IntPtr buffer = Marshal.AllocHGlobal(rawsize);
            Marshal.StructureToPtr(obj, buffer, false);
            Marshal.Copy(buffer, buf, offset, rawsize);
            Marshal.FreeHGlobal(buffer);
        }

        public static object BytesToStruct(byte[] buf, int offset, int len, Type type)
        {
            object rtn;
            IntPtr buffer = Marshal.AllocHGlobal(len);
            Marshal.Copy(buf, offset, buffer, len);
            rtn = Marshal.PtrToStructure(buffer, type);
            Marshal.FreeHGlobal(buffer);
            return rtn;
        }

        public static void BytesToStruct(byte[] buf, int offset, int len, object rtn)
        {
            IntPtr buffer = Marshal.AllocHGlobal(len);
            Marshal.Copy(buf, offset, buffer, len);
            Marshal.PtrToStructure(buffer, rtn);
            Marshal.FreeHGlobal(buffer);
        }

        public static void BytesToStruct(byte[] buf, object rtn)
        {
            BytesToStruct(buf, 0, buf.Length, rtn);
        }

        public static object BytesToStruct(byte[] buf, Type type)
        {
            return BytesToStruct(buf, 0, buf.Length, type);
        }
    }
}
