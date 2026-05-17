using UnityEngine;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

public class ProjectInspectPresenter
{
    private readonly ProjectInspectView _view;
    private ModelData _modelData;
    private List<CategoryButton> _categoryButtons;

    private static int _selectedModelId;
    public static int SelectedModelId
    {
        set { _selectedModelId = value; }
    }

    private string _selectedCategory;

    public ProjectInspectPresenter(ProjectInspectView view, List<CategoryButton> categoryButtons)
    {
        _view = view;
        _categoryButtons = categoryButtons;

    }

    //--- Button Handlers ---//
    public void OnReturnClicked()
    {
        Utils.SceneHistory.BackToPrevious();
    }

    public void HandleCategorySelected(string category)
    {
        _selectedCategory = category;
        _view.SetCategoryButtonSelected(_selectedCategory);
        Debug.Log($"Selected category: {_selectedCategory}");
    }

    public async void OnRetryClicked()
    {
        long responseCode = await ProjectInspectService.DeleteModel3D(modelId: _selectedModelId);
        if (responseCode == 200)
        {
            Utils.SceneHistory.ChangeScene("Generate3D");
        }
        else
        {
            Debug.LogError($"[ProjectInspectPresenter.cs] Failed to delete model for retry. Response code: {responseCode}");
            PopupView.Instance.ShowMessage("모델 삭제에 실패했습니다. 잠시 후 다시 시도해주세요.");
        }
    }

    private bool _isDownloading = false;
    private bool _isRequestingDimension = false;
    private bool _isSubscribedToWebSocket = false;

    [Serializable]
    private class DimensionsImagePayload
    {
        [JsonProperty("model3d_id")] public int model3dId;
        [JsonProperty("member_id")] public int memberId;
        public string status;
        public string message;
        public DimensionsField dimensions;

        [Serializable]
        public class DimensionsField
        {
            public float width;
            public float length;
            public float depth;
            public float height;
            public string unit;
        }
    }

    public void OnAttachImageClicked()
    {
        if (_isRequestingDimension)
        {
            PopupView.Instance.ShowMessage("이미 치수 추정 요청이 진행 중입니다.");
            return;
        }

        string website = _view.GetWebsiteInputField();
        if (IsValidHttpUrl(website))
        {
            bool opened = _view.OpenWebsiteCapture(website, OnWebsiteCaptured);
            if (opened) return;
            // 프리팹 미할당 등 실패 시 파일 선택 폴백으로 이어감
        }

        if (NativeFilePicker.IsFilePickerBusy())
        {
            PopupView.Instance.ShowMessage("파일 선택기가 현재 사용 중입니다. 잠시 후 다시 시도해주세요.");
            return;
        }

        try
        {
            NativeFilePicker.PickFile((path) =>
            {
                if (string.IsNullOrEmpty(path)) return;

                string lower = path.ToLower();
                if (!(lower.EndsWith(".png") || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg")))
                {
                    PopupView.Instance.ShowMessage("유효하지 않은 파일 형식입니다. PNG, JPG, JPEG 파일만 선택해주세요.");
                    return;
                }

                _ = RequestDimensionByImageAsync(path);
            });
        }
        catch (Exception e)
        {
            PopupView.Instance.ShowMessage($"파일 선택 중 오류가 발생했습니다: {e.Message}");
        }
    }

    private static bool IsValidHttpUrl(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    private void OnWebsiteCaptured(byte[] jpgBytes)
    {
        if (jpgBytes == null || jpgBytes.Length == 0)
        {
            // 사용자 취소거나 캡처 실패 — 별도 안내 없음(취소가 일반적인 케이스)
            return;
        }
        _ = RequestDimensionByBytesAsync(jpgBytes, "website_capture.jpg", "image/jpeg");
    }

    private async Task RequestDimensionByBytesAsync(byte[] imageBytes, string fileName, string mime)
    {
        _isRequestingDimension = true;
        PopupView.Instance.SetLoadingPannelActive(true);
        try
        {
            var (code, _) = await ProjectInspectService.RequestDimensionByImage(_selectedModelId, imageBytes, fileName, mime);
            if (code < 200 || code >= 300)
            {
                PopupView.Instance.ShowMessage($"치수 추정 요청에 실패했습니다. (code: {code})");
                _isRequestingDimension = false;
                PopupView.Instance.SetLoadingPannelActive(false);
                return;
            }
            PopupView.AddPopup(new PopupContext("이미지로부터 치수를 추정 중입니다...", PopupView.Instance.GreenCheckCircle));
        }
        catch (Exception e)
        {
            Debug.LogError($"[ProjectInspectPresenter] RequestDimensionByBytes error: {e}");
            PopupView.Instance.ShowMessage("치수 추정 요청 중 오류가 발생했습니다.");
            _isRequestingDimension = false;
            PopupView.Instance.SetLoadingPannelActive(false);
        }
    }

    private async Task RequestDimensionByImageAsync(string imagePath)
    {
        _isRequestingDimension = true;
        PopupView.Instance.SetLoadingPannelActive(true);
        try
        {
            byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
            string fileName = Path.GetFileName(imagePath);
            string mime = imagePath.ToLower().EndsWith(".png") ? "image/png" : "image/jpeg";

            var (code, _) = await ProjectInspectService.RequestDimensionByImage(_selectedModelId, imageBytes, fileName, mime);
            if (code < 200 || code >= 300)
            {
                PopupView.Instance.ShowMessage($"치수 추정 요청에 실패했습니다. (code: {code})");
                _isRequestingDimension = false;
                PopupView.Instance.SetLoadingPannelActive(false);
                return;
            }

            PopupView.AddPopup(new PopupContext("이미지로부터 치수를 추정 중입니다...", PopupView.Instance.GreenCheckCircle));
        }
        catch (Exception e)
        {
            Debug.LogError($"[ProjectInspectPresenter] RequestDimensionByImage error: {e}");
            PopupView.Instance.ShowMessage("치수 추정 요청 중 오류가 발생했습니다.");
            _isRequestingDimension = false;
            PopupView.Instance.SetLoadingPannelActive(false);
        }
    }

    private void HandleDimensionsByImageMessage(string body)
    {
        try
        {
            var payload = JsonConvert.DeserializeObject<DimensionsImagePayload>(body);
            if (payload == null) return;
            if (payload.model3dId != _selectedModelId) return;

            _isRequestingDimension = false;
            PopupView.Instance.SetLoadingPannelActive(false);

            if (payload.status != "SUCCESS" || payload.dimensions == null)
            {
                PopupView.Instance.ShowMessage($"치수 추정에 실패했습니다. {payload.message}");
                return;
            }

            ModelDimension dim = new ModelDimension
            {
                width = payload.dimensions.width,
                height = payload.dimensions.height,
                length = payload.dimensions.length != 0 ? payload.dimensions.length : payload.dimensions.depth
            };
            _view.SetSizeInputField(dim);
            PopupView.AddPopup(new PopupContext("치수가 업데이트되었습니다.", PopupView.Instance.GreenCheckCircle));
        }
        catch (Exception e)
        {
            Debug.LogError($"[ProjectInspectPresenter] Failed to parse dimensions payload: {e}");
        }
    }

    public void Cleanup()
    {
        if (_isSubscribedToWebSocket && WebsocketController.Instance != null)
        {
            WebsocketController.Instance.OnModel3DDimensionsByImageReceived -= HandleDimensionsByImageMessage;
            _isSubscribedToWebSocket = false;
        }
    }

    public async void OnARPlaceClicked()
    {
        if (_isDownloading) return; // 중복 클릭 방지
        if (string.IsNullOrEmpty(_modelData.link)) return;

        _isDownloading = true;
        PopupView.Instance.SetLoadingPannelActive(true);

        var (responseCode, modelPath) = await ProjectInspectService.GetModel3DFile(_modelData.link);

        _isDownloading = false;
        PopupView.Instance.SetLoadingPannelActive(false);

        if (responseCode != 200)
        {
            Debug.LogError($"Failed to get model. Code: {responseCode}");
            PopupView.Instance.ShowMessage("모델 파일을 불러오는 데 실패했습니다.");
            return;
        }
        ARPlaceCore.CurrentModelDimension = _view.GetSizeInputField();
        ARPlaceCore.CurrentModelPath = modelPath; // 이제 로컬 경로가 들어감!
        await OnSaveClicked(silentMode:true);
        Utils.SceneHistory.ChangeScene("ARPlace");
    }

    public async System.Threading.Tasks.Task OnSaveClicked(bool silentMode = false)
    {
        ModelDimension dimension = _view.GetSizeInputField();
        long responseCode = await ProjectInspectService.PutModel3DDimension(_selectedModelId, dimension);
        if(responseCode != 200)
        {
            Debug.LogError($"[ProjectInspectPresenter.cs] Error while saving dimension into server. code was : {responseCode}");
            if(!silentMode) PopupView.Instance.ShowMessage("모델 크기 저장에 실패했습니다. 잠시 후 다시 시도해주세요.");
            return;
        }

        ModelUpdateData updateData = new ModelUpdateData
        {
            name = _view.GetNameInputField(),
            description = _view.GetDescriptionInputField(),
            shopPageLink = _view.GetWebsiteInputField(),
            is_shared = _view.GetIsPublicToggle(),
            furniture_type = _selectedCategory
        };

        string responseBody;
        (responseCode, responseBody) = await ProjectInspectService.PutModel3DV3(_selectedModelId, updateData);
        if (responseCode != 200)
        {
            Debug.LogError($"[ProjectInspectPresenter.cs] Error while saving model3dv2 into server. code was : {responseCode}");
            if (!silentMode) PopupView.Instance.ShowMessage("모델 정보 저장에 실패했습니다. 잠시 후 다시 시도해주세요.");
            return;
        }
        Debug.Log($"[ProjectInspectPresenter.cs] Successfully saved model info. Response code: {responseCode}, Response body: {responseBody}");
        if (!silentMode)
        {
            PopupView.AddPopup(new PopupContext("모델 정보가 저장되었습니다.", PopupView.Instance.GreenCheckCircle));
            Debug.Log("[ProjectInspectPresenter.cs] Model information saved successfully.");
        }
    }

    public async void InitializeView()
    {
        long responseCode;
        string responseBody;
        (responseCode, responseBody) = await ProjectInspectService.GetSingleModel3D(_selectedModelId);

        if(responseCode != 200)
        {
            Debug.LogError($"[ProjectsInspectView.cs] Failed to initialize view : the response code was {responseCode}");
            return;
        }

        try
        {
            _modelData = JsonConvert.DeserializeObject<ModelData>(responseBody);
            if (_modelData == null)
            {
                Debug.LogError($"[ProjectsInspectView.cs] Deserialized model data is null.");
                return;
            }

            switch(_modelData.status)
            {
                case "SUCCESS":
                    string modelDimensionResponse;
                    ModelDimension modelDimension;
                    (responseCode, modelDimensionResponse) = await ProjectInspectService.GetModel3DDimension(_selectedModelId);
                    if (responseCode == 200)
                    {
                        modelDimension = JsonConvert.DeserializeObject<ModelDimension>(modelDimensionResponse);
                    }
                    else
                    {
                        modelDimension = new ModelDimension();
                    }

                    ShowSuccessView(_modelData, modelDimension);
                    break;
                case "FAILED":
                    ShowFailedView(_modelData);
                    break;
                case "PROCESSING":
                    _view.HideAllPage();
                    PopupView.Instance.ShowMessage("모델이 아직 처리중입니다.");
                    break;
                default:
                    Debug.LogWarning($"[ProjectsInspectView.cs] Unknown model status: {_modelData.status}");
                    break;
            }
        }
        catch(Exception ex)
        {
            Debug.LogError($"[ProjectsInspectView.cs] Exception while deserializing : {ex}");
        }
    }
    

    private async void ShowSuccessView(ModelData modelData, ModelDimension modelDimension)
    {
        if (!_isSubscribedToWebSocket && WebsocketController.Instance != null)
        {
            WebsocketController.Instance.OnModel3DDimensionsByImageReceived += HandleDimensionsByImageMessage;
            _isSubscribedToWebSocket = true;
        }

        foreach (var item in _categoryButtons)
        {
            if (item.button) item.button.onClick.AddListener(() => HandleCategorySelected(item.categoryName));
        }

        _selectedCategory = modelData.furniture_type;
        _view.SetCategoryButtonSelected(_selectedCategory);
        _view.SetNameInputField(modelData.name);
        _view.SetDescriptionInputField(modelData.description);
        _view.SetWebsiteInputField(modelData.shopPageLink);
        _view.SetSizeInputField(modelDimension);
        _view.SetIsPublicToggle(modelData.is_shared);
        _view.ShowDonePage();
        _view.SpawnModel3D(await DownloadModel3D(modelData.link));
        //_view.SetDoneImage(await Utils.ImageUtils.LoadSpriteFromUrlAsync(modelData.thumbnailUrl));
    }

    private void ShowFailedView(ModelData modelData)
    {
        _view.SetFailReason(modelData.errorMessage);
        _view.ShowFailedPage();
    }

    private async Task<string> DownloadModel3D(string modelUrl)
    {
        var(responseCode, localPath) = await ProjectInspectService.GetModel3DFile(modelUrl);
        if(responseCode == 200)
        {
            Debug.Log($"Model downloaded successfully. Local path: {localPath}");
            return localPath;
        }
        else
        {
            Debug.LogError($"Failed to download model. Response code: {responseCode}");
            PopupView.Instance.ShowMessage("모델 파일을 불러오는 데 실패했습니다.");
            return string.Empty;
        }
    }
}
