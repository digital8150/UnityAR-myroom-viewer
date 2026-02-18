using UnityEngine;
using UnityEngine.UI;

public interface IProjectStorageView
{

}

public class ProjectStorageView : MonoBehaviour, IProjectStorageView
{
    [Header("Buttons")]
    [SerializeField]
    private Button _toHomeButton;

    private ProjectStoragePresenter _presenter;

    private void Awake()
    {
        _presenter = new ProjectStoragePresenter(this);
    }

    private void Start()
    {
        _toHomeButton?.onClick.AddListener(_presenter.OnToHomeClicked);
    }

    private void OnDestroy()
    {
        _toHomeButton?.onClick.RemoveAllListeners();
    }
}
