using UnityEngine;

public class Generate3DPresenter
{
    private readonly IGenerate3DView _view;
    private string _imagePath = null;

    public Generate3DPresenter(IGenerate3DView view)
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
                    PopupView.Instance.ShowMessage("사용자가 파일 선택을 취소했습니다.");
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

    }

    //--- Private Methods ---//
    private bool IsValidFileExtensioin(string path)
    {
        string lowerPath = path.ToLower();
        return lowerPath.EndsWith(".png") || lowerPath.EndsWith(".jpg") || lowerPath.EndsWith(".jpeg");
    }

    private async void OnUserConfirmedGeneration()
    {
        _view.ShowConvertingPage();
        Debug.Log("사용자가 3D 모델 생성을 확인했습니다.");
        long resultCode;
        resultCode = await Generate3DService.PostUpload(_imagePath);
        Debug.Log($"3D 모델 생성 요청 결과 코드: {resultCode}");
    }

    private void OnUserDeniedGeneration()
    {
        Debug.Log("사용자가 3D 모델 생성을 거부했습니다.");
        _imagePath = null;
    }
}
