using UnityEngine;
using UnityEngine.UI;

public interface IHomeView
{

}

public class HomeView : MonoBehaviour, IHomeView
{
    //--- Settings ---//
    [Header("Buttons")]
    [SerializeField]
    private Button _toGenerate3DBtn;
    [SerializeField]
    private Button _toProjectsBtn;
    [SerializeField]
    private Button _toProjectsBtn2;

    //--- Fields ---//
    private HomePresenter _presenter;

    //--- Unity Lifecycle ---//
    private void Awake()
    {
        _presenter = new HomePresenter(this);
        _toGenerate3DBtn?.onClick.AddListener(_presenter.OnToGenerate3DClicked);
        _toProjectsBtn?.onClick.AddListener(_presenter.OnToProjectsClicked);
        _toProjectsBtn2?.onClick.AddListener(_presenter.OnToProjectsClicked);
    }

    private void OnDestroy()
    {
        _toGenerate3DBtn?.onClick.RemoveAllListeners();
        _toProjectsBtn?.onClick.RemoveAllListeners();
        _toProjectsBtn2?.onClick.RemoveAllListeners();
    }
    //--- Public Methods ---//


    //--- Private Methods ---//
}
