public class MyPagePresenter
{
    private readonly MyPageView _view;

    public MyPagePresenter(MyPageView view)
    {
        _view = view;
    }

    public async void InitializeView()
    {
        _view.SetActiveMainPage(true);
        _view.SetActiveProfileEditPage(false);
        _view.SetUserInfo(await MemberService.GetMyName());
        _view.SetProfilePicture(await MemberService.GetMemberProfilePicUrlByMemberId(int.Parse(Utils.JWTUtils.GetUserId())));
    }

    public void OnEditProfileClicked()
    {
        _view.SetActiveMainPage(false);
        _view.SetActiveProfileEditPage(true);
    }

    public async void OnSaveProfileClicked()
    {
        string nickname = _view.GetNicknameInput();
        string email = Utils.JWTUtils.GetEmail();

        if (!int.TryParse(Utils.JWTUtils.GetUserId(), out int memberId))
        {
            UnityEngine.Debug.LogError("MyPagePresenter: Failed to parse memberId from JWT.");
            return;
        }

        var (responseCode, member) = await MemberService.UpdateMember(memberId, nickname, email);

        if (responseCode == 200 && member != null)
        {
            _view.SetUserInfo(member.username);
        }
        else
        {
            UnityEngine.Debug.LogError($"MyPagePresenter: UpdateMember failed with code {responseCode}.");
            _view.SetUserInfo(nickname);
        }

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
        if (NativeFilePicker.IsFilePickerBusy())
        {
            PopupView.Instance.ShowMessage("파일 선택기가 현재 사용 중입니다. 잠시 후 다시 시도해주세요.");
            return;
        }

        try
        {
            NativeFilePicker.PickFile(async (path) =>
            {
                if (path == null) return;

                string lowerPath = path.ToLower();
                if (!lowerPath.EndsWith(".png") && !lowerPath.EndsWith(".jpg") && !lowerPath.EndsWith(".jpeg"))
                {
                    PopupView.Instance.ShowMessage("유효하지 않은 파일 형식입니다. PNG, JPG, JPEG 파일만 선택해주세요.");
                    return;
                }

                PopupView.Instance.SetLoadingPannelActive(true);

                byte[] bytes = await System.IO.File.ReadAllBytesAsync(path);
                long responseCode = await MemberService.UpdateProfileImage(bytes);

                if (responseCode == 200)
                {
                    var texture = new UnityEngine.Texture2D(2, 2);
                    if (UnityEngine.ImageConversion.LoadImage(texture, bytes))
                    {
                        var sprite = UnityEngine.Sprite.Create(
                            texture,
                            new UnityEngine.Rect(0, 0, texture.width, texture.height),
                            new UnityEngine.Vector2(0.5f, 0.5f));
                        _view.SetProfilePicture(sprite);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(texture);
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError($"[MyPagePresenter] UpdateProfileImage failed. responseCode: {responseCode}");
                    PopupView.Instance.ShowMessage("프로필 사진 업로드 중 오류가 발생했습니다.");
                }

                PopupView.Instance.SetLoadingPannelActive(false);
            });
        }
        catch (System.Exception e)
        {
            PopupView.Instance.ShowMessage($"파일 선택 중 오류가 발생했습니다: {e.Message}");
        }
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
