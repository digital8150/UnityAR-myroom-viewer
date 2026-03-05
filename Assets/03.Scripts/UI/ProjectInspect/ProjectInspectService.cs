using Newtonsoft.Json;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ProjectInspectService
{
    public static async Task<(long, string)> PutModel3DV2(int modelId, ModelUpdateData updateData)
    {
        long responseCode = 404;
        string jsonPayload = JsonConvert.SerializeObject(updateData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        using (var request = UnityWebRequest.Put($"{Utils.Settings.BaseUrl}/api/model3ds/v2/{modelId}", bodyRaw))
        {
            request.SetRequestHeader("Authorization", $"Bearer {JWTToken.Token}");
            request.SetRequestHeader("accept", "application/json");
            request.SetRequestHeader("Content-Type", "application/json");
            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();
            responseCode = request.responseCode;
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Request Failed: {request.error} | Details: {request.downloadHandler.text}");
                return (responseCode, string.Empty);
            }
            string body = request.downloadHandler.text;
            return (responseCode, body);
        }
    }

    public static async Task<long> PutModel3DDimension(int modelId, ModelDimension dimension)
    {
        long responseCode = 404;

        string jsonPayload = JsonConvert.SerializeObject(dimension);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

        using (var request = UnityWebRequest.Put($"{Utils.Settings.BaseUrl}/api/model3ds/{modelId}/dimensions", bodyRaw))
        {
            request.SetRequestHeader("Authorization", $"Bearer {JWTToken.Token}");
            request.SetRequestHeader("accept", "application/json");
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();

            while (!operation.isDone) await Task.Yield();

            responseCode = request.responseCode;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Request Failed: {request.error} | Details: {request.downloadHandler.text}");
            }
        }

        return responseCode;
    }

    public static async Task<(long, string)> GetModel3DDimension(int modelId)
    {
        long responseCode = 404;
        using(var request = UnityWebRequest.Get($"{Utils.Settings.BaseUrl}/api/model3ds/{modelId}/dimensions"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {JWTToken.Token}");
            request.SetRequestHeader("accept", "application/json");

            var operation = request.SendWebRequest();

            while(!operation.isDone) await Task.Yield();

            responseCode = request.responseCode;
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Request Failed: {request.error} | Details: {request.downloadHandler.text}");
                return (responseCode, string.Empty);
            }

            string body = request.downloadHandler.text;
            return (responseCode, body);
        }
    }

    public static async Task<long> DeleteModel3D(int modelId)
    {
        long responseCode = 0;

        using (var request = UnityWebRequest.Delete($"{Utils.Settings.BaseUrl}/api/model3ds/{modelId}"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {JWTToken.Token}");
            request.SetRequestHeader("accept", "application/json");

            var operation = request.SendWebRequest();

            while (!operation.isDone) await Task.Yield();

            responseCode = request.responseCode;
            return responseCode;
        }
    }

    public static async Task<(long, string)> GetSingleModel3D(int modelId)
    {
        long responseCode = 0;

        using (var request = UnityWebRequest.Get($"{Utils.Settings.BaseUrl}/api/model3ds/{modelId}"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {JWTToken.Token}");
            request.SetRequestHeader("accept", "application/json");

            var operation = request.SendWebRequest();

            while (!operation.isDone) await Task.Yield();

            responseCode = request.responseCode;
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Request Failed: {request.error} | Details: {request.downloadHandler.text} | URL : {Utils.Settings.BaseUrl}/api/model3ds/{modelId}");
                return (responseCode, string.Empty);
            }


            string body = request.downloadHandler.text;
            return (responseCode, body);
        }
    }
}
