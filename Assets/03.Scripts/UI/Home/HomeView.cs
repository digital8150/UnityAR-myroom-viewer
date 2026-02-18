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
    private Button toGenerate3DBtn;
    [SerializeField]
    private Button toProjectsBtn;

    //--- Fields ---//
    private HomePresenter _presenter;

    //--- Unity Lifecycle ---//
    private void Awake()
    {
        _presenter = new HomePresenter(this);
        toGenerate3DBtn?.onClick.AddListener(_presenter.OnToGenerate3DClicked);
        toProjectsBtn?.onClick.AddListener(_presenter.OnToProjectsClicked);
    }

    private void OnDestroy()
    {
        toGenerate3DBtn?.onClick.RemoveAllListeners();
        toProjectsBtn?.onClick.RemoveAllListeners();
    }
    //--- Public Methods ---//


    //--- Private Methods ---//
}
