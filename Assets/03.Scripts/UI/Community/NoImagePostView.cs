using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NoImagePostView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] protected TextMeshProUGUI _badgeText;
    [SerializeField] protected TextMeshProUGUI _titleText;
    [SerializeField] protected TextMeshProUGUI _contentText;
    [SerializeField] protected TextMeshProUGUI _infoText;
    [SerializeField] protected Button _button;

    public void SetBadgeText(string text)
    {
        if (_badgeText != null)
        {
            _badgeText.text = text;
        }
    }

    public void SetTitleText(string text) {
        if (_titleText != null)
        {
            _titleText.text = text;
        }
    }

    public void SetContentText(string text) {
        if (_contentText != null)
        {
            _contentText.text = text;
        }
    }   

    public void SetInfoText(string text) {
        if (_infoText != null)
        {
            _infoText.text = text;
        }
    }

    public Button GetButton()
    {
        return _button;
    }
}
