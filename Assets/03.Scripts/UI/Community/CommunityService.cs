using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class CommunityService : BaseService
{
    /// <summary>
    /// 프로젝트 카드에서 커뮤니티 글쓰기로 이동할 때 모델 ID를 저장합니다.
    /// </summary>
    public static int PendingModel3dId { get; set; } = -1;

    /// <summary>
    /// 대기 중인 모델 ID를 소비하고 반환합니다.
    /// </summary>
    public static int ConsumePendingModel3dId()
    {
        int id = PendingModel3dId;
        PendingModel3dId = -1;
        return id;
    }
    /// <summary>
    /// Fetches a single post by its ID.
    /// </summary>
    /// <param name="postId">The ID of the post to retrieve.</param>
    /// <returns>HTTP status code and response body.</returns>
    public static async Task<(long, string)> GetPostById(int postId)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/posts/{postId}";
        return await SendRequest(url, "GET");
    }

    /// <summary>
    /// Searches posts with pagination and optional filters.
    /// </summary>
    /// <param name="page">Zero-based page index.</param>
    /// <param name="size">Number of posts per page.</param>
    /// <param name="sort">Sort order (e.g. "latest", "popular").</param>
    /// <param name="title">Filter by post title keyword.</param>
    /// <param name="category">Filter by category name.</param>
    /// <param name="myPost">When true, returns only posts by the current user.</param>
    /// <returns>HTTP status code and response body.</returns>
    public static async Task<(long, string)> GetPostsSearch(int page, int size, string sort = "", string title = "", string category = "", bool myPost = false)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/posts/search?page={page}&size={size}&sort={sort}&title={title}&category={category}&myPost={myPost}";
        return await SendRequest(url, "GET");
    }

    /// <summary>
    /// Creates a new community post, optionally attaching up to 4 images and a 3D model reference.
    /// </summary>
    /// <param name="title">Post title.</param>
    /// <param name="content">Post body text.</param>
    /// <param name="category">Post category identifier.</param>
    /// <param name="visibilityScope">Visibility setting (e.g. "PUBLIC", "PRIVATE").</param>
    /// <param name="model3dId">Optional ID of an associated 3D model.</param>
    /// <param name="images">Optional list of image byte arrays (max 4).</param>
    /// <param name="imageFileNames">File names corresponding to each image in <paramref name="images"/>.</param>
    /// <returns>HTTP status code and response body.</returns>
    public static async Task<(long, string)> CreatePost(
        string title, string content, string category, string visibilityScope,
        int? model3dId = null, List<byte[]> images = null, List<string> imageFileNames = null)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/posts" +
            $"?title={Uri.EscapeDataString(title)}" +
            $"&content={Uri.EscapeDataString(content)}" +
            $"&category={category}" +
            $"&visibility_scope={visibilityScope}";

        if (model3dId.HasValue)
            url += $"&model3d_id={model3dId.Value}";

        if (images != null && images.Count > 0)
        {
            var form = new WWWForm();
            int count = Math.Min(images.Count, 4);
            for (int i = 0; i < count; i++)
            {
                string fileName = imageFileNames != null && i < imageFileNames.Count ? imageFileNames[i] : $"image_{i}.jpg";
                form.AddBinaryData("images", images[i], fileName, "image/jpeg");
            }
            return await SendRequest(url, "POST", form, accept: "*/*");
        }

        var (emptyBody, emptyContentType) = BuildEmptyImagesMultipart();
        return await SendRequest(url, "POST", rawBody: emptyBody, contentType: emptyContentType, accept: "*/*");
    }

    /// <summary>
    /// Adds a like from the current user to the specified post.
    /// </summary>
    /// <param name="postId">The ID of the post to like.</param>
    /// <returns>HTTP status code and response body.</returns>
    public static async Task<(long, string)> LikePost(int postId)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/posts/{postId}/likes";
        return await SendRequest(url, "POST");
    }

    /// <summary>
    /// Removes the current user's like from the specified post.
    /// </summary>
    /// <param name="postId">The ID of the post to unlike.</param>
    /// <returns>HTTP status code and response body.</returns>
    public static async Task<(long, string)> UnlikePost(int postId)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/posts/{postId}/likes";
        return await SendRequest(url, "DELETE");
    }

    /// <summary>
    /// Deletes the post with the given ID.
    /// </summary>
    /// <param name="postId">The ID of the post to delete.</param>
    /// <returns>HTTP status code and response body.</returns>
    public static async Task<(long, string)> DeletePost(int postId)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/posts/{postId}";
        return await SendRequest(url, "DELETE");
    }

    /// <summary>
    /// Updates an existing post. Existing images not listed in <paramref name="retainImageUrls"/> are removed.
    /// New images can be supplied alongside retained ones (combined max 4).
    /// </summary>
    /// <param name="postId">The ID of the post to update.</param>
    /// <param name="title">New post title.</param>
    /// <param name="content">New post body text.</param>
    /// <param name="category">New category identifier.</param>
    /// <param name="visibilityScope">New visibility setting.</param>
    /// <param name="model3dId">Optional updated 3D model reference ID.</param>
    /// <param name="retainImageUrls">URLs of existing images to keep after the update.</param>
    /// <param name="images">New image byte arrays to upload (max 4 total with retained).</param>
    /// <param name="imageFileNames">File names corresponding to each entry in <paramref name="images"/>.</param>
    /// <returns>HTTP status code and response body.</returns>
    public static async Task<(long, string)> UpdatePost(
        int postId, string title, string content, string category, string visibilityScope,
        int? model3dId = null, List<string> retainImageUrls = null, List<byte[]> images = null, List<string> imageFileNames = null)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/posts/{postId}" +
            $"?title={Uri.EscapeDataString(title)}" +
            $"&content={Uri.EscapeDataString(content)}" +
            $"&category={category}" +
            $"&visibility_scope={visibilityScope}";

        if (model3dId.HasValue)
            url += $"&model3d_id={model3dId.Value}";

        if (retainImageUrls != null)
            foreach (var imageUrl in retainImageUrls)
                url += $"&retain_image_urls={Uri.EscapeDataString(imageUrl)}";

        if (images != null && images.Count > 0)
        {
            var form = new WWWForm();
            int count = Math.Min(images.Count, 4);
            for (int i = 0; i < count; i++)
            {
                string fileName = imageFileNames != null && i < imageFileNames.Count ? imageFileNames[i] : $"image_{i}.jpg";
                form.AddBinaryData("images", images[i], fileName, "image/jpeg");
            }
            return await SendRequest(url, "PUT", form, accept: "*/*");
        }

        var (emptyBody, emptyContentType) = BuildEmptyImagesMultipart();
        return await SendRequest(url, "PUT", rawBody: emptyBody, contentType: emptyContentType, accept: "*/*");
    }

    // Builds a multipart/form-data body equivalent to curl `-F 'images='`:
    // a single text part named "images" with empty content. Required because
    // Unity's WWWForm switches to application/x-www-form-urlencoded when no
    // binary data is attached (server rejects with 500), and
    // MultipartFormDataSection refuses empty payloads, so we serialize by hand.
    private static (byte[] body, string contentType) BuildEmptyImagesMultipart()
    {
        string boundary = "----UnityFormBoundary" + Guid.NewGuid().ToString("N");
        string body =
            "--" + boundary + "\r\n" +
            "Content-Disposition: form-data; name=\"images\"\r\n" +
            "\r\n" +
            "\r\n" +
            "--" + boundary + "--\r\n";
        return (Encoding.UTF8.GetBytes(body), "multipart/form-data; boundary=" + boundary);
    }

    /// <summary>
    /// Creates a comment on a post, optionally as a reply to an existing comment.
    /// </summary>
    /// <param name="postId">The ID of the post to comment on.</param>
    /// <param name="content">Comment text.</param>
    /// <param name="parentCommentId">ID of the parent comment when creating a reply; null for top-level comments.</param>
    /// <returns>HTTP status code and response body.</returns>
    public static async Task<(long, string)> CreateComment(int postId, string content, int? parentCommentId = null)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/comments";
        string json = parentCommentId.HasValue
            ? JsonUtility.ToJson(new CreateCommentWithParentRequest { post_id = postId, content = content, parent_comment_id = parentCommentId.Value })
            : JsonUtility.ToJson(new CreateCommentRequest { post_id = postId, content = content });
        return await SendRequest(url, "POST", jsonPayload: json);
    }

    /// <summary>
    /// Deletes the comment with the given ID.
    /// </summary>
    /// <param name="commentId">The ID of the comment to delete.</param>
    /// <returns>HTTP status code and response body.</returns>
    public static async Task<(long, string)> DeleteComment(int commentId)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/comments/{commentId}";
        return await SendRequest(url, "DELETE");
    }

    /// <summary>
    /// Updates the text of an existing comment.
    /// </summary>
    /// <param name="commentId">The ID of the comment to update.</param>
    /// <param name="content">New comment text.</param>
    /// <returns>HTTP status code and response body.</returns>
    public static async Task<(long, string)> UpdateComment(int commentId, string content)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/comments/{commentId}";
        var body = new UpdateCommentRequest { content = content };
        return await SendRequest(url, "PUT", jsonPayload: JsonUtility.ToJson(body));
    }

    /// <summary>
    /// Fetches whether the current user has liked the specified post.
    /// </summary>
    /// <param name="postId">The ID of the post.</param>
    /// <returns>HTTP status code and response body containing { "liked": true/false }.</returns>
    public static async Task<(long, string)> GetPostLikedStatus(int postId)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/posts/{postId}/likes/me";
        return await SendRequest(url, "GET");
    }

    /// <summary>
    /// Fetches posts created by the current user with pagination.
    /// </summary>
    public static async Task<(long, string)> GetMyPosts(int page, int size, string sort = "createdAt,desc")
    {
        string url = $"{Utils.Settings.BaseUrl}/api/posts/my?page={page}&size={size}&sort={sort}";
        return await SendRequest(url, "GET");
    }

    /// <summary>
    /// Fetches posts liked by the current user with pagination.
    /// </summary>
    public static async Task<(long, string)> GetMyLikedPosts(int page, int size, string sort = "createdAt,desc")
    {
        string url = $"{Utils.Settings.BaseUrl}/api/posts/likes/my?page={page}&size={size}&sort={sort}";
        return await SendRequest(url, "GET");
    }
}