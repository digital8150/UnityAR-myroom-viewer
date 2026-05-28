using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

public class ARPlaceFurnitureListView : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject _panelRoot;

    [Header("List")]
    [SerializeField] private GalleryListItemView _listItemPrefab;
    [SerializeField] private RectTransform _gridParent;
    [SerializeField] private ScrollRect _scrollRect;

    [Header("Tabs")]
    [SerializeField] private Button _myTabButton;
    [SerializeField] private ProceduralImage _myTabImage;
    [SerializeField] private TextMeshProUGUI _myTabText;
    [SerializeField] private Button _sharedTabButton;
    [SerializeField] private ProceduralImage _sharedTabImage;
    [SerializeField] private TextMeshProUGUI _sharedTabText;
    [SerializeField] private Color _tabActiveColor = new Color(0.13f, 0.55f, 0.96f, 1f);

    [Header("Controls")]
    [SerializeField] private Button _closeButton;

    private readonly List<GalleryListItemView> _items = new List<GalleryListItemView>();
    private ARPlaceFurnitureListPresenter _presenter;

    public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

    private void Awake()
    {
        _presenter = new ARPlaceFurnitureListPresenter(this);
    }

    private void Start()
    {
        if (_myTabButton) _myTabButton.onClick.AddListener(_presenter.OnMyTabClicked);
        if (_sharedTabButton) _sharedTabButton.onClick.AddListener(_presenter.OnSharedTabClicked);
        if (_closeButton) _closeButton.onClick.AddListener(Close);

        if (_scrollRect) _scrollRect.onValueChanged.AddListener(OnScrollChanged);

        SetTabVisualActive(true);
        if (_panelRoot) _panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_myTabButton) _myTabButton.onClick.RemoveAllListeners();
        if (_sharedTabButton) _sharedTabButton.onClick.RemoveAllListeners();
        if (_closeButton) _closeButton.onClick.RemoveAllListeners();
        if (_scrollRect) _scrollRect.onValueChanged.RemoveAllListeners();

        ClearItems();
    }

    public void Bind(ARPlaceCore core)
    {
        _presenter.Bind(core);
    }

    public void Open()
    {
        if (_panelRoot != null) _panelRoot.SetActive(true);
        _presenter.OnOpened();
    }

    public void Close()
    {
        if (_panelRoot != null) _panelRoot.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen) Close(); else Open();
    }

    public GalleryListItemView AddItem(ModelData data)
    {
        var clone = Instantiate(_listItemPrefab, _gridParent);
        clone.SetTitleText(data.name);
        clone.SetInfoTextWithDTO(data);
        clone.SetThumbnail(data.thumbnailUrl);

        if (!string.IsNullOrEmpty(data.furniture_type))
        {
            clone.SetBadgeText(TranslateCategory(data.furniture_type));
        }

        int capturedId = data.id;
        clone.SetGoToARAction(() => _presenter.OnItemSelected(capturedId));

        _items.Add(clone);
        return clone;
    }

    public void ClearItems()
    {
        foreach (var item in _items)
        {
            if (item) Destroy(item.gameObject);
        }
        _items.Clear();
    }

    public void SetTabVisualActive(bool myActive)
    {
        if (_myTabImage) _myTabImage.color = myActive ? _tabActiveColor : Color.white;
        if (_myTabText) _myTabText.color = myActive ? Color.white : Color.black;
        if (_sharedTabImage) _sharedTabImage.color = myActive ? Color.white : _tabActiveColor;
        if (_sharedTabText) _sharedTabText.color = myActive ? Color.black : Color.white;
    }

    private void OnScrollChanged(Vector2 pos)
    {
        if (pos.y <= 0.1f)
        {
            _presenter.LoadPage();
        }
    }

    private static string TranslateCategory(string category)
    {
        return category?.ToLower() switch
        {
            "shelf" => "선반",
            "sofa" => "소파",
            "storage" => "수납장",
            "chair" => "의자",
            "lighting" => "조명",
            "desk" => "책상",
            "bed" => "침대",
            "table" => "테이블",
            "others" => "카테고리 미지정",
            _ => category
        };
    }
}
