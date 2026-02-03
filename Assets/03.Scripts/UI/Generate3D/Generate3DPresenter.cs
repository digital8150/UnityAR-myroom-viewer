using UnityEngine;

public class Generate3DPresenter
{
    private readonly IGenerate3DView _view;

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
            
            return;
        }

        try
        {
            NativeFilePicker.PickFile((path) =>
            {
                if (path == null)
                {
                    // 사용자가 파일 선택을 취소함
                }
                else if (!IsValidFileExtensioin(path))
                {
                    // 지원하지 않는 파일 형식
                }
                else
                {
                    // 파일 선택 완료

                }
            });
        }
        catch (System.Exception e)
        {
            // 파일 선택기 오류 처리
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
}
