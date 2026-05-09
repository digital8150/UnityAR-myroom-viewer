using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class Generate3DPresenter
{
    private const int MaxImages = 4;

    private readonly Generate3DView _view;
    private readonly string[] _imagePaths = new string[MaxImages];
    private bool _isCameraMode = false;

    public static int GenerateProcessingModelID = -1;

    public Generate3DPresenter(Generate3DView view)
    {
        _view = view;
    }

    public void OnReturnClicked()
    {
        Utils.SceneHistory.BackToPrevious();
    }

    public void OnEnterImageSelectionClicked()
    {
        _isCameraMode = false;
        ResetSlots();
        _view.ShowImageSelectionPage();
    }

    public void OnTakePictureClicked()
    {
        _isCameraMode = true;
        ResetSlots();
        _view.ShowImageSelectionPage();
    }

    public void OnBackToLandingClicked()
    {
        ResetSlots();
        _view.ShowLandingPage();
    }

    public void OnAddImageClicked(int slotIndex)
    {
        if (_isCameraMode)
        {
            TakePictureForSlot(slotIndex);
        }
        else
        {
            PickFileForSlot(slotIndex);
        }
    }

    private void PickFileForSlot(int slotIndex)
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
                if (path == null) return;

                if (!IsValidFileExtension(path))
                {
                    PopupView.Instance.ShowMessage("유효하지 않은 파일 형식입니다. PNG, JPG, JPEG 파일만 선택해주세요.");
                    return;
                }

                _imagePaths[slotIndex] = path;
                _ = LoadAndShowPreviewAsync(slotIndex, path);
                RefreshGenerateButton();
            });
        }
        catch (System.Exception e)
        {
            PopupView.Instance.ShowMessage($"파일 선택 중 오류가 발생했습니다: {e.Message}");
        }
    }

    private void TakePictureForSlot(int slotIndex)
    {
        if (!NativeCamera.IsCameraBusy())
        {
            NativeCamera.TakePicture((path) =>
            {
                if (path == null) return;

                _imagePaths[slotIndex] = path;
                _ = LoadCameraPreviewAsync(slotIndex, path);
                RefreshGenerateButton();
            }, maxSize: 2048);
        }
        else
        {
            PopupView.Instance.ShowMessage("카메라가 현재 사용 중입니다. 잠시 후 다시 시도해주세요.");
        }
    }

    public void OnRemoveImageClicked(int slotIndex)
    {
        _imagePaths[slotIndex] = null;
        _view.ClearSlotPreview(slotIndex);
        RefreshGenerateButton();
    }

    public void OnGenerateClicked()
    {
        PopupView.Instance.Presenter.ShowYesNo(
            "선택한 이미지를 3D 모델로 변환하시겠습니까?",
            OnUserConfirmedGeneration,
            OnUserDeniedGeneration);
    }

    private bool IsValidFileExtension(string path)
    {
        string lowerPath = path.ToLower();
        return lowerPath.EndsWith(".png") || lowerPath.EndsWith(".jpg") || lowerPath.EndsWith(".jpeg");
    }

    private async Task LoadAndShowPreviewAsync(int slotIndex, string path)
    {
        byte[] bytes = await File.ReadAllBytesAsync(path);
        var texture = new Texture2D(2, 2);
        if (texture.LoadImage(bytes))
            _view.SetSlotPreview(slotIndex, texture);
        else
            Object.Destroy(texture);
    }

    private async Task LoadCameraPreviewAsync(int slotIndex, string path)
    {
        await Task.Yield();
        Texture2D texture = NativeCamera.LoadImageAtPath(path, maxSize: 1024, markTextureNonReadable: false);
        if (texture != null)
            _view.SetSlotPreview(slotIndex, texture);
    }

    private void ResetSlots()
    {
        for (int i = 0; i < MaxImages; i++)
        {
            _imagePaths[i] = null;
            _view.ClearSlotPreview(i);
        }
        _view.SetGenerateButtonInteractable(false);
    }

    private void RefreshGenerateButton()
    {
        foreach (var path in _imagePaths)
        {
            if (!string.IsNullOrEmpty(path))
            {
                _view.SetGenerateButtonInteractable(true);
                return;
            }
        }
        _view.SetGenerateButtonInteractable(false);
    }

    private async void OnUserConfirmedGeneration()
    {
        var validPaths = new List<string>();
        foreach (var path in _imagePaths)
            if (!string.IsNullOrEmpty(path)) validPaths.Add(path);

        PopupView.Instance.SetLoadingPannelActive(true);
        long resultCode;

        if (validPaths.Count == 1)
        {
            (resultCode, GenerateProcessingModelID) = await Generate3DService.PostUpload(
                validPaths[0], furniture_type: "temp", name: "내 가구", isShared: false, isTakenPicture: _isCameraMode);
        }
        else
        {
            (resultCode, GenerateProcessingModelID) = await Generate3DService.PostUploadMulti(
                validPaths, furniture_type: "temp", name: "내 가구", isShared: false, isTakenPicture: _isCameraMode);
        }

        Debug.Log($"Upload request response code: {resultCode}");
        if (resultCode != 200)
        {
            PopupView.Instance.ShowMessage("이미지 업로드 중 오류가 발생했습니다");
            Debug.LogError($"[Generate3DPresenter] Upload failed. responseCode: {resultCode}, modelId: {GenerateProcessingModelID}");
            PopupView.Instance.SetLoadingPannelActive(false);
            return;
        }

        Utils.SceneHistory.ChangeScene("Projects");
        PopupView.Instance.SetLoadingPannelActive(false);
    }

    private void OnUserDeniedGeneration()
    {
        Debug.Log("사용자가 3D 모델 생성을 거부했습니다.");
    }
}
