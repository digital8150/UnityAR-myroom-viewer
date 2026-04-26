using System;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

// BaseService 상속을 위해 일반 class로 변경 (메서드는 static 유지)
public class ModelService : BaseService
{
    public static async Task<(long responseCode, string jsonBody)> GetModelJSONByModelId(int modelId)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/model3ds/{modelId}";

        // BaseService의 SendRequest로 통일 (토큰 리프레시 자동 적용)
        return await SendRequest(url, "GET");
    }

    public static async Task<ModelData> GetModelDataByModelId(int modelId)
    {
        var (responseCode, jsonBody) = await GetModelJSONByModelId(modelId);
        Debug.Log($"[ModelService] GetModelByModelId Response Code: {responseCode}, Body: {jsonBody}");

        if (responseCode == 200)
        {
            try
            {
                ModelData model = JsonConvert.DeserializeObject<ModelData>(jsonBody);
                Debug.Log($"[ModelService] Deserialized Model Name: {model?.name}");
                return model;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        return null;
    }

    public static async Task<string> GetModelNameByModelId(int modelId)
    {
        var (responseCode, jsonBody) = await GetModelJSONByModelId(modelId);
        Debug.Log($"[ModelService] GetModelByModelId Response Code: {responseCode}, Body: {jsonBody}");

        if (responseCode == 200)
        {
            try
            {
                ModelData model = JsonConvert.DeserializeObject<ModelData>(jsonBody);
                Debug.Log($"[ModelService] Deserialized Model Name: {model?.name}");
                return model?.name;
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
        var (responseCode, jsonBody) = await GetModelJSONByModelId(modelId);

        if (responseCode == 200)
        {
            try
            {
                ModelData model = JsonConvert.DeserializeObject<ModelData>(jsonBody);
                return model?.thumbnailUrl;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        Debug.LogError($"[ModelService] Failed to get model data for modelId: {modelId}, responseCode: {responseCode}, body: {jsonBody}");
        return null;
    }
}