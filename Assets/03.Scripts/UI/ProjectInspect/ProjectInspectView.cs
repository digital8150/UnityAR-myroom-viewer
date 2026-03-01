using TMPro;
using UnityEngine;
using UnityEngine.UI.ProceduralImage;
using UnityEngine.UI;

public class ProjectInspectView : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private GameObject _page3Done;
    [SerializeField] private GameObject _page4Failed;

    [Header("Page 3 : Done Page")]
    [SerializeField] private ProceduralImage _doneImage;

    [Header("Page 4 : Failed Page")]
    [SerializeField] private Button _reTryTakePicktureBtn;
    [SerializeField] private Button _selectAnotherPictureBtn;
    [SerializeField] private TextMeshProUGUI _generateFailReasonText;

    private static int _selectedModelId;
    public static int SelectedModelId
    {
        set { _selectedModelId = value; }
    }

    public void ShowDonePage()
    {
        if (_page3Done == null)
        {
            return;
        }
        HideAllPage();
        _page3Done?.SetActive(true);
    }

    public void ShowFailedPage()
    {
        if (_page4Failed == null)
        {
            return;
        }
        HideAllPage();
        _page4Failed?.SetActive(true);
    }

    public void UpdateDoneImage(Sprite sprite)
    {
        if (_doneImage != null)
        {
            _doneImage.sprite = sprite;
        }
    }

    public void UpdateFailReason(string reason)
    {
        if (_generateFailReasonText == null) return;
        _generateFailReasonText.text = reason;
    }

    private void HideAllPage()
    {
        if (_page3Done) _page3Done.SetActive(false);
        if (_page4Failed) _page4Failed.SetActive(false);
    }
}
