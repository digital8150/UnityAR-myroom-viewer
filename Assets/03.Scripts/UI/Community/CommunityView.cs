using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;


public class CommunityView : MonoBehaviour
{
    [Serializable]
    class CommunityCategoryButton
    {
        public Button Button;
        public TextMeshProUGUI Text;
        public ProceduralImage BackgroundImage;
        public string categoryName;
    }

    [Header("Prefabs")]
    [SerializeField] private NoImagePostView _noImagePostViewPrefab;
    [SerializeField] private WithImagePostView _withImagePostViewPrefab;

    [Header("Components")]
    [SerializeField] private Button _returnButton;
    [SerializeField] private GameObject _verticalLayoutParent;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private TextMeshProUGUI _refreshIndicator;
    [SerializeField] private PostView _postView;
    [SerializeField] private Button _writePostButton;

    [Header("Page3 : New Post")]
    [SerializeField] private GameObject _page3pannel;
    [SerializeField] private Transform _CommunityAddPictureParent;
    [SerializeField] private CommunityAddPictureButton _communityAddPictureButtonPrefab;
    [SerializeField] private TMP_InputField _titleInputField;
    [SerializeField] private TMP_InputField _contentInputField;
    [SerializeField] private CommunityCategoryButton[] _categoryButtons = new CommunityCategoryButton[5];
    [SerializeField] private Color _categorySelectedBGColor;
    [SerializeField] private Toggle _scopePublicToggle;
    [SerializeField] private Toggle _scopePrivateToggle;
    [SerializeField] private Button _submitButton;

    private CommunityPresenter _presenter;
    private Color _categoryUnselectedBGColor;

    private List<NoImagePostView> _postViews = new List<NoImagePostView>();

    private void Awake()
    {
        _presenter = new CommunityPresenter(this, _postView);
        if (_categoryButtons.Length > 0)
            _categoryUnselectedBGColor = _categoryButtons[0].BackgroundImage.color;
    }

    private void Start()
    {
        _scrollRect.onValueChanged.AddListener(_presenter.OnScrollChanged);
        _returnButton.onClick.AddListener(_presenter.OnReturnButtonClicked);
        _writePostButton.onClick.AddListener(_presenter.OnWritePostButtonClicked);
        _submitButton.onClick.AddListener(_presenter.OnSubmitNewPostButtonClicked);

        for (int i = 0; i < _categoryButtons.Length; i++)
        {
            int idx = i;
            _categoryButtons[idx].Button.onClick.AddListener(() => _presenter.OnCategorySelected(idx));
        }

        _scopePublicToggle.onValueChanged.AddListener(isOn => { if (isOn) _presenter.OnScopeSelected("PUBLIC"); });
        _scopePrivateToggle.onValueChanged.AddListener(isOn => { if (isOn) _presenter.OnScopeSelected("PRIVATE"); });

        _page3pannel.SetActive(false);
        _presenter.LoadPage();
    }

    private void OnDestroy()
    {
        _scrollRect.onValueChanged.RemoveAllListeners();
        foreach(var postView in _postViews)
        {
            postView.GetButton().onClick.RemoveAllListeners();
        }
    }

    public void SetRefreshIndicatorAlpha(float alpha)
    {
        _refreshIndicator.color = new Color(_refreshIndicator.color.r, _refreshIndicator.color.g, _refreshIndicator.color.b, alpha);
    }


    public float GetContentAnchoredY()
    {
        return _scrollRect.content.anchoredPosition.y;
    }

    public NoImagePostView CreateNoImagePostView()
    {
        NoImagePostView postView = Instantiate(_noImagePostViewPrefab, _verticalLayoutParent.transform);
        _postViews.Add(postView);
        return postView;
    }

    public WithImagePostView CreateWithImagePostView()
    {
        WithImagePostView postView = Instantiate(_withImagePostViewPrefab, _verticalLayoutParent.transform);
        _postViews.Add(postView);
        return postView;
    }

    public void ClearPosts()
    {
        for(int i = _postViews.Count - 1; i >= 0; i--)
        {
            if (_postViews[i] != null)
            {
                Destroy(_postViews[i].gameObject);
                _postViews.RemoveAt(i);
            }
        }
    }

    public void ShowNewPostPanel() => _page3pannel.SetActive(true);
    public void HideNewPostPanel() => _page3pannel.SetActive(false);

    public CommunityAddPictureButton SpawnAddPictureButton()
        => Instantiate(_communityAddPictureButtonPrefab, _CommunityAddPictureParent);

    public string GetPostTitle() => _titleInputField.text;
    public string GetPostContent() => _contentInputField.text;
    public string GetSelectedScope() => _scopePublicToggle.isOn ? "PUBLIC" : "PRIVATE";

    public void UpdateCategoryVisual(int selectedIndex)
    {
        for (int i = 0; i < _categoryButtons.Length; i++)
            _categoryButtons[i].BackgroundImage.color = i == selectedIndex ? _categorySelectedBGColor : _categoryUnselectedBGColor;
    }

    public void ResetNewPostForm()
    {
        _titleInputField.text = "";
        _contentInputField.text = "";
        _scopePublicToggle.isOn = true;
        for (int i = 0; i < _categoryButtons.Length; i++)
            _categoryButtons[i].BackgroundImage.color = _categoryUnselectedBGColor;
    }
}
