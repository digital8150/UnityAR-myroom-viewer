using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
    }

    /// <summary>
    /// ModelId를 기준으로 기존 뷰를 갱신하거나, 없으면 새로 생성하여 리스트에 추가합니다.
    /// </summary>
    /// <param name="imageSprite">표시할 썸네일 이미지</param>
    /// <param name="name">프로젝트 이름</param>
    /// <param name="modelId">데이터 고유 식별자 (조회 기준)</param>
    /// <param name="status">현재 진행 상태 텍스트</param>
    public void UpdateOrAddViewItem(Sprite imageSprite, string name, int modelId, string status, UnityAction buttonHandler)
    {
        var find = _slotsViewList.Find(item => item.ModelId == modelId);

        if(!find)
        {
            var viewSlot = Instantiate(_viewSlotPrefab, _gridParent.transform);
            viewSlot.ModelId = modelId;
            viewSlot.UpdateProjectNameText(name);
            viewSlot.UpdateThumbnailImage(imageSprite);
            viewSlot.UpdateStatusText(status);
            viewSlot.GetButton()?.onClick.AddListener(buttonHandler);
            _slotsViewList.Add(viewSlot);
            return;
        }

        find.UpdateProjectNameText(name);
        find.UpdateThumbnailImage(imageSprite);
        find.UpdateStatusText(status);
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

    private void OnScrollChanged(Vector2 pos)
    {
        if (pos.y <= 0.1f)
        {
            _presenter.LoadPage();
        }
    }
}
