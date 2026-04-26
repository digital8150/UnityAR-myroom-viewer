using UnityEngine;

public class RoomPlanPresenter
{
    private readonly RoomPlanView _view;
    private readonly TouchView _touchView;
    private string _imagePath = null;

    private Vector3 _currentPosition;
    private float _currentRotationY;

    public RoomPlanPresenter(RoomPlanView view, TouchView touchView, Material defaultWallMaterial)
    {
        _view = view;
        _touchView = touchView;
        Builder.wallMat = defaultWallMaterial;

        _touchView.OnSingleDrag += HandlePositionUpdate;
        _touchView.OnDoubleDrag += HandleRotationUpdate;

        var(initPos, initRot) = _view.GetViewPortCameraTransform();
        _currentPosition = initPos;
        _currentRotationY = initRot.y;

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

    #region Touch Input Handles
    // 1) 한 손가락: 로컬 좌표계 기준 위치 이동 (Y축 고정)
    private void HandlePositionUpdate(Vector2 delta)
    {
        // 감도를 낮춰 둔감하게 조절 (0.01f ~ 0.05f 사이 추천)
        float sensitivity = 0.02f;

        // View로부터 카메라(또는 타겟)의 현재 방향 벡터를 가져옴
        var (pos, rot) = _view.GetViewPortCameraTransform();
        Quaternion currentRot = Quaternion.Euler(rot);

        // 카메라의 Right(우측)와 Forward(전방) 벡터 계산
        Vector3 right = currentRot * Vector3.right;
        Vector3 forward = currentRot * Vector3.forward;

        // Y축 이동을 막기 위해 벡터의 Y값을 제거하고 정규화
        right.y = 0;
        forward.y = 0;
        right.Normalize();
        forward.Normalize();

        // 로컬 방향 기반 변화량 계산
        // delta.x는 좌우(right), delta.y는 앞뒤(forward) 이동에 매핑
        Vector3 localMovement = (right * delta.x * sensitivity) + (forward * delta.y * sensitivity);

        _currentPosition += localMovement;

        SetPosition(_currentPosition);
    }

    // 2) 두 손가락: 중심점 드래그 액션으로 Y축 회전
    private void HandleRotationUpdate(Vector2 centerDelta, float rotationDelta)
    {
        // 회전 감도 조절 (0.5f가 너무 빠르면 더 낮추세요)
        float rotSensitivity = 0.3f;
        _currentRotationY -= rotationDelta * rotSensitivity;

        SetRotation(_currentRotationY);
    }

    private void SetPosition(Vector3 pos)
    {
        _view.SetViewPortCameraPosition(pos);
    }

    private void SetRotation(float yAngle)
    {
        // Y축 회전만 적용
        _view.SetViewPortCameraRotation(new Vector3(0, yAngle, 0));
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
        Analyze.data = FloorPlanJson;
        GameObject builder = new GameObject("RoomBuilder");
        builder.AddComponent<Builder>();
    }
    #endregion
}