using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IProjectsView
{
    void AddNewViewSlot(Sprite imageSprite, string name);
}

public class ProjectsView : MonoBehaviour, IProjectsView
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

    private List<ViewSlotsView> _slotsViewList;
    private ProjectsPresenter _presenter;

    private void Awake()
    {
        _slotsViewList = new List<ViewSlotsView>();
        _presenter = new ProjectsPresenter(this);
    }

    private void Start()
    {
        _toGenerate3DButton?.onClick.AddListener(_presenter.ToGenerate3DClicked);
        _presenter.LoadPage();
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
    }

    public void AddNewViewSlot(Sprite imageSprite, string name)
    {
        var viewSlot = Instantiate(_viewSlotPrefab, _gridParent.transform);
        viewSlot.UpdateProjectNameText(name);
        viewSlot.UpdateThumbnailImage(imageSprite);
        _slotsViewList.Add(viewSlot);
    }

    private void OnScrollChanged(Vector2 pos)
    {
        if (pos.y <= 0.1f)
        {
            _presenter.LoadPage();
        }
    }
}
