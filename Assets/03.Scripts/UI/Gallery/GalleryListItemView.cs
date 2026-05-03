using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GalleryListItemView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Image _thumbnbailImage;
    [SerializeField] private AspectRatioFitter _thumbnailImageARF;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _infoText;
    [SerializeField] private TextMeshProUGUI _badgeText;
    [SerializeField] private Button _goToInspect;
    [SerializeField] private Button _goToAR;

    private void OnDestroy()
    {
        if (_goToInspect)
        {
            _goToInspect.onClick.RemoveAllListeners();
        }

        if (_goToAR)
        {
            _goToAR.onClick.RemoveAllListeners();
        }
    }

    #region Image
    /// <summary>
    /// 썸네일 이미지를 Sprite으로 설정합니다.
    /// </summary>
    /// <param name="thumbnail">설정할 Sprite</param>
    public void SetThumbnail(Sprite thumbnail)
    {
        if (!_thumbnailImageARF || !_thumbnbailImage)
        {
            Debug.LogError("[GalleryListItemView] Thumbnail components are not assigned.");
            return;
        }

        _thumbnbailImage.sprite = thumbnail;
        if (thumbnail != null)
        {
            _thumbnailImageARF.aspectRatio = (float)thumbnail.texture.width / thumbnail.texture.height;
        }
    }

    /// <summary>
    /// URL에서 썸네일 이미지를 비동기로 로드하여 설정합니다.
    /// </summary>
    /// <param name="imageUrl">로드할 이미지의 URL</param>
    public async void SetThumbnail(string imageUrl)
    {
        Sprite thumbnail = await Utils.ImageUtils.LoadSpriteFromUrlAsync(imageUrl);
        if (thumbnail != null)
        {
            SetThumbnail(thumbnail);
        }
        else
        {
            Debug.LogError($"[GalleryListItemView] Failed to load thumbnail from URL: {imageUrl}");
        }
    }
    #endregion

    #region Text
    /// <summary>
    /// 제목 텍스트를 설정합니다.
    /// </summary>
    /// <param name="text">설정할 제목</param>
    public void SetTitleText(string text)
    {
        if (!_titleText)
        {
            Debug.LogError("[GalleryListItemView] Title Text component is not assigned.");
            return;
        }
        _titleText.text = text;
    }

    /// <summary>
    /// 정보 텍스트를 설정합니다.
    /// </summary>
    /// <param name="text">설정할 정보 텍스트</param>
    public void SetInfoText(string text)
    {
        if (!_infoText)
        {
            Debug.LogError("[GalleryListItemView] Info Text component is not assigned.");
            return;
        }
        _infoText.text = text;
    }

    /// <summary>
    /// ModelData를 받아서 정보 텍스트를 업데이트합니다. (사용자명 • 생성시간 • 조회 • 좋아요)
    /// </summary>
    /// <param name="data">모델 데이터</param>
    public async void SetInfoTextWithDTO(ModelData data)
    {
        if (!_infoText)
        {
            Debug.LogError("[GalleryListItemView] Info Text component is not assigned.");
            return;
        }

        if (data == null)
        {
            Debug.LogError("[GalleryListItemView] ModelData is null.");
            return;
        }

        try
        {
            string username = await MemberService.GetMemberUsernameByMemberId(data.creatorId);
            if (string.IsNullOrEmpty(username))
            {
                username = "알 수 없음";
            }

            string relativeTime = GetRelativeTime(data.createdAt);

            _infoText.text = $"{username} • {relativeTime}";
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GalleryListItemView] Error setting info text with DTO: {ex.Message}");
        }
    }

    /// <summary>
    /// ISO 8601 형식의 시간을 상대 시간으로 변환합니다. (예: "4분 전")
    /// </summary>
    /// <param name="createdAtISO">ISO 8601 형식의 시간 문자열</param>
    /// <returns>상대 시간 문자열</returns>
    private string GetRelativeTime(string createdAtISO)
    {
        if (string.IsNullOrEmpty(createdAtISO))
        {
            return "방금";
        }

        try
        {
            DateTime createdAt = DateTime.Parse(createdAtISO);
            TimeSpan timeDifference = DateTime.UtcNow - createdAt.ToUniversalTime();

            if (timeDifference.TotalSeconds < 60)
                return "방금";
            else if (timeDifference.TotalMinutes < 60)
                return $"{(int)timeDifference.TotalMinutes}분 전";
            else if (timeDifference.TotalHours < 24)
                return $"{(int)timeDifference.TotalHours}시간 전";
            else if (timeDifference.TotalDays < 7)
                return $"{(int)timeDifference.TotalDays}일 전";
            else if (timeDifference.TotalDays < 30)
                return $"{(int)(timeDifference.TotalDays / 7)}주 전";
            else if (timeDifference.TotalDays < 365)
                return $"{(int)(timeDifference.TotalDays / 30)}개월 전";
            else
                return $"{(int)(timeDifference.TotalDays / 365)}년 전";
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GalleryListItemView] Error parsing date: {ex.Message}");
            return "알 수 없음";
        }
    }

    /// <summary>
    /// 배지 텍스트를 설정합니다.
    /// </summary>
    /// <param name="text">설정할 배지 텍스트</param>
    public void SetBadgeText(string text)
    {
        if (!_badgeText)
        {
            Debug.LogError("[GalleryListItemView] Badge Text component is not assigned.");
            return;
        }
        _badgeText.text = text;
    }
    #endregion

    #region Button Actions
    /// <summary>
    /// "검사" 버튼의 클릭 액션을 설정합니다.
    /// </summary>
    /// <param name="action">실행할 액션</param>
    public void SetGoToInspectAction(UnityAction action)
    {
        if (!_goToInspect)
        {
            Debug.LogError("[GalleryListItemView] GoToInspect Button component is not assigned.");
            return;
        }
        _goToInspect.onClick.RemoveAllListeners();
        _goToInspect.onClick.AddListener(action);
    }

    /// <summary>
    /// "AR" 버튼의 클릭 액션을 설정합니다.
    /// </summary>
    /// <param name="action">실행할 액션</param>
    public void SetGoToARAction(UnityAction action)
    {
        if (!_goToAR)
        {
            Debug.LogError("[GalleryListItemView] GoToAR Button component is not assigned.");
            return;
        }
        _goToAR.onClick.RemoveAllListeners();
        _goToAR.onClick.AddListener(action);
    }
    #endregion
}
