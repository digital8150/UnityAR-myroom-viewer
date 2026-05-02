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
    private Button _toProjectsBtn2;
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

    public Button Project1Button => _project1Button;
    public Button Project2Button => _project2Button;

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
    //--- Private Methods ---//
}
