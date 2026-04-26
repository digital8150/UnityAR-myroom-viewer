using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;

public class BaseService
{
    // jsonPayload 파라미터 추가
    protected static async Task<(long, string)> SendRequest(string url, string method, WWWForm form = null, string jsonPayload = null, string accept = "application/json")
    {
        var (code, body) = await ExecuteRequest(url, method, form, jsonPayload, accept);

        if (code == 401)
        {
            Debug.LogWarning("Token expired? Attempting to refresh...");
            bool isRefreshed = await AuthService.Refresh();

            if (isRefreshed)
            {
                Debug.Log("Refresh successful. Retrying request...");
                return await ExecuteRequest(url, method, form, jsonPayload, accept);
            }
        }

        return (code, body);
    }

    private static async Task<(long, string)> ExecuteRequest(string url, string method, WWWForm form = null, string jsonPayload = null, string accept = "application/json")
    {
        using var request = new UnityWebRequest(url, method);
        request.downloadHandler = new DownloadHandlerBuffer();

        if (form != null)
        {
            request.uploadHandler = new UploadHandlerRaw(form.data);
            request.SetRequestHeader("Content-Type", form.headers["Content-Type"]);
        }
        // JSON 페이로드가 있으면 UTF8 바이트로 변환해서 세팅!
        else if (!string.IsNullOrEmpty(jsonPayload))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.SetRequestHeader("Content-Type", "application/json");
        }

        request.SetRequestHeader("Authorization", $"Bearer {JWTToken.Token}");
        request.SetRequestHeader("accept", accept);

        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Request Failed: {request.error} | URL: {url}");
        }

        return (request.responseCode, request.downloadHandler.text);
    }
}