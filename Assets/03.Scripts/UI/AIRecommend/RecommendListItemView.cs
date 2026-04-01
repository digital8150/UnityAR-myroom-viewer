using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecommendListItemView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Image _thumbnbailImage;
    [SerializeField] private AspectRatioFitter _thumbnailImageARF;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private Button _goToInspect;
    [SerializeField] private Button _goToAR;

    private void OnDestroy()
    {
        if(_goToInspect)
        {
            _goToInspect.onClick.RemoveAllListeners();
        }

        if(_goToAR)
        {
            _goToAR.onClick.RemoveAllListeners();
        }
    }

    public void SetThumbnail(Sprite thumbnail)
    {
        if(!_thumbnailImageARF && !_thumbnbailImage)
        {
            Debug.LogError("[RecommendListItemView] Thumbnail components are not assigned.");
            return;
        }

        _thumbnbailImage.sprite = thumbnail;
        if (thumbnail != null)
        {
            _thumbnailImageARF.aspectRatio = (float)thumbnail.texture.width / thumbnail.texture.height;
        }
    }

    public async void SetThumbnail(string imageUrl)
    {
        Sprite thumbnail = await Utils.ImageUtils.LoadSpriteFromUrlAsync(imageUrl);
        if (thumbnail != null)
        {
            SetThumbnail(thumbnail);
        }
        else
        {
            Debug.LogError($"[RecommendListItemView] Failed to load thumbnail from URL: {imageUrl}");
        }
    }

    public void SetTitle(string title)
    {
        if (!_titleText)
        {
            Debug.LogError("[RecommendListItemView] Title Text component is not assigned.");
            return;
        }
        _titleText.text = title;
    }

    public void SetGoToInspectAction(UnityEngine.Events.UnityAction action)
    {
        if (!_goToInspect)
        {
            Debug.LogError("[RecommendListItemView] GoToInspect Button component is not assigned.");
            return;
        }
        _goToInspect.onClick.RemoveAllListeners();
        _goToInspect.onClick.AddListener(action);
    }

    public void SetGoToARAction(UnityEngine.Events.UnityAction action)
    {
        if (!_goToAR)
        {
            Debug.LogError("[RecommendListItemView] GoToAR Button component is not assigned.");
            return;
        }
        _goToAR.onClick.RemoveAllListeners();
        _goToAR.onClick.AddListener(action);
    }
}
