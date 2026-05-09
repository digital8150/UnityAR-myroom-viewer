using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

public class CommunityPresenter
{
    private const int VIEW_PER_PAGE = 6;
    private CommunityView _view;
    private PostView _postView;
    private int _pageIndex = 0;

    private bool _isLastPage = false;
    private bool _isLoading = false;
    private string _sortBy = "";

    private float _refreshDistance = 100f;
    private float _startOffset = 10f;
    private bool _refreshTriggered = false;
    private bool _isShowingDetails = false;
    private bool _isShowingNewPost = false;

    private int _selectedCategoryIndex = -1;
    private string _selectedScope = "PUBLIC";
    private List<byte[]> _selectedImageBytes = new List<byte[]>();
    private List<string> _selectedImageFileNames = new List<string>();
    private List<CommunityAddPictureButton> _pictureButtons = new List<CommunityAddPictureButton>();

    private static readonly string[] CategoryApiValues = { "QUESTION", "REVIEW", "FURNITURE", "INTERIOR", "ETC" };

    public CommunityPresenter(CommunityView view, PostView postView)
    {
        _view = view;
        _postView = postView;
        _postView.SetGoToListAction(OnReturnButtonClicked);
    }

    public void OnReturnButtonClicked()
    {
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
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content) || _selectedCategoryIndex < 0)
            return;

        string category = CategoryApiValues[_selectedCategoryIndex];
        string scope = _view.GetSelectedScope();

        (long code, _) = await CommunityService.CreatePost(
            title, content, category, scope,
            images: _selectedImageBytes.Count > 0 ? _selectedImageBytes : null,
            imageFileNames: _selectedImageFileNames.Count > 0 ? _selectedImageFileNames : null
        );

        if (code == 200 || code == 201)
        {
            CloseNewPostPanel();
            RefreshPosts();
        }
    }

    private void CloseNewPostPanel()
    {
        _isShowingNewPost = false;
        _view.HideNewPostPanel();
        _view.ResetNewPostForm();

        foreach (var btn in _pictureButtons)
            if (btn != null) GameObject.Destroy(btn.gameObject);
        _pictureButtons.Clear();
        _selectedImageBytes.Clear();
        _selectedImageFileNames.Clear();
        _selectedCategoryIndex = -1;
        _selectedScope = "PUBLIC";
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
        int index = _pictureButtons.IndexOf(btn);
        if (index < 0 || index >= _selectedImageBytes.Count) return;

        _selectedImageBytes.RemoveAt(index);
        _selectedImageFileNames.RemoveAt(index);
        RebuildPictureButtons();
    }

    private void RebuildPictureButtons()
    {
        foreach (var b in _pictureButtons)
            if (b != null) GameObject.Destroy(b.gameObject);
        _pictureButtons.Clear();

        for (int i = 0; i < _selectedImageBytes.Count; i++)
        {
            var btn = _view.SpawnAddPictureButton();
            _pictureButtons.Add(btn);
            int idx = i;
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(_selectedImageBytes[idx]);
            btn.ShowingImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
            btn.EmptyState.SetActive(false);
            btn.FilledState.SetActive(true);
            btn.RemovePictureButton.onClick.AddListener(() => OnRemovePictureClicked(btn));
        }

        if (_selectedImageBytes.Count < 4)
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

        (responseCode, jsonBody) = await CommunityService.GetPostsSearch(_pageIndex, VIEW_PER_PAGE, _sortBy);

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

    private async void OpenDetail(int postId)
    {
        if (_isShowingDetails) return;
        _isShowingDetails = true;

        long responseCode;
        string jsonBody;

        (responseCode, jsonBody) = await CommunityService.GetPostById(postId);
        if (responseCode == 200 && !string.IsNullOrEmpty(jsonBody))
        {
            try
            {
                List<Sprite> postImages = new List<Sprite>();
                PostContent postContent = JsonConvert.DeserializeObject<PostContent>(jsonBody);

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

                _postView.AddContentImage(postContent.imageUrl);

                foreach (var comment in postComments)
                {
                    _postView.AddComment(comment);
                }

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
        _postView.HidePostView();
    }
}
