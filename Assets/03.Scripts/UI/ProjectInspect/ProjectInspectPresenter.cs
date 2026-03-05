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

    public async void OnSaveClicked()
    {
        ModelDimension dimension = _view.GetSizeInputField();
        long responseCode = await ProjectInspectService.PutModel3DDimension(_selectedModelId, dimension);
        if(responseCode != 200)
        {
            Debug.LogError($"[ProjectInspectPresenter.cs] Error while saving dimension into server. code was : {responseCode}");
            PopupView.Instance.ShowMessage("모델 크기 저장에 실패했습니다. 잠시 후 다시 시도해주세요.");
            return;
        }

        ModelUpdateData updateData = new ModelUpdateData
        {
            name = _view.GetNameInputField(),
            description = _view.GetDescriptionInputField(),
            shopPageLink = _view.GetWebsiteInputField(),
            is_shared = _view.GetIsPublicToggle()
        };

        string responseBody;
        (responseCode, responseBody) = await ProjectInspectService.PutModel3DV2(_selectedModelId, updateData);
        if (responseCode != 200)
        {
            Debug.LogError($"[ProjectInspectPresenter.cs] Error while saving model3dv2 into server. code was : {responseCode}");
            PopupView.Instance.ShowMessage("모델 정보 저장에 실패했습니다. 잠시 후 다시 시도해주세요.");
            return;
        }
        Debug.Log($"[ProjectInspectPresenter.cs] Successfully saved model info. Response code: {responseCode}, Response body: {responseBody}");
        PopupView.AddPopup(new PopupContext("모델 정보가 저장되었습니다.", PopupView.Instance.GreenCheckCircle));
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
    

    private async void ShowSuccessView(ModelData modelData, ModelDimension modelDimension)
    {
        _view.SetNameInputField(modelData.name);
        _view.SetDescriptionInputField(modelData.description);
        _view.SetWebsiteInputField(modelData.shopPageLink);
        _view.SetSizeInputField(modelDimension);
        _view.SetIsPublicToggle(modelData.is_shared);
        _view.ShowDonePage();
        _view.SetDoneImage(await Utils.ImageUtils.LoadSpriteFromUrl(modelData.thumbnailUrl));
    }

    private void ShowFailedView(ModelData modelData)
    {
        _view.SetFailReason(modelData.errorMessage);
        _view.ShowFailedPage();
    }
}
