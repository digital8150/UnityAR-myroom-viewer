using UnityEngine;
using UnityEngine.UI;

public interface IGenerate3DView
{
    void ShowLandingPage();
    void ShowConvertingPage();
}

public class Generate3DView : MonoBehaviour, IGenerate3DView
{
    //--- Settings ---//
    [Header("Buttons")]
    [SerializeField]
    private Button _returnBtn;
    [SerializeField]
    private Button _loadImageBtn, _takePictureBtn;

    [Header("Pages")]
    [SerializeField]
    private GameObject _page1LandingPage;
    [SerializeField]
    private GameObject _page2Converting;

    //--- Fields ---//
    private Generate3DPresenter _presenter;

    //--- Unity Methods ---//
    private void Awake()
    {
        InitializeView();
    }

    private void Start()
    {
        ShowLandingPage();
    }

    private void OnDestroy()
    {
        DisposeListeners();
    }

    //--- Public Methods ---//
    public void ShowLandingPage()
    {
        HideAllPage();
        _page1LandingPage.SetActive(true);
    }

    public void ShowConvertingPage()
    {
        HideAllPage();
        _page2Converting.SetActive(true);
    }

    //--- Private Methods ---//
    private void InitializeView()
    {
        _presenter = new Generate3DPresenter(this);
        _returnBtn?.onClick.AddListener(_presenter.OnReturnClicked);
        _loadImageBtn?.onClick.AddListener(_presenter.OnLoadImageClicked);
        _takePictureBtn?.onClick.AddListener(_presenter.OnTakePictureClicked);
    }

    private void DisposeListeners()
    {
        _returnBtn?.onClick.RemoveAllListeners();
        _loadImageBtn?.onClick.RemoveAllListeners();
        _takePictureBtn?.onClick.RemoveAllListeners();
    }

    private void HideAllPage()
    {
        _page1LandingPage.SetActive(false);
        _page2Converting.SetActive(false);
    }
}
