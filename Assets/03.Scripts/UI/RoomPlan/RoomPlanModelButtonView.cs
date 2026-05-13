using UnityEngine;
using UnityEngine.UI;

public class RoomPlanModelButtonView : MonoBehaviour
{
    [SerializeField] private Button _mainButton;
    [SerializeField] private Button _infoButton;
    [SerializeField] private Image _mainImage;
    [SerializeField] private AspectRatioFitter _mainImageARF;
    [SerializeField] private GameObject _selectionOutline;

    public Button MainButton => _mainButton;
    public Button InfoButton => _infoButton;

    public void SetImage(Sprite sprite)
    {
        if(_mainImage) _mainImage.sprite = sprite;
        if(_mainImageARF) _mainImageARF.aspectRatio = sprite.rect.width / sprite.rect.height;
    }

    public void SetSelected(bool selected)
    {
        if (_selectionOutline) _selectionOutline.SetActive(selected);
    }
}
