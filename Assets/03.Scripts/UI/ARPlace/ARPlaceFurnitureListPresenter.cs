using System;
using Newtonsoft.Json;
using UnityEngine;

public class ARPlaceFurnitureListPresenter
{
    private const int ITEMS_PER_PAGE = 10;

    private enum FurnitureTab { My, Shared }

    private readonly ARPlaceFurnitureListView _view;
    private ARPlaceCore _core;

    private FurnitureTab _currentTab = FurnitureTab.My;
    private int _pageIndex;
    private bool _isLastPage;
    private bool _isLoading;
    private readonly string _sortBy = "createdAt,desc";

    public ARPlaceFurnitureListPresenter(ARPlaceFurnitureListView view)
    {
        _view = view;
    }

    public void Bind(ARPlaceCore core)
    {
        _core = core;
    }

    public void OnOpened()
    {
        if (_view == null) return;
        ResetAndLoad();
    }

    public void OnMyTabClicked()
    {
        if (_currentTab == FurnitureTab.My) return;
        _currentTab = FurnitureTab.My;
        _view.SetTabVisualActive(true);
        ResetAndLoad();
    }

    public void OnSharedTabClicked()
    {
        if (_currentTab == FurnitureTab.Shared) return;
        _currentTab = FurnitureTab.Shared;
        _view.SetTabVisualActive(false);
        ResetAndLoad();
    }

    public async void LoadPage()
    {
        if (_isLoading || _isLastPage) return;
        _isLoading = true;

        try
        {
            long responseCode;
            string jsonBody;

            if (_currentTab == FurnitureTab.My)
            {
                if (!int.TryParse(Utils.JWTUtils.GetUserId(), out int memberId))
                {
                    Debug.LogError("[ARPlaceFurnitureListPresenter] 사용자 ID 파싱 실패");
                    return;
                }
                (responseCode, jsonBody) = await ProjectsService.GetMemberSearch(memberId, _pageIndex, ITEMS_PER_PAGE, sort: _sortBy);
            }
            else
            {
                (responseCode, jsonBody) = await GalleryService.GetSharedSearch(_pageIndex, ITEMS_PER_PAGE, sort: _sortBy);
            }

            if (responseCode == 200 && !string.IsNullOrEmpty(jsonBody))
            {
                var data = JsonConvert.DeserializeObject<ModelSearchResponse>(jsonBody);
                if (data?.content != null)
                {
                    foreach (var modelData in data.content)
                    {
                        if (_currentTab == FurnitureTab.My && modelData.status != "SUCCESS") continue;
                        _view.AddItem(modelData);
                    }
                    _isLastPage = data.last;
                    _pageIndex++;
                }
            }
            else
            {
                Debug.LogError($"[ARPlaceFurnitureListPresenter] Load failed. code={responseCode}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            _isLoading = false;
        }
    }

    public async void OnItemSelected(int modelId)
    {
        if (_core == null)
        {
            Debug.LogError("[ARPlaceFurnitureListPresenter] ARPlaceCore is not bound.");
            return;
        }

        PopupView.Instance.SetLoadingPannelActive(true);
        try
        {
            ModelData modelData = await ModelService.GetModelDataByModelId(modelId);
            if (modelData == null)
            {
                PopupView.Instance.ShowMessage("모델 데이터를 불러오는 데 실패했습니다.");
                return;
            }

            var (fileCode, modelPath) = await ProjectInspectService.GetModel3DFile(modelData.link);
            if (fileCode != 200 || string.IsNullOrEmpty(modelPath))
            {
                PopupView.Instance.ShowMessage("모델 파일을 불러오는 데 실패했습니다.");
                return;
            }

            ModelDimension dimension;
            var (dimCode, dimJson) = await ProjectInspectService.GetModel3DDimension(modelData.id);
            dimension = dimCode == 200 && !string.IsNullOrEmpty(dimJson)
                ? JsonConvert.DeserializeObject<ModelDimension>(dimJson)
                : new ModelDimension();

            _view.Close();
            _core.PlaceFurnitureFromCatalogue(modelPath, dimension);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            PopupView.Instance.ShowMessage("가구를 불러오는 중 오류가 발생했습니다.");
        }
        finally
        {
            PopupView.Instance.SetLoadingPannelActive(false);
        }
    }

    private void ResetAndLoad()
    {
        _pageIndex = 0;
        _isLastPage = false;
        _isLoading = false;
        _view.ClearItems();
        LoadPage();
    }
}
