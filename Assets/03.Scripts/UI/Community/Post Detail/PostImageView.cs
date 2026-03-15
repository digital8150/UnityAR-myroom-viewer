using UnityEngine;
using UnityEngine.UI;

public class PostImageView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Image _image;
    [SerializeField] private AspectRatioFitter _aspectRatioFitter;

    public void UpdateImage(Sprite sprite)
    {
        if(sprite && _image)
        {
            _image.sprite = sprite;
            _aspectRatioFitter.aspectRatio = sprite.rect.width/sprite.rect.height;
        }
        else
        {
            Debug.LogError($"[PostImageView] sprite or image was null");
        }
    }
}
