using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;

public class HTTPRequest
{
    private readonly string _baseUrl;
    private readonly MonoBehaviour _coroutineRunner;

    public HTTPRequest(string baseUrl, MonoBehaviour coroutineRunner)
    {
        _baseUrl = baseUrl;
        _coroutineRunner = coroutineRunner;
    }

    private IEnumerator GetRequest<T>(string url, Action<T> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Error [{request.responseCode}]: {request.error}");
            }
            else
            {
                string json = request.downloadHandler.text;
                try
                {
                    // JsonUtility 대신 JsonConvert.DeserializeObject 사용
                    // 이제 JSON 배열을 직접 List<T>로 변환 가능
                    T result = JsonConvert.DeserializeObject<T>(json);
                    onSuccess?.Invoke(result);
                }
                catch (Exception e)
                {
                    onError?.Invoke($"JSON Parse Error: {e.Message}\nReceived JSON: {json}");
                }
            }
        }
    }

    private IEnumerator PostRequest<T>(string url, object payload, Action<T> onSuccess, Action<string> onError)
    {
        // JsonUtility 대신 JsonConvert.SerializeObject 사용하여 페이로드 직렬화
        string jsonPayload = JsonConvert.SerializeObject(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Error [{request.responseCode}]: {request.error}");
            }
            else
            {
                string json = request.downloadHandler.text;
                try
                {
                    // JsonUtility 대신 JsonConvert.DeserializeObject 사용
                    var result = JsonConvert.DeserializeObject<T>(json);
                    onSuccess?.Invoke(result);
                }
                catch (Exception e)
                {
                    onError?.Invoke($"JSON Parse Error: {e.Message}\nReceived JSON: {json}");
                }
            }
        }
    }

    private IEnumerator DeleteRequest(string url, Action onSuccess, Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequest.Delete(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Error [{request.responseCode}]: {request.error}");
            }
            else
            {
                onSuccess?.Invoke();
            }
        }
    }
}
