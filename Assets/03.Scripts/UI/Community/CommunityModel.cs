using System;
using System.Collections.Generic;

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
    public List<string> imageUrls;
    public string title;
    public string content;
    public string category; // Enum 처리 권장 (QUESTION, REVIEW, FURNITURE 등)
    public string visibilityScope;
    public int viewCount;
    public int likeCount;
    public int commentCount;
    public bool liked;
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

[Serializable]
public class CreateCommentRequest
{
    public int post_id;
    public string content;
}

[Serializable]
public class CreateCommentWithParentRequest
{
    public int post_id;
    public string content;
    public int parent_comment_id;
}

[Serializable]
public class UpdateCommentRequest
{
    public string content;
}