using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

public class MyPagePresenter
{
    private readonly MyPageView _view;

    private enum PostListMode { MyPosts, LikedPosts }
    private PostListMode _postListMode;
    private int _postListPageIndex = 0;
    private bool _postListIsLastPage = false;
    private bool _postListIsLoading = false;
    private const int POST_PAGE_SIZE = 10;
    private const string SORT = "createdAt,desc";

    public MyPagePresenter(MyPageView view)
    {
        _view = view;
    }

    public async void InitializeView()
    {
        SetUserInfo();
        _view.SetActiveMainPage(true);
        _view.SetActiveProfileEditPage(false);
        _view.SetProfilePicture(await MemberService.GetMemberProfilePicUrlByMemberId(int.Parse(Utils.JWTUtils.GetUserId())));
    }

    public void OnEditProfileClicked()
    {
        _view.SetActiveMainPage(false);
        _view.SetActiveProfileEditPage(true);
    }

    public async void SetUserInfo()
    {
        string userName = await MemberService.GetMyName();
        var (res, data) = await MemberService.GetMyActivityCount();
        string result = $"{userName}\r\n<size=60%><color=#757575>모델 {data.model3dCount} • 게시물 {data.postCount} • 댓글 {data.commentCount}</color>";
        _view.SetUserInfo(result);
        _view.SetNicknameInput(userName);
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
        OpenPostList(PostListMode.MyPosts, "내 게시글");
    }

    public void OnGoToMyLikeClicked()
    {
        OpenPostList(PostListMode.LikedPosts, "좋아요한 게시글");
    }

    private void OpenPostList(PostListMode mode, string title)
    {
        _postListMode = mode;
        _postListPageIndex = 0;
        _postListIsLastPage = false;
        _view.ClearPostListItems();
        _view.SetActiveMainPage(false);
        _view.SetActivePostListPage(true, title);
        LoadPostListPage();
    }

    public void OnPostListBackButtonClicked()
    {
        _view.SetActivePostListPage(false);
        _view.ClearPostListItems();
        _view.SetActiveMainPage(true);
    }

    public void OnPostListScrollChanged(UnityEngine.Vector2 pos)
    {
        if (pos.y <= 0.1f)
            LoadPostListPage();
    }

    private async void LoadPostListPage()
    {
        if (_postListIsLoading || _postListIsLastPage) return;
        _postListIsLoading = true;

        try
        {
            long code;
            string json;

            if (_postListMode == PostListMode.MyPosts)
                (code, json) = await CommunityService.GetMyPosts(_postListPageIndex, POST_PAGE_SIZE, SORT);
            else
                (code, json) = await CommunityService.GetMyLikedPosts(_postListPageIndex, POST_PAGE_SIZE, SORT);

            if (code == 200 && !string.IsNullOrEmpty(json))
            {
                PostResponse response = JsonConvert.DeserializeObject<PostResponse>(json);
                foreach (var item in response.content)
                {
                    var postView = _view.CreatePostListItem();
                    if (postView == null) continue;
                    postView.SetBadgeText(TranslateCategory(item.category));
                    postView.SetTitleText(item.title);
                    postView.SetContentText(item.content);
                    postView.SetInfoText($"{item.memberName}•조회 {item.viewCount}•댓글 {item.commentCount}•좋아요 {item.likeCount}");
                    int postId = item.id;
                    postView.GetButton().onClick.AddListener(() => Utils.SceneHistory.ChangeToCommunityWithPost(postId));
                }
                _postListIsLastPage = response.last;
                _postListPageIndex++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

        _postListIsLoading = false;
    }

    private static string TranslateCategory(string category)
    {
        return category switch
        {
            "QUESTION" => "질문",
            "REVIEW" => "리뷰",
            "FURNITURE" => "가구",
            "INTERIOR" => "인테리어",
            "ETC" => "기타",
            _ => category,
        };
    }

    public void OnGoToMySavedClicked()
    {
        GalleryPresenter.IsBookmarkMode = true;
        Utils.SceneHistory.ChangeScene("Gallery");
    }

    public void OnGoToMyFurnitureClicked()
    {
        Utils.SceneHistory.ChangeScene("Projects");
    }

    public void OnGoToMyRoomClicked()
    {
        Utils.SceneHistory.ChangeScene("RoomPlan");
    }

    public void OnGoToNewProjectClicked()
    {
        Utils.SceneHistory.ChangeScene("Generate3D");
    }
}
