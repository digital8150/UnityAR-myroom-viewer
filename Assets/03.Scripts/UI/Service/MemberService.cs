using System;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public class MemberDto
{
    public int id;
    public string username;
    public string email;
    public string profileImageUrl;
}

// BaseService 상속을 위해 일반 class로 변경 (메서드는 static 유지)
public class MemberService : BaseService
{
    public static async Task<(long responseCode, string jsonBody)> GetMemberJSONByMemberId(int memberId)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/members/{memberId}";

        // BaseService의 SendRequest로 통일 (토큰 리프레시 자동 적용)
        return await SendRequest(url, "GET");
    }

    public static async Task<string> GetMemberProfilePicUrlByMemberId(int memberId)
    {
        var (responseCode, jsonBody) = await GetMemberJSONByMemberId(memberId);

        if (responseCode == 200)
        {
            try
            {
                MemberDto member = JsonConvert.DeserializeObject<MemberDto>(jsonBody);
                return member.profileImageUrl;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        return null;
    }

    public static async Task<string> GetMemberUsernameByMemberId(int memberId)
    {
        var (responseCode, jsonBody) = await GetMemberJSONByMemberId(memberId);

        if (responseCode == 200)
        {
            try
            {
                MemberDto member = JsonConvert.DeserializeObject<MemberDto>(jsonBody);
                return member.username;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        return null;
    }
}