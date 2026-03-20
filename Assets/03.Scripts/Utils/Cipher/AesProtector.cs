using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Utils.Cipher
{
    public static class AesProtector
    {
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("oWGta7Ye4UlsIahx0yq2RwzqlPImRot8");
        private const int Iterations = 10000;

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return null;

            using (var aes = Aes.Create())
            {
                var keyIv = GenerateKeyAndIv();
                aes.Key = keyIv.Key;
                aes.GenerateIV(); // 매번 새로운 IV 생성

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                {
                    // IV를 데이터 맨 앞에 저장 (복호화 시 필요)
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return null;

            try
            {
                var fullCipher = Convert.FromBase64String(cipherText);
                using (var aes = Aes.Create())
                {
                    var keyIv = GenerateKeyAndIv();
                    aes.Key = keyIv.Key;

                    using (var ms = new MemoryStream(fullCipher))
                    {
                        // 저장된 IV 추출
                        var iv = new byte[aes.BlockSize / 8];
                        ms.Read(iv, 0, iv.Length);
                        aes.IV = iv;

                        using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        using (var sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
            catch
            {
                Debug.LogError("복호화 실패: 데이터가 변조되었거나 키가 일치하지 않아.");
                return null;
            }
        }

        private static (byte[] Key, byte[] Iv) GenerateKeyAndIv()
        {
            // 기기 고유 ID를 패스워드로 사용
            using (var rfc = new Rfc2898DeriveBytes(SystemInfo.deviceUniqueIdentifier, Salt, Iterations, HashAlgorithmName.SHA256))
            {
                return (rfc.GetBytes(32), rfc.GetBytes(16));
            }
        }
    }
}
