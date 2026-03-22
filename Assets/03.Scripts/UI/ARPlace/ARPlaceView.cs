using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARPlaceView : MonoBehaviour
{
    [Header("Footer Buttons")]
    [SerializeField] private Button _cancleButton;
    [SerializeField] private Button _shutterButton;
    [SerializeField] private Button _showDimensionButton;
    [SerializeField] private TextMeshProUGUI _showDimensionButtonText;

    [Header("Components")]
    [SerializeField] private ARPlaceCore _placeCore;

    private ARPlacePresenter _presenter;

    private void Awake()
    {
        _presenter = new ARPlacePresenter(this, _placeCore);
    }

    private void Start()
    {
        if(_cancleButton) _cancleButton.onClick.AddListener(_presenter.OnCancleButtonClicked);
        if(_shutterButton) _shutterButton.onClick.AddListener(_presenter.OnShutterButtonClicked);
        if (_showDimensionButton) _showDimensionButton.onClick.AddListener(_presenter.OnShowDimensionClicked);
        _presenter.UpdateView();
    }

    private void OnDestroy()
    {
        if(_cancleButton) _cancleButton.onClick.RemoveAllListeners();
        if(_shutterButton) _shutterButton.onClick.RemoveAllListeners();
        if(_showDimensionButton) _showDimensionButton.onClick.RemoveAllListeners();
    }

    public void SetShowDimensionButtonText(string text)
    {
        if(_showDimensionButtonText) _showDimensionButtonText.text = text;
    }
}
