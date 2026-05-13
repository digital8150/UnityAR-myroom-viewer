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

    public static async Task<(long responseCode, bool isBookmarked)> GetBookmarkStatus(int modelId)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/model3ds/{modelId}/bookmarks/me";
        var (responseCode, jsonBody) = await SendRequest(url, "GET");

        if (responseCode == 200)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<BookmarkStatusResponse>(jsonBody);
                bool isBookmarked = response.bookmarked;
                return (responseCode, isBookmarked);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return (responseCode, false);
            }
        }

        return (responseCode, false);
    }

    public static async Task<long> AddBookmark(int modelId)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/model3ds/{modelId}/bookmarks";
        var (responseCode, _) = await SendRequest(url, "POST");
        Debug.Log($"[ModelService] AddBookmark Response Code: {responseCode}");
        return responseCode;
    }

    public static async Task<long> RemoveBookmark(int modelId)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/model3ds/{modelId}/bookmarks";
        var (responseCode, _) = await SendRequest(url, "DELETE");
        Debug.Log($"[ModelService] RemoveBookmark Response Code: {responseCode}");
        return responseCode;
    }
}