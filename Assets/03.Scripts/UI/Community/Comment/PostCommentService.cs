using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class CommentDto
{
    public int id;                 // 댓글 고유 ID
    public int memberId;           // 작성자 회원 번호
    public string memberName;      // 작성자 이름 (최고관리자, 한재상 등)
    public int postId;             // 게시글 ID
    public string content;         // 댓글 내용
    public string createdAt;       // 생성 일시 (ISO 8601 포맷)
    public string updatedAt;       // 수정 일시
    public int? parentCommentId;   // 부모 댓글 ID (null이면 최상위 댓글)
}

public static class PostCommentService
{
    public static async Task<(long responeCode, string jsonBody)> GetPostCommentsById(int postId)
    {
        long responseCode = 404;
        using (var request = UnityWebRequest.Get($"{Utils.Settings.BaseUrl}/api/comments/post/{postId}"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {JWTToken.Token}");
            request.SetRequestHeader("accept", "application/json");

            await request.SendWebRequest();

            responseCode = request.responseCode;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Request Failed : {request.error} | Details: {request.downloadHandler.text}");
                return (responseCode, string.Empty);
            }

            string body = request.downloadHandler.text;
            return (responseCode, body);
        }
    }
}