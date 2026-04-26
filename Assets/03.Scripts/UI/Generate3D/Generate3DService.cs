using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

public class Generate3DService : BaseService
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

        // --- 회전 문제 해결 로직 ---
        if (isTakenPicture)
        {
            Texture2D texture = NativeCamera.LoadImageAtPath(imagePath, markTextureNonReadable: false);

            if (texture == null)
            {
                Debug.LogError("Failed to load image via NativeCamera.");
                return (500, -1);
            }

            imageBytes = (extension == ".png") ? texture.EncodeToPNG() : texture.EncodeToJPG();
            Object.Destroy(texture);
        }
        else
        {
            imageBytes = await File.ReadAllBytesAsync(imagePath);
        }
        // --------------------------------

        string apiUri = $"{Utils.Settings.BaseUrl}/api/model3ds/upload?furniture_type={furniture_type}&name={name}&is_shared={isShared.ToString().ToLower()}";

        var form = new WWWForm();
        form.AddBinaryData("image", imageBytes, fileName, mimeType);

        // BaseService의 SendRequest 활용 (accept 헤더를 "*/*"로 덮어씌움)
        var (responseCode, responseText) = await SendRequest(apiUri, "POST", form, "*/*");

        // 실패 시 처리
        if (responseCode < 200 || responseCode >= 300)
        {
            Debug.LogError($"업로드 실패 상세: {responseText}");
            return (responseCode, -1);
        }

        int extractedId = -1;
        Match match = Regex.Match(responseText, @"모델 ID:\s*(\d+)");

        if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
        {
            extractedId = id;
        }

        return (responseCode, extractedId);
    }
}