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

    [Header("Common")]
    [SerializeField] private Button _returnBtn;

    [Header("Page 1 : Landing Page")]
    [SerializeField] private Button _loadImageBtn;
    [SerializeField] private Button _takePictureBtn;

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
        if (_page1LandingPage) _page1LandingPage.SetActive(false);
    }
}
