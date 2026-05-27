using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI.ProceduralImage;
using Utils;

public class CommunityPresenter
{
    private const int VIEW_PER_PAGE = 6;
    private CommunityView _view;
    private PostView _postView;
    private int _pageIndex = 0;

    private bool _isLastPage = false;
    private bool _isLoading = false;
    private string _sortBy = "createdAt,desc";
    private string _searchString = "";

    private float _refreshDistance = 100f;
    private float _startOffset = 10f;
    private bool _refreshTriggered = false;
    private bool _isShowingDetails = false;
    private bool _isShowingNewPost = false;
    private bool _isShowingInspect = false;

    private int _selectedCategoryIndex = -1;
    private string _selectedScope = "PUBLIC";
    private List<byte[]> _selectedImageBytes = new List<byte[]>();
    private List<string> _selectedImageFileNames = new List<string>();
    private List<CommunityAddPictureButton> _pictureButtons = new List<CommunityAddPictureButton>();

    private string _filterCategoryName = "";

    private int _currentPostId = -1;
    private int _currentPostMemberId = -1;
    private int _currentMemberId = -1;
    private PostContent _currentPostContent = null;
    private bool _isEditingPost = false;
    private List<string> _retainImageUrls = new List<string>();
    private int? _replyTargetCommentId = null;
    private bool _currentLiked = false;
    private bool _isTogglingLike = false;

    private int? _pendingModel3dId = null;

    private static readonly string[] CategoryApiValues = {"FURNITURE", "INTERIOR", "QUESTION", "REVIEW", "ETC" };

    public CommunityPresenter(CommunityView view, PostView postView)
    {
        _view = view;
        _postView = postView;
        _postView.SetGoToListAction(OnReturnButtonClicked);
        _view.SetFilterPannelActive(false);
    }

    public void OnReturnButtonClicked()
    {
        if (_isShowingInspect)
        {
            _isShowingInspect = false;
            _view.HideInspectPage();
            return;
        }

        if (_isShowingNewPost)
        {
            CloseNewPostPanel();
            return;
        }

        if (_isShowingDetails)
        {
            _isShowingDetails = false;
            CloseDetail();
            return;
        }

        SceneHistory.BackToPrevious();
    }

    public void OnWritePostButtonClicked()
    {
        _isShowingNewPost = true;
        _view.ShowNewPostPanel();
        SpawnEmptyPictureButton();
        OnCategorySelected(0); // default to FURNITURE
    }

    public void OnCategorySelected(int index)
    {
        _selectedCategoryIndex = index;
        _view.UpdateCategoryVisual(index);
    }

    public void OnScopeSelected(string scope)
    {
        _selectedScope = scope;
    }

    public async void OnSubmitNewPostButtonClicked()
    {
        string title = _view.GetPostTitle();
        string content = _view.GetPostContent();

        if (string.IsNullOrEmpty(title))
        {
            PopupView.Instance.ShowMessage("제목을 입력해주세요.");
            return;
        }
        if (string.IsNullOrEmpty(content))
        {
            PopupView.Instance.ShowMessage("내용을 입력해주세요.");
            return;
        }
        if (_selectedCategoryIndex < 0)
        {
            PopupView.Instance.ShowMessage("카테고리를 선택해주세요.");
            return;
        }

        string category = CategoryApiValues[_selectedCategoryIndex];
        string scope = _view.GetSelectedScope();

        PopupView.Instance.SetLoadingPannelActive(true);

        long code;
        if (_isEditingPost)
        {
            (code, _) = await CommunityService.UpdatePost(
                _currentPostId, title, content, category, scope,
                retainImageUrls: _retainImageUrls.Count > 0 ? _retainImageUrls : null,
                images: _selectedImageBytes.Count > 0 ? _selectedImageBytes : null,
                imageFileNames: _selectedImageFileNames.Count > 0 ? _selectedImageFileNames : null
            );
        }
        else
        {
            (code, _) = await CommunityService.CreatePost(
                title, content, category, scope,
                model3dId: _pendingModel3dId,
                images: _selectedImageBytes.Count > 0 ? _selectedImageBytes : null,
                imageFileNames: _selectedImageFileNames.Count > 0 ? _selectedImageFileNames : null
            );
        }

        PopupView.Instance.SetLoadingPannelActive(false);

        if (code == 200 || code == 201)
        {
            CloseNewPostPanel();
            RefreshPosts();
        }
        else
        {
            PopupView.Instance.ShowMessage($"게시글 등록에 실패했습니다. (오류 코드: {code})");
        }
    }

    private void CloseNewPostPanel()
    {
        _isShowingNewPost = false;
        _isEditingPost = false;
        _view.HideNewPostPanel();
        _view.ResetNewPostForm();

        foreach (var btn in _pictureButtons)
            if (btn != null) GameObject.Destroy(btn.gameObject);
        _pictureButtons.Clear();
        _selectedImageBytes.Clear();
        _selectedImageFileNames.Clear();
        _retainImageUrls.Clear();
        _selectedCategoryIndex = -1;
        _selectedScope = "PUBLIC";
        _currentPostId = -1;
        _currentPostContent = null;
        _pendingModel3dId = null;
    }

    private void SpawnEmptyPictureButton()
    {
        var btn = _view.SpawnAddPictureButton();
        _pictureButtons.Add(btn);
        btn.EmptyState.SetActive(true);
        btn.FilledState.SetActive(false);
        btn.AddPictureButton.onClick.AddListener(() => OnAddPictureClicked(btn));
    }

    private void OnAddPictureClicked(CommunityAddPictureButton btn)
    {
        if (NativeFilePicker.IsFilePickerBusy()) return;

        NativeFilePicker.PickFile(path =>
        {
            if (string.IsNullOrEmpty(path)) return;

            byte[] bytes = File.ReadAllBytes(path);
            string fileName = Path.GetFileName(path);

            _selectedImageBytes.Add(bytes);
            _selectedImageFileNames.Add(fileName);

            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            btn.ShowingImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
            btn.EmptyState.SetActive(false);
            btn.FilledState.SetActive(true);
            btn.AddPictureButton.onClick.RemoveAllListeners();
            btn.RemovePictureButton.onClick.AddListener(() => OnRemovePictureClicked(btn));

            if (_selectedImageBytes.Count < 4)
                SpawnEmptyPictureButton();
        }, new string[] { "image/*" });
    }

    private void OnRemovePictureClicked(CommunityAddPictureButton btn)
    {
        if (string.IsNullOrEmpty(btn.ImageUrl))
        {
            int index = _pictureButtons.IndexOf(btn);
            if (index < 0) return;

            int newImageIndex = index - _retainImageUrls.Count;
            if (newImageIndex >= 0 && newImageIndex < _selectedImageBytes.Count)
            {
                _selectedImageBytes.RemoveAt(newImageIndex);
                _selectedImageFileNames.RemoveAt(newImageIndex);
            }
        }
        else
        {
            _retainImageUrls.Remove(btn.ImageUrl);
        }

        _pictureButtons.Remove(btn);
        if (btn != null) GameObject.Destroy(btn.gameObject);

        _view.RebuildPictureButtonLayout();

        int totalImages = _retainImageUrls.Count + _selectedImageBytes.Count;
        int emptyButtonCount = _pictureButtons.Count - totalImages;

        if (emptyButtonCount < 1 && totalImages < 4)
            SpawnEmptyPictureButton();
    }


    public void OnScrollChanged(Vector2 pos)
    {
        if (pos.y <= 0.1f)
        {
            LoadPage();
        }


        // 2. 상단 새로고침 (절대 좌표 기반)
        // content의 y좌표가 0보다 작아질 때가 '오버 스크롤' 상태입니다. (Top Anchor 기준)
        float overScrollY = -_view.GetContentAnchoredY();

        if (overScrollY > _startOffset)
        {
            // (현재 당긴 거리 - 시작 지점) / (목표 거리 - 시작 지점)
            float alpha = (overScrollY - _startOffset) / (_refreshDistance - _startOffset);
            _view.SetRefreshIndicatorAlpha(Mathf.Clamp01(alpha));
        }
        else
        {
            _view.SetRefreshIndicatorAlpha(0);
        }

        if (overScrollY > _refreshDistance)
        {
           _refreshTriggered = true;
        }

        if(overScrollY <= _startOffset && _refreshTriggered)
        {
            RefreshPosts();
        }
    }

    public async void LoadPage()
    {
        if (_isLoading || _isLastPage) return;
        Debug.Log($"Projects View : Loading Page {_pageIndex}");

        _isLoading = true;

        long responseCode;
        string jsonBody;

        (responseCode, jsonBody) = await CommunityService.GetPostsSearch(_pageIndex, VIEW_PER_PAGE, _sortBy, _searchString, _filterCategoryName);

        if (responseCode == 200 && !string.IsNullOrEmpty(jsonBody))
        {
            try
            {
                PostResponse postResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<PostResponse>(jsonBody);
                foreach (var item in postResponse.content)
                {
                    if(item.imageUrl == null)
                    {
                        var postView = _view.CreateNoImagePostView();
                        postView.SetBadgeText(TranslateCategory(item.category));
                        postView.SetTitleText(item.title);
                        postView.SetContentText(item.content);
                        postView.SetInfoText(TranslateInfo(item));
                        postView.GetButton().onClick.AddListener(() => OpenDetail(item.id));
                    }
                    else
                    {
                        var postView = _view.CreateWithImagePostView();
                        postView.SetBadgeText(TranslateCategory(item.category));
                        postView.SetTitleText(item.title);
                        postView.SetContentText(item.content);
                        postView.SetInfoText(TranslateInfo(item));
                        postView.SetThumbnail(item.imageUrl);
                        postView.GetButton().onClick.AddListener(() => OpenDetail(item.id));
                    }
                }

                _isLastPage = postResponse.last;
                _pageIndex++;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        _isLoading = false;
    }

    private void RefreshPosts()
    {
        _refreshTriggered = false;
        _view.ClearPosts();
        _pageIndex = 0;
        _isLastPage = false;
        LoadPage();
    }

    public async void OnModel3dInspectClicked(int model3dId)
    {
        PopupView.Instance.SetLoadingPannelActive(true);
        try
        {
            ModelData modelData = await ModelService.GetModelDataByModelId(model3dId);
            if (modelData == null)
            {
                PopupView.Instance.ShowMessage("모델 데이터를 불러오는 데 실패했습니다.");
                return;
            }

            var (responseCode, modelPath) = await ProjectInspectService.GetModel3DFile(modelData.link);
            if (responseCode != 200)
            {
                PopupView.Instance.ShowMessage("모델 파일을 불러오는 데 실패했습니다.");
                return;
            }

            var (dimCode, dimJson) = await ProjectInspectService.GetModel3DDimension(modelData.id);
            ModelDimension modelDimension = dimCode == 200
                ? JsonConvert.DeserializeObject<ModelDimension>(dimJson)
                : new ModelDimension();

            _view.InspectView.SetContent(modelDimension, modelData);
            _view.InspectView.SpawnModel3D(modelPath);
            _view.ShowInspectPage();
            _isShowingInspect = true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            PopupView.Instance.ShowMessage("상세 보기 로드 중 오류가 발생했습니다.");
        }
        finally
        {
            PopupView.Instance.SetLoadingPannelActive(false);
        }
    }

    public async void OnModel3dARClicked(int model3dId)
    {
        PopupView.Instance.SetLoadingPannelActive(true);
        try
        {
            ModelData modelData = await ModelService.GetModelDataByModelId(model3dId);
            if (modelData == null)
            {
                PopupView.Instance.ShowMessage("모델 데이터를 불러오는 데 실패했습니다.");
                return;
            }

            var (responseCode, modelPath) = await ProjectInspectService.GetModel3DFile(modelData.link);
            if (responseCode != 200)
            {
                PopupView.Instance.ShowMessage("모델 파일을 불러오는 데 실패했습니다.");
                return;
            }

            var (dimCode, dimJson) = await ProjectInspectService.GetModel3DDimension(modelData.id);
            ModelDimension modelDimension = dimCode == 200
                ? JsonConvert.DeserializeObject<ModelDimension>(dimJson)
                : new ModelDimension();

            ARPlaceCore.CurrentModelPath = modelPath;
            ARPlaceCore.CurrentModelDimension = modelDimension;
            SceneHistory.ChangeScene("ARPlace");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            PopupView.Instance.ShowMessage("AR 실행 중 오류가 발생했습니다.");
        }
        finally
        {
            PopupView.Instance.SetLoadingPannelActive(false);
        }
    }

    public void CheckAndHandlePendingModel3dId()
    {
        int pendingId = CommunityService.ConsumePendingModel3dId();
        if (pendingId > 0)
        {
            _pendingModel3dId = pendingId;
            OnWritePostButtonClicked();
        }
    }

    public void OnShowFilterClicked()
    {
        _view.SetFilterPannelActive(true);
    }

    public void OnLatestButtonClicked(ProceduralImage image, TextMeshProUGUI text)
    {
        _view.SetActiveButtonColor(image, text);
        _sortBy = "createdAt,desc";
    }

    public void OnOldestButtonClicked(ProceduralImage image, TextMeshProUGUI text)
    {
        _view.SetActiveButtonColor(image, text);
        _sortBy = "createdAt,asc";
    }

    public void OnFilterCategorySelected(string categoryName)
    {
        _filterCategoryName = categoryName;
        _view.UpdateFilterCategoryVisual(_filterCategoryName);
    }

    public void OnResetFilterButtonClicked()
    {
        _sortBy = "createdAt,desc";
        _filterCategoryName = "";
        _view.ResetFilterCategoryButtons();
        RefreshPosts();
        _view.SetFilterPannelActive(false);
    }

    public void OnApplyFilterButtonClicked()
    {
        _searchString = _view.GetNameFilterText();
        RefreshPosts();
        _view.SetFilterPannelActive(false);
    }

    private string TranslateCategory(string category)
    {
        switch (category)
        {
            case "QUESTION":
                return "질문";
            case "REVIEW":
                return "리뷰";
            case "FURNITURE":
                return "가구";
            case "INTERIOR":
                return "인테리어";
            case "ETC":
                return "기타";
            default:
                Debug.LogWarning($"알 수 없는 카테고리: {category}");
                return category; // 알 수 없는 카테고리는 그대로 반환
        }
    }

    private string TranslateInfo(PostContent content, bool isContainMeberName = true)
    {
        if (isContainMeberName)
        {
            return $"{content.memberName}•{GetRelativeTime(content.createdAt)}•조회 {content.viewCount}•댓글 {content.commentCount}•좋아요 {content.likeCount}";
        }
        return $"{GetRelativeTime(content.createdAt)}•조회 {content.viewCount}•댓글 {content.commentCount}•좋아요 {content.likeCount}";
    }

    private string GetRelativeTime(string isoDateTime)
    {
        if (string.IsNullOrEmpty(isoDateTime)) return "시간 정보 없음";

        // 1. ISO 8601 문자열을 DateTime 객체로 변환
        if (!DateTime.TryParse(isoDateTime, out DateTime dateTime))
        {
            return "알 수 없음";
        }

        // 2. 현재 시간과의 차이 계산
        TimeSpan timeSpan = DateTime.Now - dateTime;

        // 3. 차이에 따른 문자열 반환 (조건문 순서가 중요합니다)
        if (timeSpan.TotalSeconds < 60)
        {
            return "방금 전";
        }
        if (timeSpan.TotalMinutes < 60)
        {
            return $"{(int)timeSpan.TotalMinutes}분 전";
        }
        if (timeSpan.TotalHours < 24)
        {
            return $"{(int)timeSpan.TotalHours}시간 전";
        }
        if (timeSpan.TotalDays < 7)
        {
            return $"{(int)timeSpan.TotalDays}일 전";
        }
        if (timeSpan.TotalDays < 31)
        {
            return $"{(int)Math.Ceiling(timeSpan.TotalDays / 7)}주 전";
        }

        // 한 달이 넘어가면 날짜 그대로 표시 (예: 2026-03-01)
        return dateTime.ToString("yyyy-MM-dd");
    }

    public async void OpenDetail(int postId)
    {
        if (_isShowingDetails) return;
        _isShowingDetails = true;
        _currentPostId = postId;
        _replyTargetCommentId = null;
        _view.ClearCommentInput();
        _view.ResetCommentInputPlaceholder();

        long responseCode;
        string jsonBody;

        var (currentMemberCode, currentMemberJson) = await MemberService.GetMemberJSONByMe();
        if (currentMemberCode == 200 && !string.IsNullOrEmpty(currentMemberJson))
        {
            try
            {
                MemberDto currentMember = JsonConvert.DeserializeObject<MemberDto>(currentMemberJson);
                _currentMemberId = currentMember.id;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenDetail] Error while loading current member: {ex}");
                _currentMemberId = -1;
            }
        }

        (responseCode, jsonBody) = await CommunityService.GetPostById(postId);
        if (responseCode == 200 && !string.IsNullOrEmpty(jsonBody))
        {
            try
            {
                List<Sprite> postImages = new List<Sprite>();
                PostContent postContent = JsonConvert.DeserializeObject<PostContent>(jsonBody);
                _currentPostContent = postContent;
                _currentPostMemberId = postContent.memberId;

                (responseCode, jsonBody) = await PostCommentService.GetPostCommentsById(postId);
                List<CommentDto> postComments = JsonConvert.DeserializeObject<List<CommentDto>>(jsonBody);
                _postView.ResetPostView();
                _postView.SetPostView(
                        TranslateCategory(postContent.category),
                        postContent.title,
                        postContent.memberName,
                        TranslateInfo(postContent, false),
                        postContent.content,
                        postContent.commentCount,
                        await MemberService.GetMemberProfilePicUrlByMemberId(postContent.memberId)
                    );

                if (_currentMemberId == _currentPostMemberId)
                {
                    _postView.ShowEditDeleteButtons(OnEditButtonClicked, OnDeleteButtonClicked);
                }
                else
                {
                    _postView.HideEditDeleteButtons();
                }

                // Fetch liked status from the new API endpoint
                var (likedStatusCode, likedStatusJson) = await CommunityService.GetPostLikedStatus(postId);
                if (likedStatusCode == 200 && !string.IsNullOrEmpty(likedStatusJson))
                {
                    try
                    {
                        LikedStatusResponse likedStatus = JsonConvert.DeserializeObject<LikedStatusResponse>(likedStatusJson);
                        _currentLiked = likedStatus.liked;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[OpenDetail] Error while parsing liked status: {ex}");
                        _currentLiked = false;
                    }
                }
                else
                {
                    _currentLiked = false;
                }

                _postView.SetLiked(_currentLiked);
                _postView.SetLikeButtonAction(OnLikeToggled);

                var urls = postContent.imageUrls != null && postContent.imageUrls.Count > 0
                    ? postContent.imageUrls
                    : (!string.IsNullOrEmpty(postContent.imageUrl) ? new List<string> { postContent.imageUrl } : null);
                if (urls != null)
                    foreach (var url in urls)
                        _postView.AddContentImage(url);

                if (postContent.model3dId.HasValue)
                {
                    int capturedModelId = postContent.model3dId.Value;
                    string thumbnailUrl = null;
                    ModelData modelData = null;
                    try
                    {
                        modelData = await ModelService.GetModelDataByModelId(capturedModelId);
                        thumbnailUrl = modelData?.thumbnailUrl;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[OpenDetail] 모델 데이터 로드 실패: {ex.Message}");
                    }
                    _postView.AddModel3dCard(
                        capturedModelId,
                        postContent.model3dName,
                        onInspect: () => OnModel3dInspectClicked(capturedModelId),
                        onAR: () => OnModel3dARClicked(capturedModelId),
                        thumbnailUrl: thumbnailUrl,
                        modelData: modelData
                    );
                }

                RenderComments(postComments);

                _postView.ShowPostView();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenDetail] Error while loading post detail : {ex}");
            }
        }
    }

    private void CloseDetail()
    {
        _isShowingDetails = false;
        _currentPostId = -1;
        _replyTargetCommentId = null;
        _view.ClearCommentInput();
        _view.ResetCommentInputPlaceholder();
        _postView.HidePostView();
    }

    public async void OnSubmitCommentClicked()
    {
        if (_currentPostId < 0) return;

        string content = _view.GetCommentInputText();
        if (string.IsNullOrWhiteSpace(content))
        {
            PopupView.Instance.ShowMessage("댓글 내용을 입력해주세요.");
            return;
        }

        PopupView.Instance.SetLoadingPannelActive(true);
        var (code, _) = await CommunityService.CreateComment(_currentPostId, content, _replyTargetCommentId);
        PopupView.Instance.SetLoadingPannelActive(false);

        if (code == 200 || code == 201)
        {
            _view.ClearCommentInput();
            _replyTargetCommentId = null;
            _view.ResetCommentInputPlaceholder();
            await ReloadComments();
        }
        else
        {
            PopupView.Instance.ShowMessage($"댓글 등록에 실패했습니다. (오류 코드: {code})");
        }
    }

    private async void OnLikeToggled(bool newLiked)
    {
        if (_currentPostId < 0 || _isTogglingLike) return;
        if (newLiked == _currentLiked) return;

        _isTogglingLike = true;
        _currentLiked = newLiked;
        _postView.SetLiked(newLiked);

        var (code, _) = newLiked
            ? await CommunityService.LikePost(_currentPostId)
            : await CommunityService.UnlikePost(_currentPostId);

        if (code != 200 && code != 201 && code != 204)
        {
            _currentLiked = !newLiked;
            _postView.SetLiked(_currentLiked);
            PopupView.Instance.ShowMessage($"좋아요 처리에 실패했습니다. (오류 코드: {code})");
        }

        _isTogglingLike = false;
    }

    private void OnReplyButtonClicked(int commentId, string userName)
    {
        _replyTargetCommentId = commentId;
        _view.SetCommentInputPlaceholder($"@{userName}님에게 답글 작성");
        _view.FocusCommentInput();
    }

    private void OnDeleteButtonClicked()
    {
        PopupView.Instance.Presenter.ShowYesNo(
            "포스트를 삭제하시겠습니까?",
            async () => await DeletePost()
        );
    }

    private async System.Threading.Tasks.Task DeletePost()
    {
        if (_currentPostId < 0) return;

        PopupView.Instance.SetLoadingPannelActive(true);
        var (code, _) = await CommunityService.DeletePost(_currentPostId);
        PopupView.Instance.SetLoadingPannelActive(false);

        if (code == 200 || code == 204)
        {
            CloseDetail();
            RefreshPosts();
        }
        else
        {
            PopupView.Instance.ShowMessage($"포스트 삭제에 실패했습니다. (오류 코드: {code})");
        }
    }

    private void OnEditButtonClicked()
    {
        if (_currentPostContent == null) return;

        int postIdToEdit = _currentPostId;
        _isShowingDetails = false;
        _postView.HidePostView();
        _isEditingPost = true;
        _isShowingNewPost = true;
        _view.ShowNewPostPanel();

        _view.SetPostForm(_currentPostContent.title, _currentPostContent.content);

        int categoryIndex = System.Array.IndexOf(CategoryApiValues, _currentPostContent.category);
        OnCategorySelected(categoryIndex >= 0 ? categoryIndex : 0);
        OnScopeSelected(_currentPostContent.visibilityScope);

        _selectedImageBytes.Clear();
        _selectedImageFileNames.Clear();
        _retainImageUrls.Clear();

        if (_currentPostContent.imageUrls != null && _currentPostContent.imageUrls.Count > 0)
        {
            _retainImageUrls.AddRange(_currentPostContent.imageUrls);
        }
        else if (!string.IsNullOrEmpty(_currentPostContent.imageUrl))
        {
            _retainImageUrls.Add(_currentPostContent.imageUrl);
        }

        foreach (var btn in _pictureButtons)
            if (btn != null) GameObject.Destroy(btn.gameObject);
        _pictureButtons.Clear();

        _currentPostId = postIdToEdit;

        SpawnExistingPictureButtons();
    }

    private async void SpawnExistingPictureButtons()
    {
        foreach (var imageUrl in _retainImageUrls)
        {
            var btn = _view.SpawnAddPictureButton();
            _pictureButtons.Add(btn);
            btn.SetImageUrl(imageUrl);

            Sprite sprite = await ImageUtils.LoadSpriteFromUrlAsync(imageUrl);
            if (sprite != null)
            {
                btn.ShowingImage.sprite = sprite;
                btn.EmptyState.SetActive(false);
                btn.FilledState.SetActive(true);
                btn.RemovePictureButton.onClick.AddListener(() => OnRemovePictureClicked(btn));
            }
        }

        if (_retainImageUrls.Count < 4)
            SpawnEmptyPictureButton();
    }

    public void OnCommentInputFieldFocused(string currentText)
    {
        if (PopupView.Instance == null) return;
        PopupView.Instance.SetKeyboardHelperActive(true);
        PopupView.Instance.SetKeyboardHelperContent(currentText);
    }

    public void OnCommentInputFieldValueChanged(string text)
    {
        if (PopupView.Instance == null) return;
        PopupView.Instance.SetKeyboardHelperContent(text);
    }

    public void OnCommentInputFieldEndEdit(string _)
    {
        if (PopupView.Instance == null) return;
        PopupView.Instance.SetKeyboardHelperActive(false);
    }

    private async System.Threading.Tasks.Task ReloadComments()
    {
        if (_currentPostId < 0) return;

        var (code, json) = await PostCommentService.GetPostCommentsById(_currentPostId);
        if (code != 200 || string.IsNullOrEmpty(json)) return;

        try
        {
            List<CommentDto> comments = JsonConvert.DeserializeObject<List<CommentDto>>(json);
            _postView.SetCommentCount(comments.Count);
            RenderComments(comments);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void RenderComments(List<CommentDto> comments)
    {
        _postView.ClearComments();
        if (comments == null) return;

        foreach (var comment in OrderForThreadView(comments))
            _postView.AddComment(comment, OnReplyButtonClicked);
    }

    // Server returns comments as a flat list with no guaranteed ordering between
    // top-level comments and their replies. Re-organize so each thread renders as
    // [root, reply, reply, ...] grouped together. Replies are walked up the parent
    // chain to their root so any depth collapses into the same group; orphan
    // replies (parent missing from the response) are appended at the end.
    private static IEnumerable<CommentDto> OrderForThreadView(List<CommentDto> comments)
    {
        var byId = new Dictionary<int, CommentDto>(comments.Count);
        foreach (var c in comments) byId[c.id] = c;

        DateTime CreatedAt(CommentDto c) =>
            DateTime.TryParse(c.createdAt, out var dt) ? dt : DateTime.MinValue;

        int RootIdOf(CommentDto c)
        {
            var current = c;
            var seen = new HashSet<int> { current.id };
            while (current.parentCommentId.HasValue
                   && byId.TryGetValue(current.parentCommentId.Value, out var parent)
                   && seen.Add(parent.id))
            {
                current = parent;
            }
            return current.id;
        }

        var grouped = new Dictionary<int, List<CommentDto>>();
        var orphans = new List<CommentDto>();
        foreach (var c in comments)
        {
            if (c.parentCommentId.HasValue && !byId.ContainsKey(c.parentCommentId.Value))
            {
                orphans.Add(c);
                continue;
            }
            int rootId = RootIdOf(c);
            if (!grouped.TryGetValue(rootId, out var bucket))
            {
                bucket = new List<CommentDto>();
                grouped[rootId] = bucket;
            }
            bucket.Add(c);
        }

        var orderedRootIds = grouped.Keys
            .OrderBy(id => CreatedAt(byId[id]))
            .ToList();

        foreach (var rootId in orderedRootIds)
        {
            var bucket = grouped[rootId];
            yield return byId[rootId];
            foreach (var reply in bucket.Where(c => c.id != rootId).OrderBy(CreatedAt))
                yield return reply;
        }

        foreach (var orphan in orphans.OrderBy(CreatedAt))
            yield return orphan;
    }
}
