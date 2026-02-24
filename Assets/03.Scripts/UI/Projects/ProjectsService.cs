using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARSubsystems;
using System;
using System.Collections.Generic;

[Serializable]
public class ModelData
{
    public int id;
    public string name;
    public string createdAt;
    public string link;
    public int creatorId;
    public bool is_shared;
    public string description;
    public string thumbnailUrl;
    public bool is_vector_db_trained;
    public string status;
}

[Serializable]
public class PageableInfo
{
    public int offset;
    public int pageNumber;
    public int pageSize;
    public bool paged;
}

[Serializable]
public class ModelSearchResponse
{
    public List<ModelData> content; // 여기에 실제 데이터 리스트가 들어옴!
    public bool last;               // 무한 스크롤 끝인지 확인할 때 필수 ✨
    public int totalElements;
    public int totalPages;
    public int number;              // 현재 페이지 번호
    public int size;
    public bool empty;
    public PageableInfo pageable;
}

public class ProjectsService
{
    public static async Task<(long, string)> GetMemberSearch(int memberId, int page, int size, string sort = "", string name = "")
    {
        long responseCode = 404;

        using (var request = UnityWebRequest.Get($"{Utils.Settings.BaseUrl}/api/model3ds/member/{memberId}/search?name={name}&page={page}&size={size}&sort={sort}"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {JWTToken.Token}");
            request.SetRequestHeader("accept", "application/json");

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

    public static async Task<(long, string)> GetSingleModel3D(int modelId)
    {
        long responseCode = 0;

        using (var request = UnityWebRequest.Get($"{Utils.Settings.BaseUrl}/api/model3ds/{modelId}"))
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
}
