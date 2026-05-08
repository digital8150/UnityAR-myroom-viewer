public class MyPagePresenter
{
    private readonly MyPageView _view;

    public MyPagePresenter(MyPageView view)
    {
        _view = view;
    }

    public void InitializeView()
    {
        _view.SetActiveMainPage(true);
        _view.SetActiveProfileEditPage(false);
    }

    public void OnEditProfileClicked()
    {
        _view.SetActiveMainPage(false);
        _view.SetActiveProfileEditPage(true);
    }

    public void OnSaveProfileClicked()
    {
        string nickname = _view.GetNicknameInput();
        // TODO: API 호출로 닉네임 저장
        _view.SetUserInfo(nickname);
        _view.SetActiveProfileEditPage(false);
        _view.SetActiveMainPage(true);
    }

    public void OnBackButtonClicked()
    {
        Utils.SceneHistory.BackToPrevious();
    }

    public void OnCancelEditClicked()
    {
        _view.SetActiveProfileEditPage(false);
        _view.SetActiveMainPage(true);
    }

    public void OnInputFieldFocused(string currentText)
    {
        if (PopupView.Instance == null) return;
        PopupView.Instance.SetKeyboardHelperActive(true);
        PopupView.Instance.SetKeyboardHelperContent(currentText);
    }

    public void OnInputFieldValueChanged(string text)
    {
        if (PopupView.Instance == null) return;
        PopupView.Instance.SetKeyboardHelperContent(text);
    }

    public void OnInputFieldEndEdit(string _)
    {
        if (PopupView.Instance == null) return;
        PopupView.Instance.SetKeyboardHelperActive(false);
    }

    public void OnChangeProfilePictureClicked()
    {
        // TODO: 갤러리 또는 카메라에서 이미지 선택
    }

    public void OnGoToMyPostClicked()
    {
        // TODO: 내 게시글 화면으로 이동
    }

    public void OnGoToMyLikeClicked()
    {
        // TODO: 좋아요한 게시글 화면으로 이동
    }

    public void OnGoToMySavedClicked()
    {
        // TODO: 저장한 게시글 화면으로 이동
    }

    public void OnGoToMyFurnitureClicked()
    {
        // TODO: 내 가구 화면으로 이동
    }

    public void OnGoToMyRoomClicked()
    {
        // TODO: 내 방 화면으로 이동
    }

    public void OnGoToNewProjectClicked()
    {
        // TODO: 새 프로젝트 생성 화면으로 이동
    }
}
