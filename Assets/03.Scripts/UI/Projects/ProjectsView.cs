using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IProjectsView
{
    void AddNewViewSlot(Sprite imageSprite, string name, int modelId, string status);
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

    public void AddNewViewSlot(Sprite imageSprite, string name, int modelId, string status)
    {
        var viewSlot = Instantiate(_viewSlotPrefab, _gridParent.transform);
        viewSlot.ModelId = modelId;
        viewSlot.UpdateProjectNameText(name);
        viewSlot.UpdateThumbnailImage(imageSprite);
        viewSlot.UpdateStatusText(status);
        _slotsViewList.Add(viewSlot);
    }

    public void UpdateViewSlotStatusWithID(int id, string status)
    {
        var find = _slotsViewList.Find(item => item.ModelId == id);
        if(!find)
        {
            find.UpdateStatusText(status);
            return;
        }
        Debug.LogError($"[ProjectsView.cs] Cannot find view slot with id : {id}", this);
    }

    private void OnScrollChanged(Vector2 pos)
    {
        if (pos.y <= 0.1f)
        {
            _presenter.LoadPage();
        }
    }
}
