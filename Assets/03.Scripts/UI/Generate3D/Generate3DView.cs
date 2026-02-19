using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

public interface IGenerate3DView
{
    void ShowLandingPage();
    void ShowConvertingPage();
    void ShowDonePage();
    void ShowFailedPage();

    /// <summary>
    /// 진행도 바 업데이트
    /// </summary>
    /// <param name="progress">0.0~1.0의 값</param>
    void UpdateProgressBar(float progress);

    /// <summary>
    /// 진행도바를 부드럽게 업데이트
    /// </summary>
    /// <param name="targetProgress"> 목표 진행도 </param>
    /// <param name="duration"> 지속시간 </param>
    void UpdateProgressBarSmoothly(float targetProgress, float duration);

    void UpdateDoneImage(Sprite sprite);

    void UpdateFailReason(string reason);
}

public class Generate3DView : MonoBehaviour, IGenerate3DView
{
    //--- Settings ---//
    [Header("Pages")]
    [SerializeField] private GameObject _page1LandingPage;
    [SerializeField] private GameObject _page2Converting;
    [SerializeField] private GameObject _page3Done;
    [SerializeField] private GameObject _page4Failed;

    [Header("Common")]
    [SerializeField] private Button _returnBtn;

    [Header("Page 1 : Landing Page")]
    [SerializeField] private Button _loadImageBtn;
    [SerializeField] private Button _takePictureBtn;

    [Header("Page 2 : Converting Page")]
    [SerializeField] private RectTransform _convertingProgressBar;

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

    public void ShowConvertingPage()
    {
        if(_page2Converting == null)
        {
            return;
        }
        HideAllPage();
        _page2Converting?.SetActive(true);
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

    public void UpdateProgressBar(float progress)
    {
        if (_convertingProgressBar == null) return;

        progress = Mathf.Clamp01(progress);

        // 공식 수정: (progress - 1) * 354
        // progress가 0이면 -354 (인스펙터 Right: 354)
        // progress가 1이면 0 (인스펙터 Right: 0)
        float rightOffset = (progress - 1) * 354f;
        _convertingProgressBar.offsetMax = new Vector2(rightOffset, _convertingProgressBar.offsetMax.y);
    }

    public void UpdateProgressBarSmoothly(float targetProgress, float duration)
    {
        if (_convertingProgressBar == null) return;

        targetProgress = Mathf.Clamp01(targetProgress);
        float targetRightOffset = (targetProgress - 1) * 354f;

        StartCoroutine(LerpRectTransformRightOffset(_convertingProgressBar, targetRightOffset, duration));
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
        if (_page2Converting) _page2Converting.SetActive(false);
        if (_page3Done) _page3Done.SetActive(false);
        if (_page4Failed) _page4Failed.SetActive(false);
    }
}
