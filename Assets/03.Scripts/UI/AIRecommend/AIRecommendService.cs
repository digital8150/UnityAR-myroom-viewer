using System.IO;
using System.Threading.Tasks;
using UnityEngine;

// static class에서 class로 변경하여 상속 가능하게 처리 (메서드는 static 유지)
public class AIRecommendService : BaseService
{
    public static async Task<(long, string)> PostRecommends(string category, int topK, string imagePath, bool isTakenPicture = false)
    {
        if (!File.Exists(imagePath))
        {
            Debug.LogError($"File not found: {imagePath}");
            return (404, "-1");
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
                return (500, "-1");
            }

            imageBytes = (extension == ".png") ? texture.EncodeToPNG() : texture.EncodeToJPG();
            Object.Destroy(texture);
        }
        else
        {
            imageBytes = await File.ReadAllBytesAsync(imagePath);
        }
        // --------------------------------

        string apiUri = $"{Utils.Settings.BaseUrl}/api/recommands/request?category={category}&topK={topK}";

        var form = new WWWForm();
        form.AddBinaryData("image", imageBytes, fileName, mimeType);

        // 확장된 BaseService의 SendRequest 활용 (토큰 리프레시 로직까지 자동 적용됨!)
        return await SendRequest(apiUri, "POST", form, "text/plain");
    }
}