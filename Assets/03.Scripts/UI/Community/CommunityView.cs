using System.Collections.Generic;
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
}
