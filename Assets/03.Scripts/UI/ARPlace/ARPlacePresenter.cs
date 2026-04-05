using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ARPlacePresenter
{
    private ARPlaceView _view;
    private ARPlaceCore _core;

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
        // 1. 프레임 끝까지 대기 (렌더링이 완료된 후 캡처해야 함)
        yield return new WaitForEndOfFrame();

        // 2. 화면 크기의 Texture2D 생성
        int width = Screen.width;
        int height = Screen.height;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);

        // 3. 현재 카메라의 화면을 읽어옴
        Rect rect = new Rect(0, 0, width, height);
        screenShot.ReadPixels(rect, 0, 0);
        screenShot.Apply();

        // 4. 바이트 배열로 인코딩 (PNG)
        byte[] bytes = screenShot.EncodeToPNG();

        // 5. 갤러리에 저장 (Native Gallery 라이브러리 권장)
        // 직접 구현 시 경로 설정 및 권한 처리가 복잡하므로 라이브러리 사용을 추천합니다.
        string fileName = "AR_Snapshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";

        /* NativeGallery.SaveImageToGallery(bytes, "AR Photos", fileName, (success, path) => {
            Debug.Log("저장 결과: " + success + " 경로: " + path);
        });
        */

        // 메모리 해제
        UnityEngine.Object.Destroy(screenShot);
    }
}
