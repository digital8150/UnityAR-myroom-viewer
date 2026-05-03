using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GalleryView : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GalleryListItemView _listItemPrefab;

    [Header("Components")]
    [SerializeField] private GameObject _gridParent;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private ReadonlyProjectInspectView _inspectView;
    public ReadonlyProjectInspectView InspectView => _inspectView;

    [Header("Pages")]
    [SerializeField] private GameObject _galleryPage;
    [SerializeField] private GameObject _inspectPage;

    [Header("Buttons")]
    [SerializeField] private Button _backButton;

    private List<GalleryListItemView> _listItemViews;
    private GalleryPresenter _presenter;

    private void Awake()
    {
        _listItemViews = new List<GalleryListItemView>();
        _presenter = new GalleryPresenter(this);
    }

    private void Start()
    {
        _backButton?.onClick.AddListener(_presenter.OnBackClicked);
        _presenter.Initialize();
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
        _backButton?.onClick.RemoveAllListeners();
        foreach (var item in _listItemViews)
        {
            if (item)
            {
                Destroy(item.gameObject);
            }
        }
        if (_presenter != null)
        {
            _presenter.Dispose();
        }
    }

    /// <summary>
    /// 갤러리 아이템을 추가합니다.
    /// </summary>
    public GalleryListItemView AddGalleryItem(ModelData modelData)
    {
        var clone = Instantiate(_listItemPrefab, _gridParent.transform);

        clone.SetTitleText(modelData.name);
        clone.SetInfoTextWithDTO(modelData);
        clone.SetThumbnail(modelData.thumbnailUrl);

        if (!string.IsNullOrEmpty(modelData.furniture_type))
        {
            clone.SetBadgeText(TranslateCategory(modelData.furniture_type));
        }

        clone.SetGoToInspectAction(() => _presenter.OnItemInspectClicked(modelData.id));
        clone.SetGoToARAction(() => _presenter.OnItemARClicked(modelData.id));

        _listItemViews.Add(clone);
        return clone;
    }

    /// <summary>
    /// 모든 갤러리 아이템을 제거합니다.
    /// </summary>
    public void ClearGalleryItems()
    {
        foreach (var item in _listItemViews)
        {
            if (item) Destroy(item.gameObject);
        }
        _listItemViews.Clear();
    }

    /// <summary>
    /// 현재 로드된 갤러리 아이템의 개수를 반환합니다.
    /// </summary>
    public int GetListItemCount()
    {
        return _listItemViews.Count;
    }

    /// <summary>
    /// 갤러리 목록 페이지를 표시합니다.
    /// </summary>
    public void ShowGalleryPage()
    {
        if (_galleryPage) _galleryPage.SetActive(true);
        if (_inspectPage) _inspectPage.SetActive(false);
    }

    /// <summary>
    /// 상세 보기 페이지를 표시합니다.
    /// </summary>
    public void ShowInspectPage()
    {
        if (_galleryPage) _galleryPage.SetActive(false);
        if (_inspectPage) _inspectPage.SetActive(true);
    }

    private void OnScrollChanged(Vector2 pos)
    {
        if (pos.y <= 0.1f)
        {
            _presenter.LoadPage();
        }
    }

    /// <summary>
    /// 가구 카테고리 enum을 한글로 번역합니다.
    /// </summary>
    private string TranslateCategory(string category)
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
