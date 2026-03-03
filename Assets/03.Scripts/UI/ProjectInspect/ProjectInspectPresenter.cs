using UnityEngine;
using System;
using Newtonsoft.Json;

public class ProjectInspectPresenter
{
    private readonly ProjectInspectView _view;

    private static int _selectedModelId;
    public static int SelectedModelId
    {
        set { _selectedModelId = value; }
    }

    public ProjectInspectPresenter(ProjectInspectView view)
    {
        _view = view;
    }

    //--- Button Handlers ---//
    public void OnReturnClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(Utils.SceneHistory.PreviousScene);
    }

    public async void OnRetryClicked()
    {
        long responseCode = await ProjectInspectService.DeleteModel3D(modelId: _selectedModelId);
        if (responseCode == 200)
        {
            Utils.SceneHistory.MarkCurrentScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Generate3D");
        }
        else
        {
            Debug.LogError($"[ProjectInspectPresenter.cs] Failed to delete model for retry. Response code: {responseCode}");
            PopupView.Instance.ShowMessage("모델 삭제에 실패했습니다. 잠시 후 다시 시도해주세요.");
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
            ModelData modelData = JsonConvert.DeserializeObject<ModelData>(responseBody);
            if (modelData == null)
            {
                Debug.LogError($"[ProjectsInspectView.cs] Deserialized model data is null.");
                return;
            }

            switch(modelData.status)
            {
                case "SUCCESS":
                    string modelDimension;
                    (responseCode, modelDimension) = await ProjectInspectService.GetModel3DDimension(_selectedModelId);
                    if (responseCode != 200)
                    {
                        modelDimension = "가구 사이즈 정보를 입력해주세요.";
                    }
                    ShowSuccessView(modelData, modelDimension);
                    break;
                case "FAILED":
                    ShowFailedView(modelData);
                    break;
                case "PROCESSING":
                    _view.HideAllPage();
                    PopupView.Instance.ShowMessage("모델이 아직 처리중입니다.");
                    break;
                default:
                    Debug.LogWarning($"[ProjectsInspectView.cs] Unknown model status: {modelData.status}");
                    break;
            }
        }
        catch(Exception ex)
        {
            Debug.LogError($"[ProjectsInspectView.cs] Exception while deserializing : {ex}");
        }
    }
    

    private async void ShowSuccessView(ModelData modelData, string modelDimension)
    {
        _view.SetNameInputField(modelData.name);
        _view.SetDescriptionInputField(modelData.description);
        _view.SetWebsiteInputField(modelData.link);
        _view.SetSizeInputField(modelDimension);
        _view.ShowDonePage();
        _view.SetDoneImage(await Utils.ImageUtils.LoadSpriteFromUrl(modelData.thumbnailUrl));
    }

    private void ShowFailedView(ModelData modelData)
    {
        //_view.UpdateFailReason(modelData.failReason);
        _view.ShowFailedPage();
    }
}
