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
    [SerializeField] private TextMeshProUGUI _statusText;

    public int ModelId { get; set; }

    private void OnDestroy()
    {
        if(!_button) _button.onClick.RemoveAllListeners();
    }

    public void UpdateThumbnailImage(Sprite sprite)
    {
        if(_thumbnailImage) _thumbnailImage.sprite = sprite;
    }

    public void UpdateProjectNameText(string content)
    {
        if(_projectNameText) _projectNameText.text = content;
    }

    public void UpdateStatusText(string content)
    {
        if(_statusText) _statusText.text = content;
    }

    public Button GetButton() => _button;
}
