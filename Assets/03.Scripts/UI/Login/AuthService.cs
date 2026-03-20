using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

public static class AuthService
{
    public static async Task<(long code, string body)> PostRefrsh(RefreshRequest data)
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