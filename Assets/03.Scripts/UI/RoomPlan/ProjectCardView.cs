using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ProjectCardView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image _cardImage;
    [SerializeField] private AspectRatioFitter _cardImageARF;

    [SerializeField] private TextMeshProUGUI _cardText;
    [SerializeField] private Button _cardButton;

    [Header("Long Press")]
    [SerializeField] private float _longPressDuration = 0.7f;

    private UnityAction _onLongPress;
    private float _pressStartTime = -1f;
    private bool _longPressFired = false;

    #region Public Methods
    public void SetCardImage(Sprite image)
    {
        if(!image || !_cardImage || !_cardImageARF)
        {
            Debug.LogWarning("ProjectCardView: SetCardImage - Missing references or image is null.");
            return;
        }

        _cardImage.sprite = image;
        _cardImageARF.aspectRatio = (float)image.rect.width / image.rect.height;
    }

    public void SetCardText(string text)
    {
        if(!_cardText)
        {
            Debug.LogWarning("ProjectCardView: SetCardText - Missing reference to TextMeshProUGUI.");
            return;
        }
        _cardText.text = text;
    }

    public void SetButtonAction(UnityAction action)
    {
        if(!_cardButton)
        {
            Debug.LogWarning("ProjectCardView: SetButtonAction - Missing reference to Button.");
            return;
        }
        _cardButton.onClick.RemoveAllListeners();
        if(action != null)
        {
            _cardButton.onClick.AddListener(action);
        }
    }

    public void SetLongPressAction(UnityAction action)
    {
        _onLongPress = action;
    }
    #endregion

    private void Update()
    {
        if (_longPressFired || _pressStartTime < 0f) return;
        if (Time.unscaledTime - _pressStartTime < _longPressDuration) return;

        _longPressFired = true;
        // 롱프레스 발동 시 클릭 발화를 막기 위해 Button을 잠시 비활성화
        if (_cardButton != null) _cardButton.interactable = false;
        _onLongPress?.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressStartTime = Time.unscaledTime;
        _longPressFired = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetPress();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetPress();
    }

    private void ResetPress()
    {
        _pressStartTime = -1f;
        // 다음 입력에 대비해 다시 활성화
        if (_longPressFired && _cardButton != null) _cardButton.interactable = true;
        _longPressFired = false;
    }
}
