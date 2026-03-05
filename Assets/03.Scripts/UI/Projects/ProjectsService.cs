using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARSubsystems;
using System;
using System.Collections.Generic;

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
