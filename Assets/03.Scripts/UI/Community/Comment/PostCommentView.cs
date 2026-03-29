using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

public class PostCommentView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private ProceduralImage _profilePictureImage;
    [SerializeField] private TextMeshProUGUI _userNameText;
    [SerializeField] private TextMeshProUGUI _commentContentText;
    [SerializeField] private Button _addReplyButton;
    [SerializeField] private VerticalLayoutGroup _verticalLayoutGroup;

    [Header("Settings")]
    [SerializeField] private int _replyLeftPadding = 24;
    [SerializeField] private Sprite _defaultProfilePic;

    private PostCommentPresenter _presenter;
    public PostCommentPresenter Presenter => _presenter;

    private void Awake()
    {
        _presenter = new PostCommentPresenter(this, _replyLeftPadding);
    }

    private void OnDestroy()
    {
        if(_addReplyButton) _addReplyButton.onClick.RemoveAllListeners();
    }

    public void SetUserNameText(string content)
    {
        if (_userNameText) _userNameText.text = content;
    }
    
    public void SetProfilePicture(Sprite sprite)
    {
        if(_profilePictureImage) _profilePictureImage.sprite = sprite;
    }

    public void SetProfilePicture()
    {
        if(_profilePictureImage) _profilePictureImage.sprite = _defaultProfilePic;
    }

    public async void SetProfilePicture(string imageSourceURL)
    {
        Sprite sprite = await Utils.ImageUtils.LoadSpriteFromUrlAsync(imageSourceURL);
        if(sprite && _profilePictureImage) _profilePictureImage.sprite = sprite;
    }

    public void SetCommentContentText(string content)
    {
        if(_commentContentText) _commentContentText.text = content;
    }

    public void SetAddReplyButtonAction(UnityAction action)
    {
        if (_addReplyButton) _addReplyButton.onClick.AddListener(action);
    }

    public void SetLeftPaddingg(int leftPadding)
    {
        if (_verticalLayoutGroup) _verticalLayoutGroup.padding.left = leftPadding;
    }
}
