using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

public class ViewSlotsView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private ProceduralImage _thumbnailImage;
    [SerializeField] private TextMeshProUGUI _projectNameText;
    [SerializeField] private Button _button;

    public void UpdateThumbnailImage(Sprite sprite)
    {
        _thumbnailImage.sprite = sprite;
    }

    public void UpdateProjectNameText(string content)
    {
        _projectNameText.text = content;
    }

    public Button GetButton() => _button;
}
