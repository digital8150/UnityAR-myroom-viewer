using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

public class ProjectsView : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField]
    private ViewSlotsView _viewSlotPrefab;


    [Header("Components")]
    [SerializeField]
    private GameObject _gridParent;
    [SerializeField]
    private ScrollRect _scrollRect;

    [Header("Buttons")]
    [SerializeField]
    private Button _toGenerate3DButton;

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

    private List<ViewSlotsView> _slotsViewList;
    private ProjectsPresenter _presenter;

    #region Unity Lifecycle
    private void Awake()
    {
        _slotsViewList = new List<ViewSlotsView>();
        _presenter = new ProjectsPresenter(this);

        if(_showFilter) _showFilter.onClick.AddListener(_presenter.OnShowFilterClicked);
        if(_latestButton) _latestButton.onClick.AddListener(() => _presenter.OnLatestButtonClicked(_latestButtonImage, _latestButtonText));
        if(_oldestButton) _oldestButton.onClick.AddListener(() => _presenter.OnOldestButtonClicked(_oldestButtonImage, _oldestButtonText));
        if(_resetFilterButton) _resetFilterButton.onClick.AddListener(_presenter.OnResetFilterButtonClicked);
        if(_applyFilterButton) _applyFilterButton.onClick.AddListener(_presenter.OnApplyFilterButtonClicked);
    }

    private void Start()
    {
        _toGenerate3DButton?.onClick.AddListener(_presenter.ToGenerate3DClicked);
        _presenter.StartUp();
    }

    private void OnEnable()
    {
        _scrollRect.onValueChanged.AddListener(OnScrollChanged);
    }

    private void OnDisable()
    {
        _scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
    }

    private void OnDestroy()
    {
        _toGenerate3DButton?.onClick.RemoveAllListeners();
        foreach(var viewSlot in _slotsViewList)
        {
            viewSlot?.GetButton()?.onClick.RemoveAllListeners();
        }
        if (_presenter != null)
        {
            _presenter.Dispose();
        }

        if (_showFilter) _showFilter.onClick.RemoveAllListeners();
        if (_latestButton) _latestButton.onClick.RemoveAllListeners();
        if (_oldestButton) _oldestButton.onClick.RemoveAllListeners();
        if (_resetFilterButton) _resetFilterButton.onClick.RemoveAllListeners();
        if (_applyFilterButton) _applyFilterButton.onClick.RemoveAllListeners();
    }
    #endregion

    public string GetNameFilterInput()
    {
        if(_nameFilterInput == null)
        {
            Debug.LogError($"[ProjectsView.cs] Name Filter Input is not assigned.", this);
            return string.Empty;
        }
        return _nameFilterInput.text;
    }

    public void SetNameFilterInput(string text)
    {
        if(_nameFilterInput == null)
        {
            Debug.LogError($"[ProjectsView.cs] Name Filter Input is not assigned.", this);
            return;
        }
        _nameFilterInput.text = text;
    }

    public void SetActiveButtonColor(ProceduralImage image, TextMeshProUGUI text)
    {
        if (!image || !text || !_latestButtonImage || !_latestButtonText || !_oldestButtonImage || !_oldestButtonText)
        {
            Debug.LogError($"[ProjectView.cs] Something was null when updating Active Button Color");
            return;
        }

        // Reset all buttons to default color
        _latestButtonImage.color = Color.white;
        _latestButtonText.color = Color.black;
        _oldestButtonImage.color = Color.white;
        _oldestButtonText.color = Color.black;
        
        // Set the active button color
        image.color = _buttonActiveColor;
        text.color = Color.white;
    }

    public void SetFilterPannelActive(bool isActive)
    {
        if(_filterPanel) _filterPanel.SetActive(isActive);
        else Debug.LogError($"[ProjectsView.cs] Filter Panel is not assigned.", this);
    }

    /// <summary>
    /// ModelId를 기준으로 기존 뷰를 갱신하거나, 없으면 새로 생성하여 리스트에 추가합니다.
    /// </summary>
    /// <param name="imageSprite">표시할 썸네일 이미지</param>
    /// <param name="name">프로젝트 이름</param>
    /// <param name="modelId">데이터 고유 식별자 (조회 기준)</param>
    /// <param name="status">현재 진행 상태 텍스트</param>
    public ViewSlotsView UpdateOrAddViewItem(Sprite imageSprite, string name, int modelId, string status, UnityAction buttonHandler)
    {
        var find = _slotsViewList.Find(item => item.ModelId == modelId);

        if(!find)
        {
            var viewSlot = Instantiate(_viewSlotPrefab, _gridParent.transform);
            viewSlot.ModelId = modelId;
            viewSlot.UpdateProjectNameText(name);
            if(imageSprite) viewSlot.UpdateThumbnailImage(imageSprite);
            viewSlot.UpdateStatusText(status);
            viewSlot.GetButton()?.onClick.AddListener(buttonHandler);
            _slotsViewList.Add(viewSlot);
            return viewSlot;
        }

        find.UpdateProjectNameText(name);
        if(imageSprite) find.UpdateThumbnailImage(imageSprite);
        find.UpdateStatusText(status);
        return find;
    }

    public bool UpdateViewSlotStatusWithID(int id, string status)
    {
        var find = _slotsViewList.Find(item => item.ModelId == id);
        if(!find)
        {
            find.UpdateStatusText(status);
            return true;
        }
        Debug.LogError($"[ProjectsView.cs] Cannot find view slot with id : {id}", this);
        return false;
    }

    public void ClearViewItems()
    {
        foreach(var viewSlot in _slotsViewList)
        {
            if(viewSlot) Destroy(viewSlot.gameObject);
        }
        _slotsViewList.Clear();
    }

    private void OnScrollChanged(Vector2 pos)
    {
        if (pos.y <= 0.1f)
        {
            _presenter.LoadPage();
        }
    }
}
