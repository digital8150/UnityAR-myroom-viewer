using UnityEngine;

public class RoomPlanPresenter
{
    private RoomPlanView _view;
    private string _imagePath = null;

    public RoomPlanPresenter(RoomPlanView view)
    {
        _view = view;
        InitView();
    }

    private void InitView()
    {
        ShowProjectListPage();

        _view.SetButtonActions(
            onBack: OnBackButtonClicked,
            onNewProject: OnNewProjectButtonClicked
        );

        _view.SetModalActions(
            onCancel: OnModalCancelClicked,
            onSelectDefault: OnModalSelectDefaultClicked,
            onLoadFloor: OnModalLoadFloorClicked
        );
    }

    #region Page Controls
    private void HideAllPages()
    {
        _view.SetActiveModal(false);
        _view.SetActiveProjectListPage(false);
        _view.SetActiveEditPage(false);
    }

    private void ShowProjectListPage()
    {
        HideAllPages();
        _view.SetActiveProjectListPage(true);
    }

    private void ShowEditPage()
    {
        HideAllPages();
        _view.SetActiveEditPage(true);
    }
    #endregion

    #region Button Actions
    private void OnBackButtonClicked()
    {
        Utils.SceneHistory.BackToPrevious();
    }

    private void OnNewProjectButtonClicked()
    {
        _view.SetActiveModal(true);
    }

    private void OnModalCancelClicked()
    {
        _view.SetActiveModal(false);
    }

    private void OnModalSelectDefaultClicked()
    {
        _view.SetActiveModal(false);

    }

    private void OnModalLoadFloorClicked()
    {
        _view.SetActiveModal(false);
        //load image and upload
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
                    PopupView.Instance.Presenter.ShowYesNo("선택한 이미지로 방을 생성하시겠습니까?", OnUserConfirmedGeneration, OnUserDeniedGeneration);
                }
            });
        }
        catch (System.Exception e)
        {
            PopupView.Instance.ShowMessage($"파일 선택 중 오류가 발생했습니다: {e.Message}");
        }
    }
    #endregion

    #region Private Helpers
    private bool IsValidFileExtensioin(string path)
    {
        string lowerPath = path.ToLower();
        return lowerPath.EndsWith(".png") || lowerPath.EndsWith(".jpg") || lowerPath.EndsWith(".jpeg");
    }

    private async void OnUserConfirmedGeneration()
    {
        PopupView.Instance.SetLoadingPannelActive(true);
        Debug.Log("User confirmed model generation");
        var(resultCode, responseJson) = await RoomPlanService.PostUpload(_imagePath);
        Debug.Log($"Upload request response code : {resultCode}");

        if (resultCode != 200)
        {
            PopupView.Instance.ShowMessage("이미지 업로드 중 오류가 발생했습니다");
            PopupView.Instance.SetLoadingPannelActive(false);
            return;
        }

        ConstructRoom(responseJson);
        PopupView.Instance.SetLoadingPannelActive(false);
        ShowEditPage();

    }

    private void OnUserDeniedGeneration()
    {
        Debug.Log("사용자가 3D 모델 생성을 거부했습니다.");
        _imagePath = null;
    }

    private void ConstructRoom(string FloorPlanJson)
    {

    }
    #endregion
}