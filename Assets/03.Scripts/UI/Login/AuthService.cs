using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Text;

public class AuthService
{
    private const string BaseUrl = "http://home.codingbot.kr:8080";

    public async Task<long> Register(RegisterRequest data)
    {
        string json = JsonUtility.ToJson(data);
        var request = await SendPost($"{BaseUrl}/api/auth/register", json);
        return request.responseCode;
    }

    public async Task<(long code, string token)> Login(LoginRequest data)
    {
        string json = JsonUtility.ToJson(data);
        Debug.Log(json);
        var request = await SendPost($"{BaseUrl}/api/auth/login", json);

        if (request.responseCode == 200)
        {
            var res = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            return (200, res.token);
        }
        return (request.responseCode, null);
    }

    private async Task<UnityWebRequest> SendPost(string url, string json)
    {
        var request = new UnityWebRequest(url, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("accept", "*/*");

        var operation = request.SendWebRequest();
        while (!operation.isDone) await Task.Yield();
        return request;
    }
}