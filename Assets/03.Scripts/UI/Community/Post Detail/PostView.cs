using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
using Utils;

public class PostView : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private PostCommentView _commentViewPrefab;
    [SerializeField] private PostImageView _imageViewPrefab;

    [Header("Components")]
    [SerializeField] private RectTransform _postDetailPanel;
    [SerializeField] private ScrollRect _postDetailScrollViewContent;
    [SerializeField] private TextMeshProUGUI _badgeText;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private ProceduralImage _profilePicture;
    [SerializeField] private TextMeshProUGUI _userNameText;
    [SerializeField] private TextMeshProUGUI _infoText;
    [SerializeField] private GameObject _imagesParent;
    [SerializeField] private Button _likeButton;
    [SerializeField] private Sprite _defaultLikeSprite;
    [SerializeField] private Sprite _filledLikeSprite;
    [SerializeField] private Image _likeButtonImage;
    [SerializeField] private TextMeshProUGUI _contentText;
    [SerializeField] private Button _goToListButton;
    [SerializeField] private TextMeshProUGUI _commentsTitleText;
    [SerializeField] private GameObject _commentsParent;

    [Header("Settings")]
    [SerializeField] private float _hiddenLeftOffset = 500f;  // 숨겼을 때 Left 값
    [SerializeField] private float _hiddenRightOffset = -500f; // 숨겼을 때 Right 값
    [SerializeField] private float _duration;
    [SerializeField] private Sprite _defaultProfilePic;

    private Coroutine _activeCoroutine;

    /// <summary>
    /// 포스트 세부사항 뷰를 설정합니다
    /// </summary>
    /// <param name="category"></param>
    /// <param name="title"></param>
    /// <param name="profilePic"></param>
    /// <param name="userName"></param>
    /// <param name="info"></param>
    /// <param name="images"></param>
    /// <param name="content"></param>
    /// <param name="commentsCount"></param>
    /// <param name="comments"></param>
    public void SetPostView(string category, string title, string userName, string info, string content, int commentsCount = 0, string userProfilePic = null)
    {
        if(_badgeText) _badgeText.text = category;
        if(_titleText) _titleText.text = title;
        if(_userNameText) _userNameText.text = userName;
        if(_infoText) _infoText.text = info;
        if(_contentText) _contentText.text = content;
        if (_commentsTitleText) _commentsTitleText.text = $"댓글 {commentsCount}";

        if (userProfilePic != null && _profilePicture) SetProfilePicture(userProfilePic);
        else _profilePicture.sprite = _defaultProfilePic;
    }

    public void SetGoToListAction(Action action)
    {
        _goToListButton.onClick.RemoveAllListeners();
        _goToListButton.onClick.AddListener(() => action());
    }

    public void SetLiked(bool liked)
    {
        if (_likeButtonImage)
            _likeButtonImage.sprite = liked ? _filledLikeSprite : _defaultLikeSprite;
    }

    public void SetLikeButtonAction(Action<bool> onToggle)
    {
        if (!_likeButton) return;
        _likeButton.onClick.RemoveAllListeners();
        _likeButton.onClick.AddListener(() =>
        {
            bool newLiked = _likeButtonImage != null && _likeButtonImage.sprite != _filledLikeSprite;
            onToggle?.Invoke(newLiked);
        });
    }

    public void ResetPostView()
    {
        foreach(Transform child in _imagesParent.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        foreach(Transform child in _commentsParent.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        _postDetailScrollViewContent.verticalNormalizedPosition = 1f;
    }

    public async void SetProfilePicture(string imageUrl)
    {
        if(_profilePicture)
        {
            _profilePicture.sprite = await ImageUtils.LoadSpriteFromUrlAsync(imageUrl);
        }
    }

    public async void AddContentImage(string imageUrl)
    {
        Sprite contentImage = await ImageUtils.LoadSpriteFromUrlAsync(imageUrl);
        if (contentImage != null)
        {
            var clone = Instantiate(_imageViewPrefab, _imagesParent.transform);
            clone.UpdateImage(contentImage);
        }
    }

    public void AddComment(CommentDto commentDto, Action<int, string> replyCallback = null)
    {
        var clone = Instantiate(_commentViewPrefab, _commentsParent.transform);
        clone.Presenter.SetComment(commentDto, replyCallback);
    }

    public void ClearComments()
    {
        foreach (Transform child in _commentsParent.transform)
            GameObject.Destroy(child.gameObject);
    }

    public void SetCommentCount(int count)
    {
        if (_commentsTitleText) _commentsTitleText.text = $"댓글 {count}";
    }

    /// <summary>
    /// 포스트 세부사항 뷰를 슬라이드-인 하여 표출합니다.
    /// </summary>
    public void ShowPostView()
    {
        SafeStartCoroutine(SlideRoutine(0f, 0f));
    }

    /// <summary>
    /// 포스트 세부사항 뷰를 슬라이드-아웃 시킵니다.
    /// </summary>
    public void HidePostView()
    {
        SafeStartCoroutine(SlideRoutine(_hiddenLeftOffset, _hiddenRightOffset));
    }

    private void SafeStartCoroutine(IEnumerator routine)
    {
        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        _activeCoroutine = StartCoroutine(routine);
    }

    private IEnumerator SlideRoutine(float targetLeft, float targetRight)
    {
        float elapsed = 0f;

        Vector2 startOffsetMin = _postDetailPanel.offsetMin;
        Vector2 startOffsetMax = _postDetailPanel.offsetMax;

        // 위아래(Y값)는 유지하고 가로(X값)만 목표치로 변경
        Vector2 endOffsetMin = new Vector2(targetLeft, startOffsetMin.y);
        Vector2 endOffsetMax = new Vector2(targetRight, startOffsetMax.y);

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            // MG한 느낌을 위해 0.4초 정도가 딱 적당하더라! ✨
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _duration);

            _postDetailPanel.offsetMin = Vector2.Lerp(startOffsetMin, endOffsetMin, t);
            _postDetailPanel.offsetMax = Vector2.Lerp(startOffsetMax, endOffsetMax, t);

            yield return null;
        }

        _postDetailPanel.offsetMin = endOffsetMin;
        _postDetailPanel.offsetMax = endOffsetMax;

        _activeCoroutine = null;
    }
}
