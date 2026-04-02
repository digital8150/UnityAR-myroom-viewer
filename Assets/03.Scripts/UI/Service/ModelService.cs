using System;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
using Newtonsoft.Json;

public static class ModelService
{
    public static async Task<(long responseCode, string jsonBody)> GetModelByModelId(int modelId)
    {
        using(var request = UnityWebRequest.Get($"{Utils.Settings.BaseUrl}/api/model3ds/{modelId}"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {JWTToken.Token}");
            request.SetRequestHeader("accept", "application/json");
            await request.SendWebRequest();
            return (request.responseCode, request.downloadHandler.text);
        }
    }

    public static async Task<string> GetModelNameByModelId(int modelId)
    {
        var(responseCode, jsonBody) = await GetModelByModelId(modelId);
        Debug.Log($"[ModelService] GetModelByModelId Response Code: {responseCode}, Body: {jsonBody}");
        if (responseCode == 200)
        {
            try
            {
                ModelData model = JsonConvert.DeserializeObject<ModelData>(jsonBody);
                Debug.Log($"[ModelService] Deserialized Model Name: {model.name}");
                return model.name;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        return null;
    }

    public static async Task<string> GetModelThumbnailUrlByModelId(int modelId)
    {
        var(responseCode, jsonBody) = await GetModelByModelId(modelId);

        if(responseCode == 200)
        {
            try
            {
                ModelData model = JsonConvert.DeserializeObject<ModelData>(jsonBody);
                return model.thumbnailUrl;
            }catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        Debug.LogError($"[ModelService] Failed to get model data for modelId: {modelId}, responseCode: {responseCode}, body: {jsonBody}");
        return null;
    }
}