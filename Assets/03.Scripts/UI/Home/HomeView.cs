using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;



public class HomeView : MonoBehaviour
{
    //--- Settings ---//
    [Header("Buttons")]
    [SerializeField]
    private Button _toGenerate3DBtn;
    [SerializeField]
    private Button _toGenerate3DBtn2;
    [SerializeField]
    private Button _toProjectsBtn;

    [SerializeField]
    private Button _toCommunityBtn;
    [SerializeField]
    private Button _toAIRecommendBtn;
    [SerializeField]
    private Button _toProjectGallery;
    [SerializeField]
    private Button _toRoomPlan3D;

    [Header("ProjectGallery")]
    [SerializeField] private Image _projectGallery1Thumbnail;
    [SerializeField] private TextMeshProUGUI _projectGallery1Text;
    [SerializeField] private Image _projectGallery2Thumbnail;
    [SerializeField] private TextMeshProUGUI _projectGallery2Text;
    [SerializeField] private Button _projectGallery1Button;
    [SerializeField] private Button _projectGallery2Button;

    [Header("Recent Projects")]
    [SerializeField] private Image _project1Thumbnail;
    [SerializeField] private TextMeshProUGUI _project1Text;
    [SerializeField] private Image _project2Thumbnail;
    [SerializeField] private TextMeshProUGUI _project2Text;
    [SerializeField] private Button _project1Button;
    [SerializeField] private Button _project2Button;

    [Header("Side Bar")]
    [SerializeField] private Image _profilePictureImage;
    [SerializeField] private AspectRatioFitter _profilePictureImageARF;
    [SerializeField] private TextMeshProUGUI _userNameText;
    [SerializeField] private Button _openSidebarBtn;
    [SerializeField] private Button _sidebarGoToMyPage;
    [SerializeField] private Button _sidebarGoToAIRecommend;
    [SerializeField] private Button _sidebarGoToGenerate3D;
    [SerializeField] private Button _sidebarGoToARPlace;
    [SerializeField] private Button _sidebarGoToGallery;
    [SerializeField] private Button _sidebarGoToMyProjects;
    [SerializeField] private Button _sidebarGoToCommunity;
    [SerializeField] private Button _sidebarGoToRoomPlan3D;
    [SerializeField] private Button _closeSidebarBtn;
    [Space(10)]
    [SerializeField] private GameObject _sidebarPannel;
    [SerializeField] private Image _sidebarBlocker;
    [SerializeField] private Transform _sidebarTransform;
    [Space(10)]
    [SerializeField] private float _sidebarAnimationDuration = 0.3f;
    [SerializeField] private Color _sidebarBlockerColor;

    [Header("Navigation Bar")]
    [SerializeField] private Button _homeButton;
    [SerializeField] private Button _scanButton;
    [SerializeField] private Button _myPageButton;
    [SerializeField] private Button _toProjectsBtn2;
    public Button Project1Button => _project1Button;
    public Button Project2Button => _project2Button;
    public Button ProjectGallery1Button => _projectGallery1Button;
    public Button ProjectGallery2Button => _projectGallery2Button;

    //--- Fields ---//
    private HomePresenter _presenter;

    //--- Unity Lifecycle ---//
    private void Awake()
    {
        _presenter = new HomePresenter(this);
        if(_toGenerate3DBtn) _toGenerate3DBtn.onClick.AddListener(_presenter.OnToGenerate3DClicked);
        if(_toGenerate3DBtn2) _toGenerate3DBtn2.onClick.AddListener(_presenter.OnToGenerate3DClicked);
        if(_toProjectsBtn) _toProjectsBtn.onClick.AddListener(_presenter.OnToProjectsClicked);
        if(_toProjectsBtn2) _toProjectsBtn2.onClick.AddListener(_presenter.OnToProjectsClicked);
        if(_toCommunityBtn) _toCommunityBtn.onClick.AddListener(_presenter.OnToCommunityClicked);
        if(_toAIRecommendBtn) _toAIRecommendBtn.onClick.AddListener(_presenter.OnToAIRecommendClicked);
        if(_openSidebarBtn) _openSidebarBtn.onClick.AddListener(OpenSidebar);
        if(_closeSidebarBtn) _closeSidebarBtn.onClick.AddListener(() => CloseSidebar());
        if(_toProjectGallery) _toProjectGallery.onClick.AddListener(_presenter.OnGoToGalleryClicked);
        if(_myPageButton) _myPageButton.onClick.AddListener(_presenter.OnToMyPageClicked);

        // Sidebar GoTo Buttons
        if (_sidebarGoToAIRecommend) _sidebarGoToAIRecommend.onClick.AddListener(_presenter.OnSidebarGoToAIRecommendClicked);
        if (_sidebarGoToMyPage) _sidebarGoToMyPage.onClick.AddListener(_presenter.OnToMyPageClicked);
        if(_sidebarGoToGenerate3D) _sidebarGoToGenerate3D.onClick.AddListener(_presenter.OnSidebarGoToGenerate3DClicked);
        if(_sidebarGoToARPlace) _sidebarGoToARPlace.onClick.AddListener(_presenter.OnSidebarGoToARPlaceClicked);
        if(_sidebarGoToGallery) _sidebarGoToGallery.onClick.AddListener(_presenter.OnGoToGalleryClicked);
        if(_sidebarGoToMyProjects) _sidebarGoToMyProjects.onClick.AddListener(_presenter.OnSidebarGoToMyProjectsClicked);
        if(_sidebarGoToCommunity) _sidebarGoToCommunity.onClick.AddListener(_presenter.OnSidebarGoToCommunityClicked);
        if(_sidebarGoToRoomPlan3D) _sidebarGoToRoomPlan3D.onClick.AddListener(_presenter.OnSidebarGoToRoomPlan3DClicked);

        if(_scanButton) _scanButton.onClick.AddListener(_presenter.OnToGenerate3DClicked);
        if(_toRoomPlan3D) _toRoomPlan3D.onClick.AddListener(_presenter.OnSidebarGoToRoomPlan3DClicked);

        _presenter.InitializeView();
    }

    private void OnDestroy()
    {
        if(_toGenerate3DBtn) _toGenerate3DBtn.onClick.RemoveAllListeners();
        if(_toGenerate3DBtn2) _toGenerate3DBtn2.onClick.RemoveAllListeners();
        if (_toProjectsBtn) _toProjectsBtn.onClick.RemoveAllListeners();
        if(_toProjectsBtn2) _toProjectsBtn2.onClick.RemoveAllListeners();
        if(_toCommunityBtn) _toCommunityBtn.onClick.RemoveAllListeners();
        if(_project1Button) _project1Button.onClick.RemoveAllListeners();
        if(_project2Button) _project2Button.onClick.RemoveAllListeners();
    }

    //--- Public Methods ---//
    #region Recent Projects
    public void SetProject1Thumbnail(Sprite thumbnail)
    {
        if (_project1Thumbnail)
        {
            _project1Thumbnail.sprite = thumbnail;
            _project1Thumbnail.GetComponent<AspectRatioFitter>().aspectRatio = thumbnail.rect.width / thumbnail.rect.height;
        }
    }

    public async void SetProject1Thumbnail(string imageUrl)
    {
        if (_project1Thumbnail) SetProject1Thumbnail(await Utils.ImageUtils.LoadSpriteFromUrlAsync(imageUrl));
    }

    public void SetProject2Thumbnail(Sprite thumbnail)
    {
        if(_project2Thumbnail)
        { 
            _project2Thumbnail.sprite = thumbnail;
            _project2Thumbnail.GetComponent<AspectRatioFitter>().aspectRatio = thumbnail.rect.width / thumbnail.rect.height;
        }
    }

    public async void SetProject2Thumbnail(string imageUrl)
    {
        if (_project2Thumbnail) SetProject2Thumbnail(await Utils.ImageUtils.LoadSpriteFromUrlAsync(imageUrl));
    }

    public void SetProject1Text(string text)
    {
        if(_project1Text) _project1Text.text = text;
    }

    public void SetProject2Text(string text)
    {
        if(_project2Text) _project2Text.text = text;
    }
    #endregion
    #region Project Gallery
    public void SetProjectGallery1Thumbnail(Sprite thumbnail)
    {
        if (_projectGallery1Thumbnail)
        {
            _projectGallery1Thumbnail.sprite = thumbnail;
            _projectGallery1Thumbnail.GetComponent<AspectRatioFitter>().aspectRatio = thumbnail.rect.width / thumbnail.rect.height;
        }
    }

    public async void SetProjectGallery1Thumbnail(string imageUrl)
    {
        if (_projectGallery1Thumbnail) SetProjectGallery1Thumbnail(await Utils.ImageUtils.LoadSpriteFromUrlAsync(imageUrl));
    }

    public void SetProjectGallery2Thumbnail(Sprite thumbnail)
    {
        if (_projectGallery2Thumbnail)
        {
            _projectGallery2Thumbnail.sprite = thumbnail;
            _projectGallery2Thumbnail.GetComponent<AspectRatioFitter>().aspectRatio = thumbnail.rect.width / thumbnail.rect.height;
        }
    }

    public async void SetProjectGallery2Thumbnail(string imageUrl)
    {
        if (_projectGallery2Thumbnail) SetProjectGallery2Thumbnail(await Utils.ImageUtils.LoadSpriteFromUrlAsync(imageUrl));
    }

    public void SetProjectGallery1Text(string text)
    {
        if (_projectGallery1Text) _projectGallery1Text.text = text;
    }

    public void SetProjectGallery2Text(string text)
    {
        if (_projectGallery2Text) _projectGallery2Text.text = text;
    }

    #endregion
    #region SideBar
    public void OpenSidebar()
    {
        if (!_sidebarPannel || !_sidebarBlocker) return;

        _sidebarBlocker.color = new Color(0, 0, 0, 0);
        _sidebarBlocker.gameObject.SetActive(true);
        _sidebarPannel.SetActive(true);

        StartCoroutine(AniamteSidebar(true));
        StartCoroutine(AnimateSidebarBlocker(true));
    }

    public void CloseSidebar(bool instant = false)
    {
        if (!_sidebarPannel || !_sidebarBlocker) return;

        if (instant)
        {
            _sidebarPannel.SetActive(false);
            _sidebarBlocker.gameObject.SetActive(false);
        }
        else
        {
            StartCoroutine(AniamteSidebar(false));
            StartCoroutine(AnimateSidebarBlocker(false));
        }
    }

    public void SetSideBarUserName(string userName)
    {
        if(_userNameText) _userNameText.text = userName;
    }

    public void SetSideBarProfilePicture(Sprite sprite)
    {
        if(_profilePictureImage) _profilePictureImage.sprite = sprite;
        if(_profilePictureImageARF) _profilePictureImageARF.aspectRatio = sprite.rect.width / sprite.rect.height;   
    }

    private IEnumerator AniamteSidebar(bool open)
    {
        float elapsedTime = 0f;
        Vector3 startScale = open ? new Vector3(0, 1, 1) : new Vector3(1,1,1);
        Vector3 endScale = open ? new Vector3(1,1,1) : new Vector3(0,1,1);
        while (elapsedTime < _sidebarAnimationDuration)
        {
            float t = elapsedTime / _sidebarAnimationDuration;
            _sidebarTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        _sidebarTransform.localScale = endScale;
        if(!open)
        {
            _sidebarPannel.SetActive(false);
            _sidebarBlocker.gameObject.SetActive(false);
        }
    }

    private IEnumerator AnimateSidebarBlocker(bool open)
    {
        float elapsedTime = 0f;
        Color startColor = open ? new Color(0, 0, 0, 0) : _sidebarBlockerColor;
        Color endColor = open ? _sidebarBlockerColor : new Color(0, 0, 0, 0);
        while (elapsedTime < _sidebarAnimationDuration)
        {
            float t = elapsedTime / _sidebarAnimationDuration;
            _sidebarBlocker.color = Color.Lerp(startColor, endColor, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        _sidebarBlocker.color = endColor;
    }
    #endregion
    //--- Private Methods ---//
}
