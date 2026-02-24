using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Generate3DPresenter
{
    private readonly Generate3DView _view;
    private string _imagePath = null;
    private ModelGenerationResponse _generated3DModel;

    public static int GenerateProcessingModelID = -1;

    public Generate3DPresenter(Generate3DView view)
    {
        _view = view;
    }

    public void OnReturnClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
    }

    public void OnLoadImageClicked()
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
                    PopupView.Instance.Presenter.ShowYesNo("선택한 이미지를 3D 모델로 변환하시겠습니까?", OnUserConfirmedGeneration, OnUserDeniedGeneration);
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
        NativeCamera.TakePicture((path) => {
            if(path != null)
            {
                _imagePath = path;
                PopupView.Instance.Presenter.ShowYesNo("선택한 이미지를 3D 모델로 변환하시겠습니까?", OnUserConfirmedGeneration, OnUserDeniedGeneration);
            }
        });
    }

    //--- Private Methods ---//
    private bool IsValidFileExtensioin(string path)
    {
        string lowerPath = path.ToLower();
        return lowerPath.EndsWith(".png") || lowerPath.EndsWith(".jpg") || lowerPath.EndsWith(".jpeg");
    }

    private async void OnUserConfirmedGeneration()
    {
        Debug.Log("User confirmed model generation");
        long resultCode;
        (resultCode, GenerateProcessingModelID) = await Generate3DService.PostUpload(_imagePath);
        Debug.Log($"Upload request response code : {resultCode}");
        if(resultCode != 200)
        {
            PopupView.Instance.ShowMessage("이미지 업로드 중 오류가 발생했습니다");
            Debug.LogError($"[Generate3DPresenter.cs] Something went wrong while uploading image!! responseCode : {resultCode} response modelId : {GenerateProcessingModelID}");
            return;
        }
        SceneManager.LoadScene("Projects");
    }

    private void OnUserDeniedGeneration()
    {
        Debug.Log("사용자가 3D 모델 생성을 거부했습니다.");
        _imagePath = null;
    }
}
