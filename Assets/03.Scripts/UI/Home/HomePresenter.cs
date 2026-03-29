using System;
using UnityEngine;

public class HomePresenter
{
    private readonly HomeView _view;

    public HomePresenter(HomeView view)
    {
        _view = view;
    }

    public void OnToGenerate3DClicked()
    {
        Debug.Log("Navigate to 3D Generation Scene");
        Utils.SceneHistory.ChangeScene("Generate3D");
    }

    public void OnToProjectsClicked()
    {
        Debug.Log("Navigate to Projects Scene");
        Utils.SceneHistory.ChangeScene("Projects");
    }

    public void OnToCommunityClicked()
    {
        Debug.Log("Navigate to Community Scene");
        Utils.SceneHistory.ChangeScene("Community");
    }

    public async void InitializeView()
    {
        var (responseCode, jsonBody) = await ProjectsService.GetMemberSearch(
            Int32.Parse(Utils.JWTUtils.GetUserId()), 0, 2, sort: "createdAt,desc");

        if (responseCode == 200 && !string.IsNullOrEmpty(jsonBody))
        {
            try
            {
                ModelSearchResponse data = Newtonsoft.Json.JsonConvert.DeserializeObject<ModelSearchResponse>(jsonBody);
                _view.SetProject1Thumbnail(data.content[0].thumbnailUrl);
                _view.SetProject1Text(data.content[0].name);
                if(_view.Project1Button) _view.Project1Button.onClick.AddListener(() => OnProjectButtonClicked(data.content[0].id));

                _view.SetProject2Thumbnail(data.content[1].thumbnailUrl);
                _view.SetProject2Text(data.content[1].name);
                if (_view.Project2Button) _view.Project2Button.onClick.AddListener(() => OnProjectButtonClicked(data.content[1].id));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    private void OnProjectButtonClicked(int modelId)
    {
        Debug.Log($"[HomePresenter] Move to project Inspect view with modeId : {modelId}");
        ProjectInspectPresenter.SelectedModelId = modelId;
        Utils.SceneHistory.ChangeScene("ProjectInspect");
    }
}