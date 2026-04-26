using System.Threading.Tasks;
using System.IO;
using UnityEngine;

public class RoomPlanService : BaseService
{
    private static readonly string APIUrl = "http://127.0.0.1:5000";

    public static async Task<(long responseCode, string jsonBody)> PostUpload(string imagePath)
    {
        if(!File.Exists(imagePath))
        {
            Debug.LogError("$FileNotFound");
            return (404, null);
        }

        byte[] imageBytes = File.ReadAllBytes(imagePath);
        string fileName = Path.GetFileName(imagePath);
        string extension = Path.GetExtension(imagePath).ToLower();
        string mimeType = extension == ".png" ? "image/png" : "image/jpeg";

        var form = new WWWForm();
        form.AddBinaryData("image", imageBytes, fileName, mimeType);

        var(responseCode, responseText) = await SendRequest(APIUrl, "POST", form);
        if(responseCode < 200 || responseCode >= 300)
        {
            Debug.LogError($"업로드 실패 상세: {responseText}");
            return (responseCode, null);
        }
        return (responseCode, responseText);
    }
}
