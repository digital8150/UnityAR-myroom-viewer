using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PostCommentPresenter
{
    private PostCommentView _view;
    private int _replyLeftPadding;

    public PostCommentPresenter(PostCommentView view, int replyLeftPadding)
    {
        _view = view;
        _replyLeftPadding = replyLeftPadding;
    }

    public async void SetComment(CommentDto commentDto)
    {
        var (responseCode, jsonBody) = await MemberService.GetMemberByMemberId(commentDto.memberId);
        string profilePictureUrl = null;
        if (responseCode == 200)
        {
            try
            {
                MemberDto member = JsonConvert.DeserializeObject<MemberDto>(jsonBody);
                profilePictureUrl = member.profileImageUrl;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        if (!string.IsNullOrEmpty(profilePictureUrl)) _view.SetProfilePicture(profilePictureUrl);
        else _view.SetProfilePicture();

        _view.SetUserNameText(TranslateUsernameText(commentDto));
        _view.SetCommentContentText(commentDto.content);
        
        if(commentDto.parentCommentId != null)
        {
            _view.SetLeftPaddingg(_replyLeftPadding);
        }
    }

    private string TranslateUsernameText(CommentDto commentDto)
    {
        return $"{commentDto.memberName} <size=80%><color=#828C94> <font=\"Pretendard-Regular SDF\">{GetRelativeTime(commentDto.createdAt)}</font></size></color>";
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
}