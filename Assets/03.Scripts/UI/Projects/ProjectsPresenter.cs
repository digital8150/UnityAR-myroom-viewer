using Newtonsoft.Json;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProjectsPresenter : IDisposable
{
    private const int VIEW_PER_PAGE = 11;
    private ProjectsView _view;
    private int _pageIndex = 0;

    private bool _isLastPage = false;
    private bool _isLoading = false;
    private string _sortBy = "id";

    public ProjectsPresenter(ProjectsView view)
    {
        _view = view;
        WebsocketController.Instance.OnModel3DGenerated += HandleModelGenerated;
        WebsocketController.Instance.OnModel3DGenerateFailed += HandleModelGenerated;
    }

    public void Dispose()
    {
        WebsocketController.Instance.OnModel3DGenerated -= HandleModelGenerated;
        WebsocketController.Instance.OnModel3DGenerateFailed -= HandleModelGenerated;
    }

    public void StartUp()
    {
        _sortBy = "createdAt,desc";
        LoadPage();
    }

    public async void LoadPage()
    {
        if (_isLoading || _isLastPage) return;
        Debug.Log($"Projects View : Loading Page {_pageIndex}");

        _isLoading = true;

        long responseCode;
        string jsonBody;

        (responseCode, jsonBody) = await ProjectsService.GetMemberSearch(
            Int32.Parse(Utils.JWTUtils.GetUserId()), _pageIndex, VIEW_PER_PAGE, sort:_sortBy);

        if (responseCode == 200 && !string.IsNullOrEmpty(jsonBody))
        {
            try
            {
                ModelSearchResponse data = Newtonsoft.Json.JsonConvert.DeserializeObject<ModelSearchResponse>(jsonBody);
                foreach (var item in data.content)
                {
                    var itemView = _view.UpdateOrAddViewItem(
                        null,
                        item.name,
                        item.id,
                        TranslateStatus(item.status),
                        () => OnButtonClicked(item.id));

                    LoadThumbnailAsync(itemView, item.thumbnailUrl);
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
        Utils.SceneHistory.MarkCurrentScene();
        SceneManager.LoadScene("Generate3D");
    }

    private async void LoadThumbnailAsync(ViewSlotsView itemView, string thumbnailUrl)
    {
        try
        {
            Sprite thumbnailSprite = await Utils.ImageUtils.LoadSpriteFromUrl(Utils.Settings.ReplaceLocalhost(thumbnailUrl));
            if(itemView && thumbnailSprite)
            {
                itemView.UpdateThumbnailImage(thumbnailSprite);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
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

    private void HandleModelGenerated(string websocketResponse)
    {
        try
        {
            ModelGenerationResponse modelGenerationResponse = JsonConvert.DeserializeObject<ModelGenerationResponse>(websocketResponse);
            RefreshView();
        }
        catch
        {

        }
    }

    private void RefreshView()
    {
        _pageIndex = 0;
        _isLastPage = false;
        _view.ClearViewItems();
        LoadPage();
    }

    private void OnButtonClicked(int modelId)
    {
        ProjectInspectPresenter.SelectedModelId = modelId;
        Utils.SceneHistory.MarkCurrentScene();
        SceneManager.LoadScene("ProjectInspect");
    }
}