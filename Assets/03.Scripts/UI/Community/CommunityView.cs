using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommunityView : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private NoImagePostView _noImagePostViewPrefab;
    [SerializeField] private WithImagePostView _withImagePostViewPrefab;

    [Header("Components")]
    [SerializeField] private GameObject _verticalLayoutParent;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private TextMeshProUGUI _refreshIndicator;

    private CommunityPresenter _presenter;

    private List<NoImagePostView> _postViews = new List<NoImagePostView>();

    private void Awake()
    {
        _presenter = new CommunityPresenter(this);
    }

    private void Start()
    {
        _scrollRect.onValueChanged.AddListener(_presenter.OnScrollChanged);
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
}
