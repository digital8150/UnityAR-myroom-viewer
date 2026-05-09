using System.Collections.Generic;
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

        if (isTakenPicture)
        {
            Texture2D texture = NativeCamera.LoadImageAtPath(imagePath, markTextureNonReadable: false);
            if (texture == null)
            {
                Debug.LogError("Failed to load image via NativeCamera.");
                return (500, -1);
            }
            imageBytes = extension == ".png" ? texture.EncodeToPNG() : texture.EncodeToJPG();
            Object.Destroy(texture);
        }
        else
        {
            imageBytes = await File.ReadAllBytesAsync(imagePath);
        }

        string apiUri = $"{Utils.Settings.BaseUrl}/api/model3ds/upload?furniture_type={furniture_type}&name={name}&is_shared={isShared.ToString().ToLower()}";
        var form = new WWWForm();
        form.AddBinaryData("image", imageBytes, fileName, mimeType);

        var (responseCode, responseText) = await SendRequest(apiUri, "POST", form, "*/*");

        if (responseCode < 200 || responseCode >= 300)
        {
            Debug.LogError($"업로드 실패 상세: {responseText}");
            return (responseCode, -1);
        }

        return (responseCode, ExtractModelId(responseText));
    }

    // Multi-view upload: requires 2–4 images.
    public static async Task<(long responseCode, int modelId)> PostUploadMulti(IReadOnlyList<string> imagePaths, string furniture_type = "table", string name = "테이블", string description = "", bool isShared = true, bool isTakenPicture = false)
    {
        if (imagePaths == null || imagePaths.Count < 2 || imagePaths.Count > 4)
        {
            Debug.LogError($"PostUploadMulti requires 2–4 images, got {imagePaths?.Count ?? 0}.");
            return (400, -1);
        }

        string apiUri = $"{Utils.Settings.BaseUrl}/api/model3ds/upload-multi?furniture_type={furniture_type}&name={name}&is_shared={isShared.ToString().ToLower()}";
        var form = new WWWForm();

        foreach (string imagePath in imagePaths)
        {
            if (!File.Exists(imagePath))
            {
                Debug.LogError($"File not found: {imagePath}");
                return (404, -1);
            }

            string fileName = Path.GetFileName(imagePath);
            string extension = Path.GetExtension(imagePath).ToLower();
            string mimeType = extension == ".png" ? "image/png" : "image/jpeg";
            byte[] imageBytes;

            if (isTakenPicture)
            {
                Texture2D texture = NativeCamera.LoadImageAtPath(imagePath, markTextureNonReadable: false);
                if (texture == null)
                {
                    Debug.LogError($"Failed to load image via NativeCamera: {imagePath}");
                    return (500, -1);
                }
                imageBytes = extension == ".png" ? texture.EncodeToPNG() : texture.EncodeToJPG();
                Object.Destroy(texture);
            }
            else
            {
                imageBytes = await File.ReadAllBytesAsync(imagePath);
            }

            form.AddBinaryData("images", imageBytes, fileName, mimeType);
        }

        var (responseCode, responseText) = await SendRequest(apiUri, "POST", form, "*/*");

        if (responseCode < 200 || responseCode >= 300)
        {
            Debug.LogError($"멀티뷰 업로드 실패 상세: {responseText}");
            return (responseCode, -1);
        }

        return (responseCode, ExtractModelId(responseText));
    }

    private static int ExtractModelId(string responseText)
    {
        Match match = Regex.Match(responseText, @"모델 ID:\s*(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
            return id;
        return -1;
    }
}
