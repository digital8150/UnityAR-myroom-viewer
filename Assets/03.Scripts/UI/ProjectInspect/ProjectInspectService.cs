using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public class ProjectInspectService
{
    public static async Task<(long responseCode, string localPath)> GetModel3DFile(string s3Url)
    {
        try
        {
            // 1. 파일 이름 추출 (S3 주소 마지막 부분)
            string fileName = Path.GetFileName(s3Url);
            // 2. 로컬 저장 경로 설정 (Application.persistentDataPath/Models/파일명.glb)
            string directoryPath = Path.Combine(Application.persistentDataPath, "Models");
            string localPath = Path.Combine(directoryPath, fileName);

            // 폴더가 없으면 생성
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

            // 3. 캐싱 확인: 이미 파일이 존재하면 바로 반환
            if (File.Exists(localPath))
            {
                Debug.Log($"[Cache Hit] 모델이 이미 존재함: {localPath}");
                return (200, localPath);
            }

            // 4. 파일 다운로드 (UnityWebRequest)
            using (UnityWebRequest www = UnityWebRequest.Get(s3Url))
            {
                // 다운로드 대기
                var operation = www.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    // 5. 로컬에 파일 쓰기
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
        long responseCode = 404;
        string jsonPayload = JsonConvert.SerializeObject(updateData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        using (var request = UnityWebRequest.Put($"{Utils.Settings.BaseUrl}/api/model3ds/v3/{modelId}", bodyRaw))
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
