using UnityEngine;
namespace Utils.Cipher
{
    public class SecureStorage
    {
        private static SecureStorage _instance;
        public static SecureStorage Instance => _instance ??= new SecureStorage();

        private SecureStorage() { }

        /// <summary>
        /// 데이터를 암호화하여 PlayerPrefs에 저장합니다.
        /// </summary>
        public void SetValue(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                DeleteValue(key);
                return;
            }

            var encryptedValue = AesProtector.Encrypt(value);
            PlayerPrefs.SetString(key, encryptedValue);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// PlayerPrefs에서 암호화된 데이터를 가져와 복호화합니다.
        /// </summary>
        public string GetValue(string key)
        {
            var encryptedValue = PlayerPrefs.GetString(key, null);
            if (string.IsNullOrEmpty(encryptedValue)) return null;

            return AesProtector.Decrypt(encryptedValue);
        }

        /// <summary>
        /// 저장된 값을 삭제합니다.
        /// </summary>
        public void DeleteValue(string key)
        {
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }
    }
}
