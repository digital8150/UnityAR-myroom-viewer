using Newtonsoft.Json;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI.ProceduralImage;

public class ProjectsPresenter : IDisposable
{
    private const int VIEW_PER_PAGE = 11;
    private ProjectsView _view;
    private int _pageIndex = 0;

    private bool _isLastPage = false;
    private bool _isLoading = false;
    private string _sortBy = "id";
    private string _filterByName = "";

    public ProjectsPresenter(ProjectsView view)
    {
        _view = view;
        _view.SetFilterPannelActive(false);
        WebsocketController.Instance.OnModel3DGenerated += HandleModelGenerated;
        WebsocketController.Instance.OnModel3DGenerateFailed += HandleModelGenerated;
    }

    #region Button Handlers
    public void OnShowFilterClicked()
    {
        _view.SetFilterPannelActive(true);
    }

    public void OnLatestButtonClicked(ProceduralImage image, TextMeshProUGUI text)
    {
        _view.SetActiveButtonColor(image, text);
        _sortBy = "createdAt,desc";
    }

    public void OnOldestButtonClicked(ProceduralImage image, TextMeshProUGUI text)
    {
        _view.SetActiveButtonColor(image, text);
        _sortBy = "createdAt,asc";
    }

    public void OnResetFilterButtonClicked()
    {
        _sortBy = "createdAt,desc";
        _filterByName = "";
        RefreshView();
        _view.SetFilterPannelActive(false);
        _view.SetNameFilterInput(string.Empty);
    }

    public void OnApplyFilterButtonClicked()
    {
        _filterByName = _view.GetNameFilterInput();
        RefreshView();
        _view.SetFilterPannelActive(false);
    }
    #endregion

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
            Int32.Parse(Utils.JWTUtils.GetUserId()), _pageIndex, VIEW_PER_PAGE, sort:_sortBy, name: _filterByName);

        if (responseCode == 200 && !string.IsNullOrEmpty(jsonBody))
        {
            try
            {
                ModelSearchResponse data = Newtonsoft.Json.JsonConvert.DeserializeObject<ModelSearchResponse>(jsonBody);
                foreach (var item in data.content)
                {
                    var capturedId = item.id;
                    var itemView = _view.UpdateOrAddViewItem(
                        null,
                        item.name,
                        item.id,
                        TranslateStatus(item.status),
                        () => OnButtonClicked(capturedId),
                        () => OnSlotLongPressed(capturedId));

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
        Utils.SceneHistory.ChangeScene("Generate3D");
    }

    private async void LoadThumbnailAsync(ViewSlotsView itemView, string thumbnailUrl)
    {
        try
        {
            Sprite thumbnailSprite = await Utils.ImageUtils.LoadSpriteFromUrlAsync(Utils.Settings.ReplaceLocalhost(thumbnailUrl));
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
            
        }
        catch
        {

        }finally
        {
            RefreshView();
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
        Utils.SceneHistory.ChangeScene("ProjectInspect");
    }

    private void OnSlotLongPressed(int modelId)
    {
        PopupView.Instance.Presenter.ShowYesNo(
            "이 모델을 삭제하시겠습니까?",
            () => DeleteModel(modelId));
    }

    private async void DeleteModel(int modelId)
    {
        PopupView.Instance.SetLoadingPannelActive(true);
        long code = await ProjectInspectService.DeleteModel3D(modelId);
        PopupView.Instance.SetLoadingPannelActive(false);

        if (code == 200 || code == 204)
        {
            PopupView.AddPopup(new PopupContext("모델이 삭제되었습니다.", PopupView.Instance.GreenCheckCircle));
            RefreshView();
        }
        else
        {
            PopupView.Instance.ShowMessage($"모델 삭제에 실패했습니다. (code: {code})");
        }
    }
}