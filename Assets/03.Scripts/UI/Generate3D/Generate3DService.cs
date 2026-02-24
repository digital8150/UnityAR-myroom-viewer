using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions; // 추가 필수!
using UnityEngine;
using UnityEngine.Networking;

public class Generate3DService
{
    public static async Task<(long responseCode, int modelId)> PostUpload(string imagePath, string furniture_type = "table", string name = "테이블", string description = "", bool isShared = true)
    {
        if (!File.Exists(imagePath))
        {
            Debug.LogError($"File not found: {imagePath}");
            return (404, -1);
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

        // API URI 구성 (가독성을 위해 변수로 추출)
        string apiUri = $"{Utils.Settings.BaseUrl}/api/model3ds/upload?furniture_type={furniture_type}&name={name}&is_shared={isShared.ToString().ToLower()}";

        using (var request = UnityWebRequest.Post(apiUri, new WWWForm()))
        {
            // BinaryData 추가 (WWWForm을 생성자 대신 Add로 넣어야 안전함)
            var form = new WWWForm();
            form.AddBinaryData("image", imageBytes, fileName, mimeType);
            request.uploadHandler = new UploadHandlerRaw(form.data);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", form.headers["Content-Type"]);

            request.SetRequestHeader("Authorization", $"Bearer {JWTToken.Token}");
            request.SetRequestHeader("accept", "*/*");

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"업로드 실패: {request.error} | 상세: {request.downloadHandler.text}");
                return (request.responseCode, -1);
            }

            // --- 모델 ID 추출 로직 ---
            string responseText = request.downloadHandler.text;
            int extractedId = -1;

            // 정규식: "모델 ID: " 뒤에 오는 숫자를 찾음
            Match match = Regex.Match(responseText, @"모델 ID:\s*(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
            {
                extractedId = id;
            }
            else
            {
                Debug.LogWarning($"ID 추출 실패. 응답 텍스트: {responseText}");
            }

            return (request.responseCode, extractedId);
        }
    }
}