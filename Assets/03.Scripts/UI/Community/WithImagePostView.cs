using TMPro;
using UnityEngine;
using UnityEngine.UI.ProceduralImage;

public class WithImagePostView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TextMeshProUGUI _badgeText;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _contentText;
    [SerializeField] private TextMeshProUGUI _infoText;
    [SerializeField] private ProceduralImage _thumbnailImage;

    public void SetBadgeText(string text)
    {
        if (_badgeText != null)
        {
            _badgeText.text = text;
        }
    }

    public void SetTitleText(string text)
    {
        if (_titleText != null)
        {
            _titleText.text = text;
        }
    }

    public void SetContentText(string text)
    {
        if (_contentText != null)
        {
            _contentText.text = text;
        }
    }

    public void SetInfoText(string text)
    {
        if (_infoText != null)
        {
            _infoText.text = text;
        }
    }

    public void SetThumbnail(Sprite sprite)
    {
        if (_thumbnailImage != null)
        {
            _thumbnailImage.sprite = sprite;
        }
    }

    public async void SetThumbnail(string imageUrl)
    {
        if(_thumbnailImage != null)
        {
            Sprite sprite = await Utils.ImageUtils.LoadSpriteFromUrl(imageUrl);
            if (sprite != null)
            {
                _thumbnailImage.sprite = sprite;
            }
        }
    }
}
