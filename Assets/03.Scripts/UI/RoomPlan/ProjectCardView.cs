using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ProjectCardView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _cardImage;
    [SerializeField] private AspectRatioFitter _cardImageARF;

    [SerializeField] private TextMeshProUGUI _cardText;
    [SerializeField] private Button _cardButton;

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
    #endregion
}
