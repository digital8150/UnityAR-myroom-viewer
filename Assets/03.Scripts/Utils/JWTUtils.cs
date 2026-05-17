using System;
using System.Text;
using UnityEngine;

[Serializable]
public class JWTPayload
{
    public string id;
    public string userId;
    public string memberId;
    public string sub;
    public string email;
}

public class JWTToken
{
    public static string Token;
    public static string RefreshToken;
}

namespace Utils
{
    public static class JWTUtils
    {
        public static string GetUserId()
        {
            var payload = GetDecodedPayload();
            if (payload == null) return null;

            return !string.IsNullOrEmpty(payload.id) ? payload.id :
                   !string.IsNullOrEmpty(payload.userId) ? payload.userId :
                   !string.IsNullOrEmpty(payload.memberId) ? payload.memberId : payload.sub;
        }

        public static string GetEmail()
        {
            var payload = GetDecodedPayload();
            if (payload == null) return null;

            return !string.IsNullOrEmpty(payload.sub) ? payload.sub : payload.email;
        }

        private static JWTPayload GetDecodedPayload()
        {
            string token = JWTToken.Token;
            if (string.IsNullOrEmpty(token)) return null;

            try
            {
                // 1. JWT의 페이로드(두 번째 부분) 추출
                string[] parts = token.Split('.');
                if (parts.Length < 2) return null;

                string payloadBase64 = parts[1];

                // 2. Base64 Padding 보정 (4의 배수가 아니면 '=' 추가)
                payloadBase64 = payloadBase64.PadRight(payloadBase64.Length + (4 - payloadBase64.Length % 4) % 4, '=')
                                          .Replace('-', '+')
                                          .Replace('_', '/');

                // 3. 디코딩 후 JSON 파싱
                byte[] decodedBytes = Convert.FromBase64String(payloadBase64);
                string json = Encoding.UTF8.GetString(decodedBytes);

                return JsonUtility.FromJson<JWTPayload>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[JWT Error] {e.Message}");
                return null;
            }
        }
    }
}