using System;
using System.Threading.Tasks;
using UnityEngine.Networking;
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

public static class MemberService
{
    public static async Task<(long responseCode, string jsonBody)> GetMemberJSONByMemberId(int memberId)
    {
        using(var request = UnityWebRequest.Get($"{Utils.Settings.BaseUrl}/api/members/{memberId}"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {JWTToken.Token}");
            request.SetRequestHeader("accept", "application/json");

            await request.SendWebRequest();

            return (request.responseCode, request.downloadHandler.text);
        }
    }

    public static async Task<string> GetMemberProfilePicUrlByMemberId(int memberId)
    {
        var(responseCode, jsonBody) = await GetMemberJSONByMemberId(memberId);
        if(responseCode == 200)
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
        var(responseCode, jsonBody) = await GetMemberJSONByMemberId(memberId);
        if(responseCode == 200)
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