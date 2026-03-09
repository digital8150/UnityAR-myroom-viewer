using UnityEngine;
using UnityEngine.UI;

public class ARPlaceView : MonoBehaviour
{
    [SerializeField] private Button _cancleButton;
    [SerializeField] private Button _shutterButton;

    private ARPlacePresenter _presenter;

    private void Awake()
    {
        _presenter = new ARPlacePresenter(this);
    }

    private void Start()
    {
        if(_cancleButton) _cancleButton.onClick.AddListener(_presenter.OnCancleButtonClicked);
        if(_shutterButton) _shutterButton.onClick.AddListener(_presenter.OnShutterButtonClicked);
    }

    private void OnDestroy()
    {
        if(_cancleButton) _cancleButton.onClick.RemoveListener(_presenter.OnCancleButtonClicked);
        if(_shutterButton) _shutterButton.onClick.RemoveListener(_presenter.OnShutterButtonClicked);
    }

}
