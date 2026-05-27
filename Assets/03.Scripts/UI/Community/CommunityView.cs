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

    [Serializable]
    class FilterCategoryButton
    {
        public Button Button;
        public Image BackgroundImage;
        public TextMeshProUGUI Text;
        public string categoryName;
    }

    [Header("Prefabs")]
    [SerializeField] private NoImagePostView _noImagePostViewPrefab;
    [SerializeField] private WithImagePostView _withImagePostViewPrefab;

    [Header("Inspect Page")]
    [SerializeField] private ReadonlyProjectInspectView _inspectView;
    [SerializeField] private GameObject _inspectPage;
    public ReadonlyProjectInspectView InspectView => _inspectView;

    [Header("Components")]
    [SerializeField] private Button _returnButton;
    [SerializeField] private GameObject _verticalLayoutParent;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private TextMeshProUGUI _refreshIndicator;
    [SerializeField] private PostView _postView;
    [SerializeField] private Button _writePostButton;
    [SerializeField] private Button _writeCommentButton;
    [SerializeField] private TMP_InputField _commentInputField;

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

    [Header("Filter")]
    [SerializeField] private GameObject _filterPanel;
    [SerializeField] private Button _showFilter;
    [SerializeField] private Button _latestButton;
    [SerializeField] private ProceduralImage _latestButtonImage;
    [SerializeField] private TextMeshProUGUI _latestButtonText;
    [SerializeField] private Button _oldestButton;
    [SerializeField] private ProceduralImage _oldestButtonImage;
    [SerializeField] private TextMeshProUGUI _oldestButtonText;
    [SerializeField] private Button _resetFilterButton;
    [SerializeField] private Button _applyFilterButton;
    [SerializeField] private TMP_InputField _nameFilterInput;
    [SerializeField] private Color _buttonActiveColor;
    [SerializeField] private FilterCategoryButton[] _categoryFilterButtons = new FilterCategoryButton[5];

    private CommunityPresenter _presenter;

    private List<NoImagePostView> _postViews = new List<NoImagePostView>();
    private string _defaultCommentPlaceholder;

    private void Awake()
    {
        _presenter = new CommunityPresenter(this, _postView);
        if (_commentInputField && _commentInputField.placeholder is TextMeshProUGUI ph)
            _defaultCommentPlaceholder = ph.text;
    }

    private void Start()
    {
        _scrollRect.onValueChanged.AddListener(_presenter.OnScrollChanged);
        _returnButton.onClick.AddListener(_presenter.OnReturnButtonClicked);
        _writePostButton.onClick.AddListener(_presenter.OnWritePostButtonClicked);
        _submitButton.onClick.AddListener(_presenter.OnSubmitNewPostButtonClicked);
        if (_writeCommentButton) _writeCommentButton.onClick.AddListener(_presenter.OnSubmitCommentClicked);
        if (_commentInputField)
        {
            _commentInputField.onSelect.AddListener(_presenter.OnCommentInputFieldFocused);
            _commentInputField.onValueChanged.AddListener(_presenter.OnCommentInputFieldValueChanged);
            _commentInputField.onEndEdit.AddListener(_presenter.OnCommentInputFieldEndEdit);
        }

        for (int i = 0; i < _categoryButtons.Length; i++)
        {
            int idx = i;
            _categoryButtons[idx].Button.onClick.AddListener(() => _presenter.OnCategorySelected(idx));
        }

        _scopePublicToggle.onValueChanged.AddListener(isOn => { if (isOn) _presenter.OnScopeSelected("PUBLIC"); });
        _scopePrivateToggle.onValueChanged.AddListener(isOn => { if (isOn) _presenter.OnScopeSelected("PRIVATE"); });

        if (_showFilter) _showFilter.onClick.AddListener(_presenter.OnShowFilterClicked);
        if (_latestButton) _latestButton.onClick.AddListener(() => _presenter.OnLatestButtonClicked(_latestButtonImage, _latestButtonText));
        if (_oldestButton) _oldestButton.onClick.AddListener(() => _presenter.OnOldestButtonClicked(_oldestButtonImage, _oldestButtonText));
        if (_resetFilterButton) _resetFilterButton.onClick.AddListener(_presenter.OnResetFilterButtonClicked);
        if (_applyFilterButton) _applyFilterButton.onClick.AddListener(_presenter.OnApplyFilterButtonClicked);

        if (_categoryFilterButtons != null)
        {
            for (int i = 0; i < _categoryFilterButtons.Length; i++)
            {
                int idx = i;
                if (_categoryFilterButtons[idx]?.Button)
                    _categoryFilterButtons[idx].Button.onClick.AddListener(() => _presenter.OnFilterCategorySelected(_categoryFilterButtons[idx].categoryName));
            }
        }

        _page3pannel.SetActive(false);

        int pendingPostId = Utils.SceneHistory.ConsumePendingPostId();
        if (pendingPostId > 0)
        {
            _presenter.LoadPage();
            _presenter.OpenDetail(pendingPostId);
        }
        else
        {
            _presenter.LoadPage();
            // 프로젝트 카드에서 공유하기로 이동한 경우 글쓰기 패널을 자동으로 띄움
            _presenter.CheckAndHandlePendingModel3dId();
        }
    }

    private void OnDestroy()
    {
        _scrollRect.onValueChanged.RemoveAllListeners();
        foreach(var postView in _postViews)
        {
            postView.GetButton().onClick.RemoveAllListeners();
        }
        if (_commentInputField)
        {
            _commentInputField.onSelect.RemoveAllListeners();
            _commentInputField.onValueChanged.RemoveAllListeners();
            _commentInputField.onEndEdit.RemoveAllListeners();
        }
    }

    public void ShowInspectPage()
    {
        if (_inspectPage) _inspectPage.SetActive(true);
    }

    public void HideInspectPage()
    {
        if (_inspectPage) _inspectPage.SetActive(false);
        if (_inspectView) _inspectView.Cleanup();
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
    {
        var button = Instantiate(_communityAddPictureButtonPrefab, _CommunityAddPictureParent);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_CommunityAddPictureParent.parent);
        return button;
    }

    public string GetPostTitle() => _titleInputField.text;
    public string GetPostContent() => _contentInputField.text;
    public string GetSelectedScope() => _scopePublicToggle.isOn ? "PUBLIC" : "PRIVATE";

    public void UpdateCategoryVisual(int selectedIndex)
    {
        for (int i = 0; i < _categoryButtons.Length; i++)
        {
            bool selected = i == selectedIndex;
            _categoryButtons[i].BackgroundImage.color = selected ? _categorySelectedBGColor : Color.white;
            _categoryButtons[i].Text.color = selected ? Color.white : Color.black;
        }
    }

    public void ResetNewPostForm()
    {
        _titleInputField.text = "";
        _contentInputField.text = "";
        _scopePublicToggle.isOn = true;
        for (int i = 0; i < _categoryButtons.Length; i++)
        {
            _categoryButtons[i].BackgroundImage.color = Color.white;
            _categoryButtons[i].Text.color = Color.black;
        }
    }

    public void SetPostForm(string title, string content)
    {
        _titleInputField.text = title;
        _contentInputField.text = content;
    }

    public string GetCommentInputText() => _commentInputField ? _commentInputField.text : string.Empty;

    public void ClearCommentInput()
    {
        if (_commentInputField) _commentInputField.text = string.Empty;
    }

    public void SetCommentInputPlaceholder(string placeholder)
    {
        if (_commentInputField && _commentInputField.placeholder is TextMeshProUGUI ph)
            ph.text = placeholder;
    }

    public void ResetCommentInputPlaceholder()
    {
        if (_commentInputField && _commentInputField.placeholder is TextMeshProUGUI ph)
            ph.text = _defaultCommentPlaceholder;
    }

    public void FocusCommentInput()
    {
        if (_commentInputField) _commentInputField.ActivateInputField();
    }

    public void RebuildPictureButtonLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_CommunityAddPictureParent.parent);
    }

    #region filter
    public void SetActiveButtonColor(ProceduralImage image, TextMeshProUGUI text)
    {
        if (!image || !text || !_latestButtonImage || !_latestButtonText || !_oldestButtonImage || !_oldestButtonText)
        {
            Debug.LogError($"[CommunityView.cs] Something was null when updating Active Button Color");
            return;
        }

        _latestButtonImage.color = Color.white;
        _latestButtonText.color = _buttonActiveColor;
        _oldestButtonImage.color = Color.white;
        _oldestButtonText.color = _buttonActiveColor;

        image.color = _buttonActiveColor;
        text.color = Color.white;
    }

    public void SetFilterPannelActive(bool isActive)
    {
        if (_filterPanel) _filterPanel.SetActive(isActive);
        else Debug.LogError($"[CommunityView.cs] Filter Panel is not assigned.", this);
    }

    public void ResetFilterCategoryButtons()
    {
        if (_categoryFilterButtons != null)
        {
            foreach (var btn in _categoryFilterButtons)
            {
                if (btn?.BackgroundImage) btn.BackgroundImage.color = Color.white;
                if (btn?.Text) btn.Text.color = _buttonActiveColor;
            }
        }
    }

    public void UpdateFilterCategoryVisual(string selectedCategoryName)
    {
        if (_categoryFilterButtons == null) return;

        for (int i = 0; i < _categoryFilterButtons.Length; i++)
        {
            if (_categoryFilterButtons[i] == null) continue;

            bool selected = _categoryFilterButtons[i].categoryName == selectedCategoryName;
            if (_categoryFilterButtons[i].BackgroundImage)
                _categoryFilterButtons[i].BackgroundImage.color = selected ? _buttonActiveColor : Color.white;
            if (_categoryFilterButtons[i].Text)
                _categoryFilterButtons[i].Text.color = selected ? Color.white : _buttonActiveColor;
        }
    }

    public string GetNameFilterText() => _nameFilterInput ? _nameFilterInput.text : string.Empty;
    #endregion
}
