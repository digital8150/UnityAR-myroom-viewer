using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProjectsPresenter
{
    private const int VIEW_PER_PAGE = 11;
    private IProjectsView _view;
    private int _pageIndex = 0;

    private bool _isLastPage = false;
    private bool _isLoading = false;

    public ProjectsPresenter(IProjectsView view)
    {
        _view = view;
    }

    public async void StartUp()
    {
        if(Generate3DPresenter.GenerateProcessingModelID == -1)
        {
            LoadPage();
            return;
        }

        long responseCode;
        string jsonBody;
        (responseCode, jsonBody) = await ProjectsService.GetSingleModel3D(Generate3DPresenter.GenerateProcessingModelID);

        try
        {
            if (responseCode == 200 && !string.IsNullOrEmpty(jsonBody))
            {
                ModelData data = Newtonsoft.Json.JsonConvert.DeserializeObject<ModelData>(jsonBody);
                _view.UpdateOrAddViewItem(
                    await Utils.ImageUtils.LoadSpriteFromUrl(Utils.Settings.ReplaceLocalhost(data.thumbnailUrl)),
                    data.name,
                    data.id,
                    TranslateStatus(data.status));
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            LoadPage();
        }
    }

    public async void LoadPage()
    {
        if (_isLoading || _isLastPage) return;
        Debug.Log($"Projects View : Loading Page {_pageIndex}");

        _isLoading = true;

        long responseCode;
        string jsonBody;

        (responseCode, jsonBody) = await ProjectsService.GetMemberSearch(
            Int32.Parse(Utils.JWTUtils.GetUserId()), _pageIndex, VIEW_PER_PAGE);

        if (responseCode == 200 && !string.IsNullOrEmpty(jsonBody))
        {
            try
            {
                ModelSearchResponse data = Newtonsoft.Json.JsonConvert.DeserializeObject<ModelSearchResponse>(jsonBody);
                foreach (var item in data.content)
                {
                    _view.UpdateOrAddViewItem(
                        await Utils.ImageUtils.LoadSpriteFromUrl(Utils.Settings.ReplaceLocalhost(item.thumbnailUrl)),
                        item.name,
                        item.id,
                        TranslateStatus(item.status));
                }

                _isLastPage = data.last;
                _pageIndex++;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        _isLoading = false;
    }

    public void ToGenerate3DClicked()
    {
        SceneManager.LoadScene("Generate3D");
    }

    private string TranslateStatus(string status)
    {
        switch (status)
        {
            case "SUCCESS":
                return "";
            case "PROCESSING":
                return "처리중";
            case "FAILED":
                return "실패";
            default:
                Debug.LogError($"[ProjectsPresenter.cs] Unknown Status String : {status}");
                return "";
        }
    }
}