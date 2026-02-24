using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

public class Generate3DView : MonoBehaviour
{
    //--- Settings ---//
    [Header("Pages")]
    [SerializeField] private GameObject _page1LandingPage;
    [SerializeField] private GameObject _page3Done;
    [SerializeField] private GameObject _page4Failed;

    [Header("Common")]
    [SerializeField] private Button _returnBtn;

    [Header("Page 1 : Landing Page")]
    [SerializeField] private Button _loadImageBtn;
    [SerializeField] private Button _takePictureBtn;

    [Header("Page 3 : Done Page")]
    [SerializeField] private ProceduralImage _doneImage;

    [Header("Page 4 : Failed Page")]
    [SerializeField] private Button _reTryTakePicktureBtn;
    [SerializeField] private Button _selectAnotherPictureBtn;
    [SerializeField] private TextMeshProUGUI _generateFailReasonText;

    //--- Fields ---//
    private Generate3DPresenter _presenter;

    //--- Unity Methods ---//
    private void Awake()
    {
        InitializeView();
    }

    private void Start()
    {
        ShowLandingPage();
    }

    private void OnDestroy()
    {
        DisposeListeners();
    }

    //--- Public Methods ---//
    public void ShowLandingPage()
    {
        if(_page1LandingPage == null)
        {
            return;
        }
        HideAllPage();
        _page1LandingPage?.SetActive(true);
    }


    public void ShowDonePage()
    {
        if(_page3Done == null)
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
        if(_generateFailReasonText == null) return;
        _generateFailReasonText.text = reason;
    }
    //--- Private Methods ---//
    private void InitializeView()
    {
        _presenter = new Generate3DPresenter(this);
        _returnBtn?.onClick.AddListener(_presenter.OnReturnClicked);
        _loadImageBtn?.onClick.AddListener(_presenter.OnLoadImageClicked);
        _takePictureBtn?.onClick.AddListener(_presenter.OnTakePictureClicked);
        _reTryTakePicktureBtn?.onClick.AddListener(_presenter.OnTakePictureClicked);
        _selectAnotherPictureBtn?.onClick.AddListener(_presenter.OnLoadImageClicked);
    }

    private void DisposeListeners()
    {
        _returnBtn?.onClick.RemoveAllListeners();
        _loadImageBtn?.onClick.RemoveAllListeners();
        _takePictureBtn?.onClick.RemoveAllListeners();
        _reTryTakePicktureBtn?.onClick.RemoveAllListeners();
        _selectAnotherPictureBtn.onClick.RemoveAllListeners();
    }

    private IEnumerator LerpRectTransformRightOffset(RectTransform rectTransform, float targetRightOffset, float duration)
    {
        float elapsed = 0f;
        float initialRightOffset = rectTransform.offsetMax.x;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newRightOffset = Mathf.Lerp(initialRightOffset, targetRightOffset, elapsed / duration);
            rectTransform.offsetMax = new Vector2(newRightOffset, rectTransform.offsetMax.y);
            yield return null;
        }
        rectTransform.offsetMax = new Vector2(targetRightOffset, rectTransform.offsetMax.y);
    }

    private void HideAllPage()
    {
        if (_page1LandingPage) _page1LandingPage.SetActive(false);
        if (_page3Done) _page3Done.SetActive(false);
        if (_page4Failed) _page4Failed.SetActive(false);
    }
}
