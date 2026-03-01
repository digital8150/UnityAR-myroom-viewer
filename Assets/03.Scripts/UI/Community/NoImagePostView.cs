using TMPro;
using UnityEngine;

public class NoImagePostView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TextMeshProUGUI _badgeText;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _contentText;
    [SerializeField] private TextMeshProUGUI _infoText;

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
}
