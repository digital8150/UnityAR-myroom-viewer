using Newtonsoft.Json;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI.ProceduralImage;

enum GalleryPage
{
    List,
    Inspect
}

public class GalleryPresenter : IDisposable
{
    private const int ITEMS_PER_PAGE = 10;

    /// <summary>
    /// Gallery 씬에 진입할 때 특정 모델을 Inspect 페이지로 바로 열기 위한 모델 ID
    /// </summary>
    public static int? SelectedModelId = null;

    private GalleryView _view;
    private int _pageIndex = 0;

    private bool _isLastPage = false;
    private bool _isLoading = false;
    private GalleryPage _currentPage = GalleryPage.List;

    private string _sortBy = "createdAt,desc";
    private string _filterByName = "";

    public GalleryPresenter(GalleryView view)
    {
        _view = view;
    }

    public void Dispose()
    {
        // 필요하면 리소스 정리
    }

    /// <summary>
    /// 갤러리를 초기화하고 첫 페이지를 로드합니다.
    /// SelectedModelId가 설정되어 있으면 해당 모델을 직접 엽니다.
    /// </summary>
    public void Initialize()
    {
        _view.SetFilterPannelActive(false);
        _pageIndex = 0;
        _isLastPage = false;
        _isLoading = false;
        _view.ClearGalleryItems();

        // 특정 모델 ID가 지정된 경우 해당 모델을 직접 오픈
        if (SelectedModelId.HasValue)
        {
            int modelId = SelectedModelId.Value;
            SelectedModelId = null; // 사용한 후 초기화
            OnItemInspectClicked(modelId);
        }
        else
        {
            _view.ShowGalleryPage();
            LoadPage();
        }
    }

    /// <summary>
    /// 다음 페이지를 로드합니다. (무한 스크롤용)
    /// </summary>
    public async void LoadPage()
    {
        if (_isLoading || _isLastPage) return;

        Debug.Log($"[GalleryPresenter] Loading page {_pageIndex}");
        _isLoading = true;

        try
        {
            var (responseCode, jsonBody) = await GalleryService.GetSharedSearch(
                page: _pageIndex,
                size: ITEMS_PER_PAGE,
                sort: _sortBy,
                name: _filterByName
            );

            if (responseCode == 200 && !string.IsNullOrEmpty(jsonBody))
            {
                ModelSearchResponse data = JsonConvert.DeserializeObject<ModelSearchResponse>(jsonBody);

                if (data != null && data.content != null)
                {
                    foreach (var modelData in data.content)
                    {
                        _view.AddGalleryItem(modelData);
                    }

                    _isLastPage = data.last;
                    _pageIndex++;

                    Debug.Log($"[GalleryPresenter] Loaded page {_pageIndex - 1}, IsLastPage: {_isLastPage}");
                }
            }
            else
            {
                Debug.LogError($"[GalleryPresenter] Failed to load gallery. ResponseCode: {responseCode}");
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

    #region Button Actions
    /// <summary>
    /// 아이템의 검사 버튼 클릭 처리
    /// </summary>
    public async void OnItemInspectClicked(int modelId)
    {
        Debug.Log($"[GalleryPresenter] Inspect clicked for model {modelId}");
        PopupView.Instance.SetLoadingPannelActive(true);

        try
        {
            ModelData modelData = await ModelService.GetModelDataByModelId(modelId);
            if (modelData == null)
            {
                PopupView.Instance.ShowMessage("모델 데이터를 불러오는 데 실패했습니다.");
                PopupView.Instance.SetLoadingPannelActive(false);
                return;
            }

            var (responseCode, modelPath) = await ProjectInspectService.GetModel3DFile(modelData.link);
            if (responseCode != 200)
            {
                Debug.LogError($"[GalleryPresenter] Failed to get model. Code: {responseCode}");
                PopupView.Instance.ShowMessage("모델 파일을 불러오는 데 실패했습니다.");
                PopupView.Instance.SetLoadingPannelActive(false);
                return;
            }

            var (dimResponseCode, modelDimensionResponse) = await ProjectInspectService.GetModel3DDimension(modelData.id);
            ModelDimension modelDimension;
            if (dimResponseCode == 200)
            {
                modelDimension = JsonConvert.DeserializeObject<ModelDimension>(modelDimensionResponse);
            }
            else
            {
                modelDimension = new ModelDimension();
            }

            _view.InspectView.SetContent(modelDimension, modelData);
            _view.InspectView.SpawnModel3D(modelPath);
            _view.ShowInspectPage();
            _currentPage = GalleryPage.Inspect;

            PopupView.Instance.SetLoadingPannelActive(false);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            PopupView.Instance.ShowMessage("상세 보기 로드 중 오류가 발생했습니다.");
            PopupView.Instance.SetLoadingPannelActive(false);
        }
    }

    /// <summary>
    /// 아이템의 AR 버튼 클릭 처리
    /// </summary>
    public async void OnItemARClicked(int modelId)
    {
        Debug.Log($"[GalleryPresenter] AR clicked for model {modelId}");
        PopupView.Instance.SetLoadingPannelActive(true);

        try
        {
            ModelData modelData = await ModelService.GetModelDataByModelId(modelId);
            if (modelData == null)
            {
                PopupView.Instance.ShowMessage("모델 데이터를 불러오는 데 실패했습니다.");
                PopupView.Instance.SetLoadingPannelActive(false);
                return;
            }

            var (responseCode, modelPath) = await ProjectInspectService.GetModel3DFile(modelData.link);
            if (responseCode != 200)
            {
                Debug.LogError($"[GalleryPresenter] Failed to get model. Code: {responseCode}");
                PopupView.Instance.ShowMessage("모델 파일을 불러오는 데 실패했습니다.");
                PopupView.Instance.SetLoadingPannelActive(false);
                return;
            }

            var (dimResponseCode, modelDimensionResponse) = await ProjectInspectService.GetModel3DDimension(modelData.id);
            ModelDimension modelDimension;
            if (dimResponseCode == 200)
            {
                modelDimension = JsonConvert.DeserializeObject<ModelDimension>(modelDimensionResponse);
            }
            else
            {
                modelDimension = new ModelDimension();
            }

            ARPlaceCore.CurrentModelPath = modelPath;
            ARPlaceCore.CurrentModelDimension = modelDimension;
            Utils.SceneHistory.ChangeScene("ARPlace");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            PopupView.Instance.ShowMessage("AR 실행 중 오류가 발생했습니다.");
        }
        finally
        {
            PopupView.Instance.SetLoadingPannelActive(false);
        }
    }

    /// <summary>
    /// 뒤로가기 버튼 클릭 처리
    /// </summary>
    public void OnBackClicked()
    {
        Debug.Log("[GalleryPresenter] Back button clicked");

        switch (_currentPage)
        {
            case GalleryPage.List:
                Utils.SceneHistory.BackToPrevious();
                break;
            case GalleryPage.Inspect:
                _view.ShowGalleryPage();
                _currentPage = GalleryPage.List;
                // 갤러리 목록이 비어있으면 다시 로드
                if (_view.GetListItemCount() == 0)
                {
                    _pageIndex = 0;
                    _isLastPage = false;
                    LoadPage();
                }
                break;
        }
    }

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

    private void RefreshView()
    {
        _pageIndex = 0;
        _isLastPage = false;
        _isLoading = false;
        _view.ClearGalleryItems();
        LoadPage();
    }
    #endregion
}
