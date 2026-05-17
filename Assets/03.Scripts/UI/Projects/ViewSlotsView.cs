using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ViewSlotsView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler
{
    [Header("Components")]
    [SerializeField] private Image _thumbnailImage;
    [SerializeField] private AspectRatioFitter _aspectRatioFitter;
    [SerializeField] private TextMeshProUGUI _projectNameText;
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _statusText;

    [Header("Long Press")]
    [SerializeField] private float _longPressDuration = 0.8f;

    public int ModelId { get; set; }

    private Action _longPressHandler;
    private float _pressStartTime;
    private bool _isPressing;
    private bool _longPressFired;

    private void Update()
    {
        if (!_isPressing || _longPressFired) return;
        if (Time.unscaledTime - _pressStartTime >= _longPressDuration)
        {
            _longPressFired = true;
            _isPressing = false;
            _longPressHandler?.Invoke();
        }
    }

    private void OnDestroy()
    {
        if (_button) _button.onClick.RemoveAllListeners();
        _longPressHandler = null;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPressing = true;
        _longPressFired = false;
        _pressStartTime = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressing = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isPressing = false;
    }

    public void UpdateThumbnailImage(Sprite sprite)
    {
        if(_thumbnailImage) _thumbnailImage.sprite = sprite;
        if(_aspectRatioFitter && sprite) _aspectRatioFitter.aspectRatio = sprite.rect.width / sprite.rect.height;
    }

    public void UpdateProjectNameText(string content)
    {
        if(_projectNameText) _projectNameText.text = content;
    }

    public void UpdateStatusText(string content)
    {
        if(_statusText) _statusText.text = content;
    }

    public void SetClickHandler(UnityAction handler)
    {
        if (!_button) return;
        _button.onClick.RemoveAllListeners();
        if (handler == null) return;
        _button.onClick.AddListener(() =>
        {
            if (_longPressFired) return;
            handler();
        });
    }

    public void SetLongPressHandler(Action handler)
    {
        _longPressHandler = handler;
    }

    public Button GetButton() => _button;
}
