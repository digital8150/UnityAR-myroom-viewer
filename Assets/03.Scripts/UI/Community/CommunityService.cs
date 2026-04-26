using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    public int commentCount;
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

// BaseService를 상속받아 중복 코드 완벽 제거!
public class CommunityService : BaseService
{
    public static async Task<(long, string)> GetPostById(int postId)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/posts/{postId}";

        // BaseService의 SendRequest 호출 (GET 방식, 기본 application/json 적용)
        return await SendRequest(url, "GET");
    }

    public static async Task<(long, string)> GetPostsPublic(int page, int size, string sort = "")
    {
        string url = $"{Utils.Settings.BaseUrl}/api/posts/public?page={page}&size={size}&sort={sort}";

        return await SendRequest(url, "GET");
    }
}