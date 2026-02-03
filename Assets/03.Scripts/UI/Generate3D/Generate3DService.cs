using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class Generate3DService
{
    public static async Task<long> PostUpload(string imagePath, string furniture_type = "table", string name = "테이블", string description = "", bool isShared = true)
    {
        if(!File.Exists(imagePath))
        {
            Debug.LogError($"File not found in device!: {imagePath}");
            return 404;
        }

        byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
        string fileName = Path.GetFileName(imagePath);
        string extension = Path.GetExtension(imagePath).ToLower();

        string mimeType = extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        WWWForm form = new WWWForm();
        form.AddBinaryData("image", imageBytes, fileName, mimeType);

        Debug.Log($"Uploading file: {fileName}, Type: {furniture_type}, Name: {name}, IsShared: {isShared}");
        Debug.Log($"apiUri : {Utils.Settings.BaseUrl}/api/model3ds/upload?furniture_type={furniture_type}&name={name}&is_shared={isShared.ToString()}");
        using (var request = UnityWebRequest.Post($"{Utils.Settings.BaseUrl}/api/model3ds/upload?furniture_type={furniture_type}&name={name}&is_shared={isShared.ToString()}", form))
        {
            request.SetRequestHeader("Authorization", $"Bearer {JWTToken.Token}");
            request.SetRequestHeader("accept", "*/*");

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"업로드 실패: {request.error} | 상세: {request.downloadHandler.text}");
            }
            return request.responseCode;
        }
    }
}
