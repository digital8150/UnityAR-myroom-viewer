using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ProjectInspectService : BaseService
{
    public static async Task<(long responseCode, string localPath)> GetModel3DFile(string s3Url)
    {
        try
        {
            string fileName = Path.GetFileName(s3Url);
            string directoryPath = Path.Combine(Application.persistentDataPath, "Models");
            string localPath = Path.Combine(directoryPath, fileName);

            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

            if (File.Exists(localPath))
            {
                Debug.Log($"[Cache Hit] 모델이 이미 존재함: {localPath}");
                return (200, localPath);
            }

            // S3 직접 다운로드는 BaseService 생략
            using (UnityWebRequest www = UnityWebRequest.Get(s3Url))
            {
                // Task.Yield() 제거하고 깔끔한 await로 수정
                await www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    await File.WriteAllBytesAsync(localPath, www.downloadHandler.data);
                    Debug.Log($"[Download Success] 모델 다운로드 완료: {localPath}");
                    return (200, localPath);
                }
                else
                {
                    Debug.LogError($"[Download Failed] {www.error}");
                    return (www.responseCode, null);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Service Error] {e.Message}");
            return (500, null);
        }
    }

    public static async Task<(long, string)> PutModel3DV3(int modelId, ModelUpdateData updateData)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/model3ds/v3/{modelId}";
        string jsonPayload = JsonConvert.SerializeObject(updateData);

        return await SendRequest(url, "PUT", jsonPayload: jsonPayload);
    }

    public static async Task<long> PutModel3DDimension(int modelId, ModelDimension dimension)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/model3ds/{modelId}/dimensions";
        string jsonPayload = JsonConvert.SerializeObject(dimension);

        var (code, _) = await SendRequest(url, "PUT", jsonPayload: jsonPayload);
        return code;
    }

    public static async Task<(long, string)> GetModel3DDimension(int modelId)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/model3ds/{modelId}/dimensions";
        return await SendRequest(url, "GET");
    }

    public static async Task<(long, string)> RequestDimensionByImage(int model3dId, byte[] imageBytes, string fileName = "image.jpg", string mimeType = "image/jpeg")
    {
        string url = $"{Utils.Settings.BaseUrl}/api/model3ds/{model3dId}/dimensions/request-by-image";

        WWWForm form = new();
        form.AddBinaryData("image", imageBytes, fileName, mimeType);

        return await SendRequest(url, "POST", form: form);
    }

    public static async Task<long> DeleteModel3D(int modelId)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/model3ds/{modelId}";
        var (code, _) = await SendRequest(url, "DELETE");
        return code;
    }

    public static async Task<(long, string)> GetSingleModel3D(int modelId)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/model3ds/{modelId}";
        return await SendRequest(url, "GET");
    }
}