using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public interface IGenerate3DView
{
    void ShowLandingPage();
    void ShowConvertingPage();

    /// <summary>
    /// 진행도 바 업데이트
    /// </summary>
    /// <param name="progress">0.0~1.0의 값</param>
    void UpdateProgressBar(float progress);

    void UpdateProgressBarSmoothly(float targetProgress, float duration);
}

public class Generate3DView : MonoBehaviour, IGenerate3DView
{
    //--- Settings ---//
    [Header("Pages")]
    [SerializeField]
    private GameObject _page1LandingPage;
    [SerializeField]
    private GameObject _page2Converting;

    [Header("Common")]
    [SerializeField]
    private Button _returnBtn;

    [Header("Page 1 : Landing Page")]
    [SerializeField]
    private Button _loadImageBtn, _takePictureBtn;

    [Header("Page 2 : Converting Page")]
    [SerializeField]
    private RectTransform _convertingProgressBar;

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
        HideAllPage();
        _page1LandingPage.SetActive(true);
    }

    public void ShowConvertingPage()
    {
        HideAllPage();
        _page2Converting.SetActive(true);
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

    //--- Private Methods ---//
    private void InitializeView()
    {
        _presenter = new Generate3DPresenter(this);
        _returnBtn?.onClick.AddListener(_presenter.OnReturnClicked);
        _loadImageBtn?.onClick.AddListener(_presenter.OnLoadImageClicked);
        _takePictureBtn?.onClick.AddListener(_presenter.OnTakePictureClicked);
    }

    private void DisposeListeners()
    {
        _returnBtn?.onClick.RemoveAllListeners();
        _loadImageBtn?.onClick.RemoveAllListeners();
        _takePictureBtn?.onClick.RemoveAllListeners();
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
        _page1LandingPage.SetActive(false);
        _page2Converting.SetActive(false);
    }
}
