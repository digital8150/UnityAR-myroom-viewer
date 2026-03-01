using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ProjectInspectService
{
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
