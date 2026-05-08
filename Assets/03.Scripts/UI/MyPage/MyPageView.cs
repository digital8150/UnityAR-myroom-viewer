using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class MyPageView : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private GameObject _mainPage;
    [SerializeField] private GameObject _profileEditPage;

    [Header("Page 1 : Main Page")]
    [SerializeField] private Button _backButton;
    [SerializeField] private Image _profilePictureImage;
    [SerializeField] private AspectRatioFitter _profilePictureImageARF;
    [SerializeField] private TextMeshProUGUI _userInfoText;
    [SerializeField] private Button _editProfileButton;
    [SerializeField] private Button _goToMyPost;
    [SerializeField] private Button _goToMyLike;
    [SerializeField] private Button _goToMySaved;
    [SerializeField] private Button _goToMyFurniture;
    [SerializeField] private Button _goToMyRoom;
    [SerializeField] private Button _goToNewProject;

    [Header("Page 2 : Profile Edit Page")]
    [SerializeField] private Button _saveProfileButton;
    [SerializeField] private Button _cancelEditButton;
    [SerializeField] private TMP_InputField _nicknameInputField;
    [SerializeField] private Button _changeProfilePictureButton;

    private MyPagePresenter _presenter;

    #region Unity Life Cycle

    private void Awake()
    {
        _presenter = new MyPagePresenter(this);

        if (_backButton) _backButton.onClick.AddListener(_presenter.OnBackButtonClicked);
        if (_editProfileButton) _editProfileButton.onClick.AddListener(_presenter.OnEditProfileClicked);
        if (_goToMyPost) _goToMyPost.onClick.AddListener(_presenter.OnGoToMyPostClicked);
        if (_goToMyLike) _goToMyLike.onClick.AddListener(_presenter.OnGoToMyLikeClicked);
        if (_goToMySaved) _goToMySaved.onClick.AddListener(_presenter.OnGoToMySavedClicked);
        if (_goToMyFurniture) _goToMyFurniture.onClick.AddListener(_presenter.OnGoToMyFurnitureClicked);
        if (_goToMyRoom) _goToMyRoom.onClick.AddListener(_presenter.OnGoToMyRoomClicked);
        if (_goToNewProject) _goToNewProject.onClick.AddListener(_presenter.OnGoToNewProjectClicked);
        if (_saveProfileButton) _saveProfileButton.onClick.AddListener(_presenter.OnSaveProfileClicked);
        if (_cancelEditButton) _cancelEditButton.onClick.AddListener(_presenter.OnCancelEditClicked);
        if (_changeProfilePictureButton) _changeProfilePictureButton.onClick.AddListener(_presenter.OnChangeProfilePictureClicked);
        if (_nicknameInputField)
        {
            _nicknameInputField.onSelect.AddListener(_presenter.OnInputFieldFocused);
            _nicknameInputField.onValueChanged.AddListener(_presenter.OnInputFieldValueChanged);
            _nicknameInputField.onEndEdit.AddListener(_presenter.OnInputFieldEndEdit);
        }
        _presenter.InitializeView();
    }

    private void OnDestroy()
    {
        if (_backButton) _backButton.onClick.RemoveAllListeners();
        if (_editProfileButton) _editProfileButton.onClick.RemoveAllListeners();
        if (_goToMyPost) _goToMyPost.onClick.RemoveAllListeners();
        if (_goToMyLike) _goToMyLike.onClick.RemoveAllListeners();
        if (_goToMySaved) _goToMySaved.onClick.RemoveAllListeners();
        if (_goToMyFurniture) _goToMyFurniture.onClick.RemoveAllListeners();
        if (_goToMyRoom) _goToMyRoom.onClick.RemoveAllListeners();
        if (_goToNewProject) _goToNewProject.onClick.RemoveAllListeners();
        if (_saveProfileButton) _saveProfileButton.onClick.RemoveAllListeners();
        if (_cancelEditButton) _cancelEditButton.onClick.RemoveAllListeners();
        if (_changeProfilePictureButton) _changeProfilePictureButton.onClick.RemoveAllListeners();
        if (_nicknameInputField)
        {
            _nicknameInputField.onSelect.RemoveAllListeners();
            _nicknameInputField.onValueChanged.RemoveAllListeners();
            _nicknameInputField.onEndEdit.RemoveAllListeners();
        }
    }

    #endregion

    #region Public Methods

    public void SetActiveMainPage(bool isActive)
    {
        if (_mainPage == null)
        {
            Debug.LogWarning("MyPageView: _mainPage is null.");
            return;
        }
        _mainPage.SetActive(isActive);
    }

    public void SetActiveProfileEditPage(bool isActive)
    {
        if (_profileEditPage == null)
        {
            Debug.LogWarning("MyPageView: _profileEditPage is null.");
            return;
        }
        _profileEditPage.SetActive(isActive);
    }

    public void SetProfilePicture(Sprite sprite)
    {
        if (_profilePictureImage == null)
        {
            Debug.LogWarning("MyPageView: _profilePictureImage is null.");
            return;
        }
        _profilePictureImage.sprite = sprite;
        if (_profilePictureImageARF != null)
            _profilePictureImageARF.aspectRatio = sprite.rect.width / sprite.rect.height;
    }

    public async void SetProfilePicture(string imageUrl)
    {
        if (_profilePictureImage == null) return;
        SetProfilePicture(await Utils.ImageUtils.LoadSpriteFromUrlAsync(imageUrl));
    }

    public void SetUserInfo(string userInfo)
    {
        if (_userInfoText == null)
        {
            Debug.LogWarning("MyPageView: _userInfoText is null.");
            return;
        }
        _userInfoText.text = userInfo;
    }

    public string GetNicknameInput()
    {
        return _nicknameInputField != null ? _nicknameInputField.text : string.Empty;
    }

    public void SetNicknameInput(string nickname)
    {
        if (_nicknameInputField != null)
            _nicknameInputField.text = nickname;
    }

    #endregion

    #region Private Methods

    private void SetButton(Button button, UnityAction action)
    {
        if (button == null)
        {
            Debug.LogWarning("MyPageView: Button reference is missing.");
            return;
        }
        button.onClick.RemoveAllListeners();
        if (action != null)
            button.onClick.AddListener(action);
    }

    #endregion
}
