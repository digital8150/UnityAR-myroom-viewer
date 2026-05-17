using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ARPlaceView : MonoBehaviour
{
    [Header("Footer Buttons")]
    [SerializeField] private Button _cancleButton;
    [SerializeField] private Button _shutterButton;
    [SerializeField] private Button _showDimensionButton;
    [SerializeField] private TextMeshProUGUI _showDimensionButtonText;

    [Header("Capture")]
    [SerializeField] private Camera _arCamera;

    [Header("Capture Feedback")]
    [SerializeField] private CanvasGroup _flashOverlay;
    [SerializeField] private float _flashHoldDuration = 0.08f;
    [SerializeField] private float _flashFadeDuration = 0.35f;

    [Header("Components")]
    [SerializeField] private ARPlaceCore _placeCore;

    private ARPlacePresenter _presenter;
    private Coroutine _flashCoroutine;

    private void Awake()
    {
        _presenter = new ARPlacePresenter(this, _placeCore);
        if (_flashOverlay) _flashOverlay.alpha = 0f;
    }

    private void Start()
    {
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

    public void SetShutterInteractable(bool interactable)
    {
        if (_shutterButton) _shutterButton.interactable = interactable;
    }

    public void PlayFlashEffect()
    {
        if (!_flashOverlay) return;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        _flashOverlay.alpha = 1f;
        if (_flashHoldDuration > 0f) yield return new WaitForSeconds(_flashHoldDuration);

        float t = 0f;
        while (t < _flashFadeDuration)
        {
            t += Time.deltaTime;
            _flashOverlay.alpha = 1f - (t / _flashFadeDuration);
            yield return null;
        }
        _flashOverlay.alpha = 0f;
        _flashCoroutine = null;
    }

    public Texture2D CaptureARCamera()
    {
        if (!_arCamera) return null;

        int width = Screen.width;
        int height = Screen.height;
        RenderTexture rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);

        RenderTexture prevCamTarget = _arCamera.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        _arCamera.targetTexture = rt;
        _arCamera.Render();

        RenderTexture.active = rt;
        Texture2D shot = new Texture2D(width, height, TextureFormat.RGB24, false);
        shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        shot.Apply();

        _arCamera.targetTexture = prevCamTarget;
        RenderTexture.active = prevActive;
        RenderTexture.ReleaseTemporary(rt);

        return shot;
    }

    public void SetCancleButtonAction(UnityAction action)
    {
        if (_cancleButton)
        {
            _cancleButton.onClick.RemoveAllListeners();
            _cancleButton.onClick.AddListener(action);
        }
    }
}
