using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Utils;

public static class AuthService
{
    public static async Task<(long code, string body)> PostRefresh(RefreshRequest data)
    {
        long responseCode = 404;
        string json = JsonConvert.SerializeObject(data);
        using (var request = await SendPost($"{Utils.Settings.BaseUrl}/api/auth/refresh", json))
        {
            responseCode = request.responseCode;
            if(responseCode == 200)
            {
                return (responseCode, request.downloadHandler.text);
            }
            return(responseCode, string.Empty);
        }
    }

    public static async Task<long> Register(RegisterRequest data)
    {
        string json = JsonUtility.ToJson(data);
        using(var request = await SendPost($"{Utils.Settings.BaseUrl}/api/auth/register", json))
        {
            return request.responseCode;
        }
    }

    public static async Task<(long code, string jsonBody)> Login(LoginRequest data)
    {
        string json = JsonUtility.ToJson(data);

        using(var request = await SendPost($"{Utils.Settings.BaseUrl}/api/auth/login", json))
        {
            if (request.responseCode == 200)
            {
                var responseJson = request.downloadHandler.text;
                return (request.responseCode, responseJson);
            }
            return (request.responseCode, null);
        }
    }

    public static async Task<(long, string)> GetExists(string email)
    {
        long responseCode = 404;
        using(var request = UnityWebRequest.Get($"{Utils.Settings.BaseUrl}/api/auth/exists?email={email}"))
        {
            request.SetRequestHeader("accept", "application/json");

            await request.SendWebRequest();
            responseCode = request.responseCode;

            if(request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Request Failed: {request.error} | Details: {request.downloadHandler.text}");
                return (responseCode, string.Empty);
            }

            return (responseCode, request.downloadHandler.text);
        }
    }

    /// <summary>
    /// Token이 만료되었을 때, Refresh Token을 이용하여 새로운 Access Token을 발급받는 메서드입니다. 재발급 실패 시 로그인 화면으로 이동합니다.
    /// </summary>
    public static async Task<bool> Refresh()
    {
        var reqData = new RefreshRequest { refreshToken = JWTToken.RefreshToken };

        var (responseCode, responseBody) = await PostRefresh(reqData);

        if (responseCode == 200)
        {
            try
            {
                var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseBody);
                if (!string.IsNullOrEmpty(loginResponse?.token))
                {
                    JWTToken.Token = loginResponse.token;
                    JWTToken.RefreshToken = loginResponse.refreshToken;
                    return true;
                }
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Failed to parse refresh response: {ex.Message}");
            }
        }

        // 실패 시 로직
        Debug.LogError($"Token refresh failed with code {responseCode}: {responseBody}");
        Utils.SceneHistory.ChangeScene("LoginScene");
        return false;
    }

    private static async Task<UnityWebRequest> SendPost(string url, string json)
    {
        var request = new UnityWebRequest(url, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("accept", "*/*");

        await request.SendWebRequest();

        return request;
    }
}