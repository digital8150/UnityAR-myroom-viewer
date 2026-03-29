using Newtonsoft.Json;
using System.Collections.Generic;
using System;
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

    public CommunityPresenter(CommunityView view, PostView postView)
    {
        _view = view;
        _postView = postView;
    }

    public void OnReturnButtonClicked()
    {
        if (_isShowingDetails)
        {
            _isShowingDetails = false;
            CloseDetail();
            return;
        }

        SceneHistory.BackToPrevious();
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

        (responseCode, jsonBody) = await CommunityService.GetPostsPublic(_pageIndex, VIEW_PER_PAGE, _sortBy);

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
