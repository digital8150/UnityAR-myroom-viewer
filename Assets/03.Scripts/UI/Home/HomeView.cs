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

    //--- Fields ---//
    private HomePresenter _presenter;

    //--- Unity Lifecycle ---//
    private void Awake()
    {
        _presenter = new HomePresenter(this);
        toGenerate3DBtn?.onClick.AddListener(_presenter.OnToGenerate3DClicked);
    }

    private void OnDestroy()
    {
        toGenerate3DBtn?.onClick.RemoveListener(_presenter.OnToGenerate3DClicked);
    }
    //--- Public Methods ---//


    //--- Private Methods ---//
}
