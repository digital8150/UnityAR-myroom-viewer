using System.Threading.Tasks;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Utils;

public class RoomPlanService : BaseService
{
    private static string BaseUrl => $"{Settings.BaseUrl}/api/room3d";

    // 1. Room3D 단건 조회 - GET /api/room3d/{room3dId}
    public static async Task<(long responseCode, string jsonBody)> GetRoom3D(long room3dId)
    {
        return await SendRequest($"{BaseUrl}/{room3dId}", "GET");
    }

    // 2. Room3D 수정 - PUT /api/room3d/{room3dId}
    public static async Task<(long responseCode, string jsonBody)> UpdateRoom3D(
        long room3dId,
        string roomName = null,
        string description = null,
        string xmlFilePath = null)
    {
        string url = BuildQueryUrl($"{BaseUrl}/{room3dId}", roomName, description);

        bool hasXml = !string.IsNullOrEmpty(xmlFilePath) && File.Exists(xmlFilePath);
        if (hasXml)
        {
            var form = new WWWForm();
            byte[] xmlBytes = File.ReadAllBytes(xmlFilePath);
            form.AddBinaryData("xml_file", xmlBytes, Path.GetFileName(xmlFilePath), "application/xml");
            return await SendRequest(url, "PUT", form);
        }

        return await SendRequest(url, "PUT");
    }

    // 3. Room3D 삭제 - DELETE /api/room3d/{room3dId}
    public static async Task<(long responseCode, string jsonBody)> DeleteRoom3D(long room3dId)
    {
        return await SendRequest($"{BaseUrl}/{room3dId}", "DELETE");
    }

    // 4. 도면 이미지 업로드 및 Room3D 생성 - POST /api/room3d
    public static async Task<(long responseCode, string jsonBody)> CreateRoom3DFromImage(
        string imagePath,
        string roomName,
        string description = null)
    {
        if (!File.Exists(imagePath))
        {
            Debug.LogError($"FileNotFound: {imagePath}");
            return (404, null);
        }

        string url = BuildQueryUrl(BaseUrl, roomName, description);

        string ext = Path.GetExtension(imagePath).ToLower();
        string mimeType = ext == ".png" ? "image/png" : "image/jpeg";

        var form = new WWWForm();
        form.AddBinaryData("image", File.ReadAllBytes(imagePath), Path.GetFileName(imagePath), mimeType);

        var (code, body) = await SendRequest(url, "POST", form);
        if (code < 200 || code >= 300)
            Debug.LogError($"이미지 업로드 실패 ({code}): {body}");
        return (code, body);
    }

    // 5. XML 파일 업로드 및 Room3D 생성 - POST /api/room3d/xml
    public static async Task<(long responseCode, string jsonBody)> CreateRoom3DFromXml(
        string xmlFilePath,
        string roomName,
        string description = null)
    {
        if (!File.Exists(xmlFilePath))
        {
            Debug.LogError($"FileNotFound: {xmlFilePath}");
            return (404, null);
        }

        string url = BuildQueryUrl($"{BaseUrl}/xml", roomName, description);

        var form = new WWWForm();
        form.AddBinaryData("xml_file", File.ReadAllBytes(xmlFilePath), Path.GetFileName(xmlFilePath), "application/xml");

        var (code, body) = await SendRequest(url, "POST", form);
        if (code < 200 || code >= 300)
            Debug.LogError($"XML 업로드 실패 ({code}): {body}");
        return (code, body);
    }

    // 6. 내 Room3D 목록 조회 - GET /api/room3d/my
    public static async Task<(long responseCode, string jsonBody)> GetMyRoom3DList(
        int page = 0,
        int size = 10,
        string sort = "id,DESC")
    {
        string url = $"{BaseUrl}/my?page={page}&size={size}&sort={UnityWebRequest.EscapeURL(sort)}";
        return await SendRequest(url, "GET");
    }

    private static string BuildQueryUrl(string baseUrl, string roomName, string description)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(roomName))
            sb.Append($"room_name={UnityWebRequest.EscapeURL(roomName)}&");
        if (!string.IsNullOrEmpty(description))
            sb.Append($"description={UnityWebRequest.EscapeURL(description)}&");

        if (sb.Length == 0) return baseUrl;
        sb.Length--; // trailing '&' 제거
        return $"{baseUrl}?{sb}";
    }
}
