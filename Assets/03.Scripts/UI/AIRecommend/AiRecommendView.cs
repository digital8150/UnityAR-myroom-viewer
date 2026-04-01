using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AIRecommendView : MonoBehaviour
{
    [Header("Page1: Landing")]
    [SerializeField] private GameObject _landingPage;
    [SerializeField] private Button _loadImageButton;
    [SerializeField] private Button _takePictureButton;
    [Space(10)]
    
    [Header("Page2: Select Category")]
    [SerializeField] private GameObject _categoryPage;
    [SerializeField] private List<CategoryButton> _categoryButtons;
    public List<CategoryButton> CategoryButtons => _categoryButtons;
    [SerializeField] private Button _confirmCategoryButton;
    [SerializeField] private Color _selectedCategoryBGColor = new Color(0.8f, 0.8f, 0.8f);
    [Space(10)]

    [Header("page3: Loading")]
    [SerializeField] private GameObject _loadingPage;
    [SerializeField] private Slider _loadingProgressBar;
    [SerializeField] private TextMeshProUGUI _loadingText;
    [Space(10)]

    [Header("Page4: Result")]
    [SerializeField] private GameObject _resultPage;
    [SerializeField] private Image _resultThumbnailImage;
    [SerializeField] private AspectRatioFitter _resultThumbnailImageARF;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private Button _goToRecommendListButton;
    [Space(10)] 

    [Header("Page5: List")]
    [SerializeField] private GameObject _listPage;
    [SerializeField] private Transform _recommendListParent;
    [SerializeField] private RecommendListItemView _listItemPrefab;
    [SerializeField] private TextMeshProUGUI _listResultText;

    private AIRecommendPresenter _presenter;

    #region Unity LifeCycle
    private void Awake()
    {
        _presenter = new AIRecommendPresenter(this);
        _presenter.Initialize();
    }

    private void OnDestroy()
    {
        if(_loadImageButton) _loadImageButton.onClick.RemoveAllListeners();
        if(_takePictureButton) _takePictureButton.onClick.RemoveAllListeners();
        if(_confirmCategoryButton) _confirmCategoryButton.onClick.RemoveAllListeners();
        if(_goToRecommendListButton) _goToRecommendListButton.onClick.RemoveAllListeners();
        foreach(var item in _categoryButtons)
        {
            if(item.button) item.button.onClick.RemoveAllListeners();
        }
    }
    #endregion

    #region Page Controls
    public void ShowLandingPage()
    {
        HideAllPage();
        if (_landingPage) _landingPage.SetActive(true);
    }

    public void ShowCategoryPage()
    {
        HideAllPage();
        if (_categoryPage) _categoryPage.SetActive(true);
    }

    public void ShowLoadingPage()
    {
        HideAllPage();
        if (_loadingPage) _loadingPage.SetActive(true);
    }

    public void ShowResultPage()
    {
        HideAllPage();
        if (_resultPage) _resultPage.SetActive(true);
    }

    public void ShowListPage()
    {
        HideAllPage();
        if (_listPage) _listPage.SetActive(true);
    }

    private void HideAllPage()
    {
        if (_landingPage) _landingPage.SetActive(false);
        if (_categoryPage) _categoryPage.SetActive(false);
        if (_loadingPage) _loadingPage.SetActive(false);
        if (_resultPage) _resultPage.SetActive(false);
        if (_listPage) _listPage.SetActive(false);
    }
    #endregion

    #region Page 1 : Landing
    public void SetLoadImageButtonAction(UnityEngine.Events.UnityAction action)
    {
        if (_loadImageButton)
        {
            _loadImageButton.onClick.RemoveAllListeners();
            _loadImageButton.onClick.AddListener(action);
        }
    }

    public void SetTakePictureButtonAction(UnityEngine.Events.UnityAction action)
    {
        if (_takePictureButton)
        {
            _takePictureButton.onClick.RemoveAllListeners();
            _takePictureButton.onClick.AddListener(action);
        }
    }
    #endregion

    #region Page 2 : Select Category
    /* 다중 선택 카테고리용 
    public void SetCategoryButtonSelected(string selectedCategory, bool isSelected)
    {
        var find = _categoryButtons.Find(item => item.categoryName == selectedCategory);
        if (find.bgImage && find.button)
        {
            find.bgImage.color = isSelected ? _selectedCategoryBGColor : Color.white;
            find.button.GetComponentInChildren<TextMeshProUGUI>().color = isSelected ? Color.white : Color.black; // 선택된 카테고리 텍스트 색상 변경
        }
    }
    */

    public void SetCategoryButtonSelected(string selectedCategory)
    {
        foreach (var item in _categoryButtons)
        {
            if (item.bgImage) item.bgImage.color = Color.white; // 선택 해제 색상으로 초기화
            if (item.button)
            {
                var text = item.button.GetComponentInChildren<TextMeshProUGUI>();
                if (text) text.color = Color.black; // 선택 해제 텍스트 색상으로 초기화
            }
        }

        var find = _categoryButtons.Find(item => item.categoryName == selectedCategory);
        if (find.bgImage && find.button)
        {
            find.bgImage.color = _selectedCategoryBGColor;
            find.button.GetComponentInChildren<TextMeshProUGUI>().color = Color.white; // 선택된 카테고리 텍스트 색상 변경
        }

    }

    public void SetConfirmCategoryButtonAction(UnityEngine.Events.UnityAction action)
    {
        if (_confirmCategoryButton)
        {
            _confirmCategoryButton.onClick.RemoveAllListeners();
            _confirmCategoryButton.onClick.AddListener(action);
        }
    }
    #endregion

    #region Page 3 : Loading
    private Coroutine _loadingCoroutine;
    /// <summary>
    /// 0~1 값으로 진행 도 업데이트
    /// </summary>
    /// <param name="progress">0% : 0, 100% : 1</param>
    public void SetLoadingProgress(float progress)
    {
        if (_loadingCoroutine != null) StopCoroutine(_loadingCoroutine);
        if (_loadingProgressBar) _loadingProgressBar.value = progress;

    }

    public void SetLoadingText(string message)
    {
        if (_loadingText) _loadingText.text = message;
    }

    public void SetLoadingProgressSmooth(float targetProgress, float duration = 0.5f)
    {
        // 이미 실행 중인 보간 작업이 있다면 중지
        if (_loadingCoroutine != null) StopCoroutine(_loadingCoroutine);

        // 새로운 보간 시작
        _loadingCoroutine = StartCoroutine(AnimateProgress(targetProgress, duration));
    }

    private IEnumerator AnimateProgress(float target, float duration)
    {
        if (_loadingProgressBar == null) yield break;

        float startValue = _loadingProgressBar.value;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Mathf.SmoothStep을 사용하면 시작과 끝이 더 부드럽습니다.
            _loadingProgressBar.value = Mathf.Lerp(startValue, target, elapsed / duration);
            yield return null;
        }

        _loadingProgressBar.value = target;
    }
    #endregion

    #region Page 4 : Result
    public void SetResultData(Sprite thumbnail, string resultMessage)
    {
        if (_resultThumbnailImage) _resultThumbnailImage.sprite = thumbnail;
        if (_resultThumbnailImageARF) _resultThumbnailImageARF.aspectRatio = thumbnail.texture.width / thumbnail.texture.height;
        if (_resultText) _resultText.text = resultMessage;
    }

    public void SetResultThumbnail(Texture2D thumbnailTexture)
    {
        if (_resultThumbnailImage && thumbnailTexture)
        {
            var sprite = Sprite.Create(thumbnailTexture, new Rect(0, 0, thumbnailTexture.width, thumbnailTexture.height), new Vector2(0.5f, 0.5f));
            _resultThumbnailImage.sprite = sprite;
            if (_resultThumbnailImageARF) _resultThumbnailImageARF.aspectRatio = thumbnailTexture.width / (float)thumbnailTexture.height;
        }
    }

    public void SetResultText(string text)
    {
        if (_resultText) _resultText.text = text;
    }

    public void SetGoToRecommendListButtonAction(UnityEngine.Events.UnityAction action)
    {
        if (_goToRecommendListButton)
        {
            _goToRecommendListButton.onClick.RemoveAllListeners();
            _goToRecommendListButton.onClick.AddListener(action);
        }
    }
    #endregion

    #region Page 5 : List
    public void SetListResultText(string text)
    {
        if(_listResultText) _listResultText.text = text;
    }

    public void AddListItem(string thumbnailUrl, string title, UnityAction goToInspectAction, UnityAction goToARAction)
    {
        var clone = Instantiate(_listItemPrefab, _recommendListParent);
        clone.SetThumbnail(thumbnailUrl);
        clone.SetTitle(title);
        clone.SetGoToARAction(goToARAction);
        clone.SetGoToInspectAction(goToInspectAction);
    }
    #endregion
}
