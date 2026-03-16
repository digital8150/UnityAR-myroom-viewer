using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class PostResponse
{
    public List<PostContent> content;
    public bool empty;
    public bool first;
    public bool last;
    public int number;
    public int numberOfElements;
    public Pageable pageable;
    public int size;
    public Sort sort;
    public int totalElements;
    public int totalPages;
}

[Serializable]
public class PostContent
{
    public int id;
    public int memberId;
    public string memberName;
    public int? model3dId; // null 허용
    public string model3dName;
    public string imageUrl;
    public string title;
    public string content;
    public string category; // Enum 처리 권장 (QUESTION, REVIEW, FURNITURE 등)
    public string visibilityScope;
    public int viewCount;
    public int likeCount;
    public string createdAt;
    public string updatedAt;
}

[Serializable]
public class Pageable
{
    public int offset;
    public int pageNumber;
    public int pageSize;
    public bool paged;
    public Sort sort;
    public bool unpaged;
}

[Serializable]
public class Sort
{
    public bool empty;
    public bool sorted;
    public bool unsorted;
}

public class CommunityService
{
    public static async Task<(long, string)> GetPostById(int postId)
    {
        long responseCode = 404;
        using (var request = UnityEngine.Networking.UnityWebRequest.Get($"{Utils.Settings.BaseUrl}/api/posts/{postId}"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {JWTToken.Token}");
            request.SetRequestHeader("accept", "application/json");

            await request.SendWebRequest();

            responseCode = request.responseCode;

            if(request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Request Failed : {request.error} | Details: {request.downloadHandler.text}");
                return (responseCode, string.Empty);
            }

            string body = request.downloadHandler.text;
            return(responseCode, body);
        }
    }

    public static async Task<(long, string)> GetPostsPublic(int page, int size, string sort = "")
    {
        long responseCode = 404;
        using (var request = UnityEngine.Networking.UnityWebRequest.Get($"{Utils.Settings.BaseUrl}/api/posts/public?page={page}&size={size}&sort={sort}"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {JWTToken.Token}");
            request.SetRequestHeader("accept", "application/json");

            await request.SendWebRequest();

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
