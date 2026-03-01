using TMPro;
using UnityEngine;
using UnityEngine.UI.ProceduralImage;
using UnityEngine.UI;

public class WithImagePostView : NoImagePostView
{
    [Header("Components")]
    [SerializeField] private ProceduralImage _thumbnailImage;

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
