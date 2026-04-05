using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class Generate3DService
{
    public static async Task<(long responseCode, int modelId)> PostUpload(string imagePath, string furniture_type = "table", string name = "테이블", string description = "", bool isShared = true, bool isTakenPicture = false)
    {
        if (!File.Exists(imagePath))
        {
            Debug.LogError($"File not found: {imagePath}");
            return (404, -1);
        }

        byte[] imageBytes;
        string fileName = Path.GetFileName(imagePath);
        string extension = Path.GetExtension(imagePath).ToLower();
        string mimeType = extension == ".png" ? "image/png" : "image/jpeg";

        // --- 회전 문제 해결 로직 추가 ---
        if (isTakenPicture)
        {
            // NativeCamera 기능을 이용해 회전값이 보정된 Texture2D 로드
            // markNonReadable을 false로 해야 인코딩(EncodeToJPG/PNG)이 가능합니다.
            Texture2D texture = NativeCamera.LoadImageAtPath(imagePath, markTextureNonReadable:false);

            if (texture == null)
            {
                Debug.LogError("Failed to load image via NativeCamera.");
                return (500, -1);
            }

            // 보정된 텍스트를 다시 바이너리로 변환 (원본 확장자에 맞춰 변환)
            imageBytes = (extension == ".png") ? texture.EncodeToPNG() : texture.EncodeToJPG();

            // 메모리 해제
            Object.Destroy(texture);
        }
        else
        {
            // 일반 파일인 경우 기존 방식대로 읽기
            imageBytes = await File.ReadAllBytesAsync(imagePath);
        }
        // --------------------------------

        string apiUri = $"{Utils.Settings.BaseUrl}/api/model3ds/upload?furniture_type={furniture_type}&name={name}&is_shared={isShared.ToString().ToLower()}";

        using (var request = new UnityWebRequest(apiUri, "POST"))
        {
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

            string responseText = request.downloadHandler.text;
            int extractedId = -1;

            Match match = Regex.Match(responseText, @"모델 ID:\s*(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
            {
                extractedId = id;
            }

            return (request.responseCode, extractedId);
        }
    }
}