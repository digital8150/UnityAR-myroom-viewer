using UnityEngine;
using UnityEngine.UI;

public interface IGenerate3DView
{
}

public class Generate3DView : MonoBehaviour, IGenerate3DView
{
    //--- Settings ---//
    [Header("Buttons")]
    [SerializeField]
    private Button _returnBtn, _loadImageBtn, _takePictureBtn;

    //--- Fields ---//
    private Generate3DPresenter _presenter;

    //--- Unity Methods ---//
    private void Awake()
    {
        InitializeView();
    }

    private void OnDestroy()
    {
        DisposeListeners();
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
}
