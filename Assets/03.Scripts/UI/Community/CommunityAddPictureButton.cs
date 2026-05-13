using UnityEngine;
using UnityEngine.UI;

public class CommunityAddPictureButton : MonoBehaviour
{
    [SerializeField] private GameObject _emptyState;
    [SerializeField] private GameObject _filledState;
    [SerializeField] private Button _addPictureButton;
    [SerializeField] private Button _removePictureButton;
    [SerializeField] private Image _showingImage;

    private string _imageUrl;

    public GameObject EmptyState => _emptyState;
    public GameObject FilledState => _filledState;
    public Button AddPictureButton => _addPictureButton;
    public Button RemovePictureButton => _removePictureButton;
    public Image ShowingImage => _showingImage;
    public string ImageUrl => _imageUrl;

    public void SetImageUrl(string url)
    {
        _imageUrl = url;
    }
}