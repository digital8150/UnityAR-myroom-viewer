using Newtonsoft.Json;
using UnityEngine;
using System;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using Utils;
using System.Linq.Expressions;

enum AIRecommendViewPage
{
    Landing,
    Category,
    Loading,
    Result,
    List,
    AR,
    Inspect
}

public class AIRecommendPresenter
{
    private AIRecommendView _view;
    private string _imagePath = null;
    private bool _isTakenPicture = false;
    private RoomAnalysisResponseDto recommendData;
    private string _selectedCategory = "others";
    private AIRecommendViewPage _currentPage;

    public AIRecommendPresenter(AIRecommendView view)
    {
        _view = view;
    }

    public void Initialize()
    {
        _view.ShowLandingPage();
        _currentPage = AIRecommendViewPage.Landing;
        _view.SetLoadImageButtonAction(OnLoadImageClicked);
        _view.SetTakePictureButtonAction(OnTakePictureClicked);
        _view.SetConfirmCategoryButtonAction(UploadImage);
        _view.SetBackButtonHandler(OnBackButtonClicked);
        foreach (var item in _view.CategoryButtons)
        {
            if (item.button) item.button.onClick.AddListener(() => HandleCategorySelected(item.categoryName));
        }
    }

    #region Landing Page Actions
    private void OnLoadImageClicked()
    {
        if (NativeFilePicker.IsFilePickerBusy())
        {
            PopupView.Instance.ShowMessage("파일 선택기가 현재 사용 중입니다. 잠시 후 다시 시도해주세요.");
            return;
        }

        try
        {
            NativeFilePicker.PickFile((path) =>
            {
                if (path == null)
                {

                }
                else if (!IsValidFileExtensioin(path))
                {
                    PopupView.Instance.ShowMessage("유효하지 않은 파일 형식입니다. PNG, JPG, JPEG 파일만 선택해주세요.");
                }
                else
                {
                    //선택 완료 이후 로직
                    Debug.Log($"선택된 파일 경로: {path}");
                    _imagePath = path;
                    _isTakenPicture = false;
                    PopupView.Instance.Presenter.ShowYesNo("선택한 방 사진으로 계속 진행하시겠습니까?", OnUserConfirmed, OnUserDenied);
                }
            });
        }
        catch (System.Exception e)
        {
            PopupView.Instance.ShowMessage($"파일 선택 중 오류가 발생했습니다: {e.Message}");
        }
    }

    public void OnTakePictureClicked()
    {
        NativeCamera.TakePicture((path) =>
        {
            if (path != null)
            {
                _imagePath = path;
                _isTakenPicture = true;
                PopupView.Instance.Presenter.ShowYesNo(
                    "촬영한 방 사진으로 계속 진행하시겠습니까?",
                    OnUserConfirmed,
                    OnUserDenied);
            }
        });
    }

    private bool IsValidFileExtensioin(string path)
    {
        string lowerPath = path.ToLower();
        return lowerPath.EndsWith(".png") || lowerPath.EndsWith(".jpg") || lowerPath.EndsWith(".jpeg");
    }

    private void OnUserConfirmed()
    {
        _view.ShowCategoryPage();
        _currentPage = AIRecommendViewPage.Category;
    }

    private void OnUserDenied()
    {
        Debug.Log("User Denied");
        _imagePath = null;
    }
    #endregion

    #region Category Page Actions
    private void HandleCategorySelected(string category)
    {
        _selectedCategory = category;
        _view.SetCategoryButtonSelected(_selectedCategory);
        Debug.Log($"Selected category: {_selectedCategory}");
    }

    private async void UploadImage()
    {
        PopupView.Instance.SetLoadingPannelActive(true);

        var (responseCode, responseText) = await AIRecommendService.PostRecommends(_selectedCategory, 10, _imagePath, _isTakenPicture);
        Debug.Log($"[RecommendPresetner] Uploaded, code : {responseCode}, text : {responseText}");

        if (responseCode == 200)
        {
            _view.SetLoadingProgress(0.0f);
            _view.ShowLoadingPage();
            _currentPage = AIRecommendViewPage.Loading;
            _view.SetLoadingProgressSmooth(0.8f, 20);
            WebsocketController.Instance.OnAIRecommendReceived += OnAIRecommendReceived;
        }
        else
        {
            PopupView.Instance.ShowMessage($"추천 요청에 실패했습니다. 나중에 다시 시도해주세요");
        }

        PopupView.Instance.SetLoadingPannelActive(false);
    }
    #endregion

    #region Loading Page Actions
    private async void OnAIRecommendReceived(string message)
    {
        WebsocketController.Instance.OnAIRecommendReceived -= OnAIRecommendReceived;
        Debug.Log($"[RecommendPresenter] Received AI Recommend: {message}");
        try
        {
            recommendData = JsonConvert.DeserializeObject<RoomAnalysisResponseDto>(message);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RecommendPresenter] error while parsing websocket response : {ex}, body : {message}");
            PopupView.Instance.ShowMessage($"AI 방 요청 처리중 오류 발생");
            return;
        }

        _view.SetLoadingProgress(1);
        SetResultPage(recommendData);
        PrepareListPage();
        await Task.Delay(500);
        _view.ShowResultPage();
        _currentPage = AIRecommendViewPage.Result;
        _view.SetGoToRecommendListButtonAction(ShowListPage);
    }
    #endregion

    #region Result Page Actions
    private async void SetResultPage(RoomAnalysisResponseDto data)
    {
        _view.SetResultText("공간 분석 결과<br><size=17px><font=\"Pretendard-Medium SDF\">\r\n" +
            $"• 스타일: {data.RoomAnalysis.Style}\r\n" +
            $"• 주요 재질: {data.RoomAnalysis.Material}\r\n" +
            $"• 감지 가구: {string.Join(", ",data.RoomAnalysis.DetectedFurniture)}\r\n" +
            $"• 주요 색상: {data.RoomAnalysis.Color}</size></font>\r\n\r\n" +
            "위 방에 어울리는 가구 추천");
        _view.SetResultThumbnail(await NativeCamera.LoadImageAtPathAsync(_imagePath));
    }
    #endregion

    #region Recommend List Page Actions
    private void ShowListPage()
    {

        _view.ShowListPage();
        _currentPage = AIRecommendViewPage.List;
    }

    private async void PrepareListPage()
    {
        foreach (var item in recommendData.Recommendation.Results)
        {
            _view.AddListItem(
                await ModelService.GetModelThumbnailUrlByModelId(item.Model3dId),
                await ModelService.GetModelNameByModelId(item.Model3dId),
                goToInspectAction: () => ShowInspectView(item.Model3dId),
                goToARAction: () => ShowARView(item.Model3dId));
        }
        _view.SetListResultText("해당 가구들 추천 이유 <font=\"Pretendard-Regular SDF\">\r\n" +
            $"{recommendData.Recommendation.Reasoning}");
    }

    private async void ShowARView(int modelId)
    {
        PopupView.Instance.SetLoadingPannelActive(true);
        ModelData modelData = await ModelService.GetModelDataByModelId(modelId);
        ModelDimension modelDimension;

        //Get Model3D File
        var (responseCode, modelPath) = await ProjectInspectService.GetModel3DFile(modelData.link);
        if (responseCode != 200)
        {
            Debug.LogError($"Failed to get model. Code: {responseCode}");
            PopupView.Instance.ShowMessage("모델 파일을 불러오는 데 실패했습니다.");
            PopupView.Instance.SetLoadingPannelActive(false);
            return;
        }

        //Get Model Dimension
        var (dimResponsCode, modelDimensionResponse) = await ProjectInspectService.GetModel3DDimension(modelData.id);
        if(dimResponsCode == 200)
        {
            modelDimension = JsonConvert.DeserializeObject<ModelDimension>(modelDimensionResponse);
        }
        else
        {
            modelDimension = new ModelDimension();
        }

        ARPlaceCore.CurrentModelPath = modelPath;
        ARPlaceCore.CurrentModelDimension = modelDimension;
        _view.ShowARPlacePage();
        _currentPage = AIRecommendViewPage.AR;
        _view.SetARCancleButtonAction(() =>
        {
            OnBackButtonClicked();
        });
        PopupView.Instance.SetLoadingPannelActive(false);
    }

    private async void ShowInspectView(int modelId)
    {
        PopupView.Instance.SetLoadingPannelActive(true);
        ModelData modelData = await ModelService.GetModelDataByModelId(modelId);
        ModelDimension modelDimension;

        //Get Model3D File
        var (responseCode, modelPath) = await ProjectInspectService.GetModel3DFile(modelData.link);
        if (responseCode != 200)
        {
            Debug.LogError($"Failed to get model. Code: {responseCode}");
            PopupView.Instance.ShowMessage("모델 파일을 불러오는 데 실패했습니다.");
            PopupView.Instance.SetLoadingPannelActive(false);
            return;
        }

        //Get Model Dimension
        var (dimResponsCode, modelDimensionResponse) = await ProjectInspectService.GetModel3DDimension(modelData.id);
        if (dimResponsCode == 200)
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
        _currentPage = AIRecommendViewPage.Inspect;
        PopupView.Instance.SetLoadingPannelActive(false);
    }
    #endregion

    private void OnBackButtonClicked()
    {
        switch(_currentPage)
        {
            case AIRecommendViewPage.Landing:
                SceneHistory.BackToPrevious();
                break;
            case AIRecommendViewPage.Category:
                _view.ShowLandingPage();
                _currentPage = AIRecommendViewPage.Landing;
                break;
            case AIRecommendViewPage.Loading:
                PopupView.Instance.Presenter.ShowYesNo("추천 요청을 취소하시겠습니까?", () =>
                {
                    WebsocketController.Instance.OnAIRecommendReceived -= OnAIRecommendReceived;
                    _view.ShowLandingPage();
                    _currentPage = AIRecommendViewPage.Landing;
                }, () => { });
                break;
            case AIRecommendViewPage.Result:
                _view.ShowLandingPage();
                _currentPage = AIRecommendViewPage.Landing;
                break;
            case AIRecommendViewPage.List:
                _view.ShowResultPage();
                _currentPage = AIRecommendViewPage.Result;
                break;
            case AIRecommendViewPage.AR:
                _view.ShowListPage();
                _currentPage = AIRecommendViewPage.List;
                break;
            case AIRecommendViewPage.Inspect:
                _view.ShowListPage();
                _currentPage = AIRecommendViewPage.List;
                break;
        }
    }
}

