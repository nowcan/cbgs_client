using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Crypto
{
    class AESHelper
    {
        public static string Encrypt(string rawInput, byte[] iv, byte[] key)
        {
            if (string.IsNullOrEmpty(rawInput))
            {
                return string.Empty;
            }

            if (key == null || iv == null || key.Length < 1 || iv.Length < 1)
            {
                throw new ArgumentException("Key/Iv is null.");
            }

            using (var rijndaelManaged = new RijndaelManaged()
            {
                Key = key, // 密钥，长度可为128， 196，256比特位
                IV = iv,  //初始化向量(Initialization vector), 用于CBC模式初始化
                KeySize = 128,//接受的密钥长度
                FeedbackSize = 8,
                BlockSize = 128,//加密时的块大小，应该与iv长度相同
                Mode = CipherMode.CBC,//加密模式
                Padding = PaddingMode.Zeros//填白模式，对于AES, C# 框架中的 PKCS　＃７等同与Java框架中 PKCS #5
            })
            {
                using (var transform = rijndaelManaged.CreateEncryptor(key, iv))
                {
                    var inputBytes = Encoding.UTF8.GetBytes(rawInput);//字节编码， 将有特等含义的字符串转化为字节流
                    var encryptedBytes = transform.TransformFinalBlock(inputBytes, 0, inputBytes.Length);//加密
                    //return Convert.ToBase64String(encryptedBytes);//将加密后的字节流转化为字符串，以便网络传输与储存。
                    return HexConvert.bytes_to_hex_string(encryptedBytes);
                }
            }
        }

        public static string Decrypt(string encryptedInput, byte[] iv, byte[] key)
        {
            if (string.IsNullOrEmpty(encryptedInput))
            {
                return string.Empty;
            }

            if (key == null || iv == null || key.Length < 1 || iv.Length < 1)
            {
                throw new ArgumentException("Key/Iv is null.");
            }

            using (var rijndaelManaged = new RijndaelManaged()
            {
                Key = key,
                IV = iv,
                KeySize = 128,
                FeedbackSize = 8,
                BlockSize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.Zeros
            })
            {
                using (var transform = rijndaelManaged.CreateDecryptor(key, iv))
                {
                    //var inputBytes = Convert.FromBase64String(encryptedInput);
                    var inputBytes = HexConvert.hex_string_to_bytes(encryptedInput);
                    var encryptedBytes = transform.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                    return Encoding.UTF8.GetString(encryptedBytes);
                }
            }
        }

        /// <summary>
        /// AES加密
        /// </summary>
        /// <param name="inputdata">输入的数据</param>
        /// <param name="iv">向量128位</param>
        /// <param name="Key">加密密钥</param>
        /// <returns></returns>
        public static byte[] Encrypt(byte[] inputdata, byte[] iv, byte[] key)
        {
            if (key == null || iv == null || key.Length < 1 || iv.Length < 1)
            {
                throw new ArgumentException("Key/Iv is null.");
            }

            using (var rijndaelManaged = new RijndaelManaged()
            {
                Key = key, // 密钥，长度可为128， 196，256比特位
                IV = iv,  //初始化向量(Initialization vector), 用于CBC模式初始化
                KeySize = 128,//接受的密钥长度
                FeedbackSize = 8,
                BlockSize = 128,//加密时的块大小，应该与iv长度相同
                Mode = CipherMode.CBC,//加密模式
                Padding = PaddingMode.Zeros//填白模式，对于AES, C# 框架中的 PKCS　＃７等同与Java框架中 PKCS #5
            })
            {
                using (var transform = rijndaelManaged.CreateEncryptor(key, iv))
                {
                    return transform.TransformFinalBlock(inputdata, 0, inputdata.Length);//加密
                }
            }
        }


        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="inputdata">输入的数据</param>
        /// <param name="iv">向量128</param>
        /// <param name="Key">key</param>
        /// <returns></returns>
        public static byte[] Decrypt(byte[] inputdata, byte[] iv, byte[] key)
        {
            if (key == null || iv == null || key.Length < 1 || iv.Length < 1)
            {
                throw new ArgumentException("Key/Iv is null.");
            }

            using (var rijndaelManaged = new RijndaelManaged()
            {
                Key = key,
                IV = iv,
                KeySize = 128,
                FeedbackSize = 8,
                BlockSize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.Zeros
            })
            {
                using (var transform = rijndaelManaged.CreateDecryptor(key, iv))
                {
                    return transform.TransformFinalBlock(inputdata, 0, inputdata.Length);
                }
            }
        }
    }
}
