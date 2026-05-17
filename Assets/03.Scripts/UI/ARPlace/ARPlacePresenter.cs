using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ARPlacePresenter
{
    private ARPlaceView _view;
    private ARPlaceCore _core;
    private bool _isCapturing;

    public ARPlacePresenter(ARPlaceView view, ARPlaceCore core)
    {
        _view = view;
        _core = core;
        _view.SetCancleButtonAction(OnCancleButtonClicked);
    }

    public void UpdateView()
    {
        _view.SetShowDimensionButtonText($"가구 치수 보기 : {TranslateStateDimensionStatus()}");
    }

    //--- Button Handler ---//
    public void OnCancleButtonClicked()
    {
        Utils.SceneHistory.BackToPrevious();
    }

    public void OnShutterButtonClicked()
    {
        if (_isCapturing) return;
        _isCapturing = true;
        _view.SetShutterInteractable(false);
        _view.StartCoroutine(TakeScreenshotAndSave());
    }


    public void OnShowDimensionClicked()
    {
        _core.ShowDimensions = !_core.ShowDimensions;
        UpdateView();
    }

    //--- Private Helpers ---//
    private string TranslateStateDimensionStatus()
    {
        if(_core.ShowDimensions)
        {
            return "<color=#20af20>ON</color>";
        }
        return "<color=#af2020>OFF</color>";
    }


    private IEnumerator TakeScreenshotAndSave()
    {
        yield return new WaitForEndOfFrame();

        Texture2D screenShot = _view.CaptureARCamera();
        if (screenShot == null)
        {
            Debug.LogError("AR 카메라 캡처 실패: ARPlaceView에 _arCamera가 할당되지 않았습니다.");
            _view.SetShutterInteractable(true);
            _isCapturing = false;
            yield break;
        }

        string fileName = "AR_Snapshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";

        _view.PlayFlashEffect();

        NativeGallery.SaveImageToGallery(
            screenShot, "AR Photos", fileName,
            (success, path) =>
            {
                Debug.Log($"갤러리 저장: success={success}, path={path}");
                _view.SetShutterInteractable(true);
                _isCapturing = false;
                UnityEngine.Object.Destroy(screenShot);
            });
    }
}
