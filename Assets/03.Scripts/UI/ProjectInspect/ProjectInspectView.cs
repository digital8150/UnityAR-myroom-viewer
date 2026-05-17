using GLTFast;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.UI.ProceduralImage;

[Serializable]
public class CategoryButton
{
    public Button button;
    public string categoryName;
    public ProceduralImage bgImage;
}

public class ProjectInspectView : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private Button _returnBtn;

    [Header("Showcase Settings")]
    [SerializeField] private float _targetMaxScale = 1.4f;      // 가장 긴 축의 목표 크기
    [SerializeField] private bool _enableAutoRotation = true;  // 자동 회전 여부
    [SerializeField] private float _rotationSpeed = 30f;       // 회전 속도

    [Header("Settings")]
    [SerializeField] private Color _selectedCategoryBGColor = new Color(0.8f, 0.8f, 0.8f);

    [Header("Pages")]
    [SerializeField] private GameObject _page3Done;
    [SerializeField] private GameObject _page4Failed;

    [Header("Page 3 : Done Page")]
    [SerializeField] private Transform _doneModelSpawnPos;
    [SerializeField] private TMP_InputField _widthInputField;
    [SerializeField] private TMP_InputField _heightInputField;
    [SerializeField] private TMP_InputField _lengthInputField;
    [SerializeField] private TMP_InputField _webSiteInputField;
    [SerializeField] private TMP_InputField _nameInputField;
    [SerializeField] private TMP_InputField _descriptionInputField;
    [SerializeField] private Toggle _isPublicToggle;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _onArPlaceButton;
    [SerializeField] private Button _attachImageButton;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private GameObject _shadowCatchPlane;
    [SerializeField] private List<CategoryButton> _categoryButtons;

    [Header("Page 4 : Failed Page")]
    [SerializeField] private Button _reTryBtn;
    [SerializeField] private TextMeshProUGUI _generateFailReasonText;

    [Header("Website Capture")]
    [SerializeField] private WebsiteCaptureView _websiteCapturePrefab;
    [SerializeField] private Transform _websiteCaptureParent; // null 이면 root Canvas 위에 추가

    private ProjectInspectPresenter _presenter;
    private GameObject _spawnedModel; // 생성된 모델 참조용


    private void Awake()
    {
        HideAllPage();
        _presenter = new ProjectInspectPresenter(this, _categoryButtons);
    }

    private void Start()
    {
        _presenter.InitializeView();
        if(_returnBtn) _returnBtn.onClick.AddListener(_presenter.OnReturnClicked);
        if(_reTryBtn) _reTryBtn.onClick.AddListener(_presenter.OnRetryClicked);
        if(_saveButton) _saveButton.onClick.AddListener(async () => await _presenter.OnSaveClicked(false));
        if(_onArPlaceButton) _onArPlaceButton.onClick.AddListener(_presenter.OnARPlaceClicked);
        if(_attachImageButton) _attachImageButton.onClick.AddListener(_presenter.OnAttachImageClicked);
    }

    private void Update()
    {
        // 2번 수정 사항: 런타임에 생성된 모델을 천천히 회전시킴
        UpdateModelRotation();
    }

    private void OnDestroy()
    {
        if(_returnBtn) _returnBtn.onClick.RemoveAllListeners();
        if(_reTryBtn) _reTryBtn.onClick.RemoveAllListeners();
        if(_saveButton) _saveButton.onClick.RemoveAllListeners();
        if(_onArPlaceButton) _onArPlaceButton.onClick.RemoveAllListeners();
        if(_attachImageButton) _attachImageButton.onClick.RemoveAllListeners();

        _presenter?.Cleanup();

        foreach(var item in _categoryButtons)
        {
            if(item.button) item.button.onClick.RemoveAllListeners();
        }
    }

    //--- Page Control ---//
    public void ShowDonePage()
    {
        if (_page3Done == null)
        {
            return;
        }
        HideAllPage();
        if(_page3Done) _page3Done.SetActive(true);
        if(_scrollRect) _scrollRect.verticalNormalizedPosition = 1f; // 페이지 전환 시 스크롤 최상단으로 이동
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

    public void HideAllPage()
    {
        if (_page3Done) _page3Done.SetActive(false);
        if (_page4Failed) _page4Failed.SetActive(false);
    }

    //--- Done Page ---//
    public async void SpawnModel3D(string localModelPath)
    {
        // 기존에 생성된 모델이 있다면 삭제
        if (_spawnedModel) Destroy(_spawnedModel);

        GameObject parentObj = new GameObject("AR_Model_Instance");
        parentObj.transform.position = _doneModelSpawnPos.position;
        parentObj.transform.rotation = _doneModelSpawnPos.rotation;

        var gltf = new GltfImport();
        bool success = await gltf.Load(localModelPath);

        if (success)
        {
            bool instantSuccess = await gltf.InstantiateMainSceneAsync(parentObj.transform);
            if (instantSuccess)
            {
                ScaleToUnitSize(parentObj, _targetMaxScale);
                _spawnedModel = parentObj;

                // 추가된 로직: 모델 바닥에 섀도우 플레인 위치시키기
                UpdateShadowPlanePosition(parentObj);
                ApplyDefaultPBRSettings(parentObj);
                Debug.Log("[ProjectInspectView] Successfully loaded and scaled model for preview");
            }
            else { Destroy(parentObj); }
        }
        else { Destroy(parentObj); }
    }

    public void SetSizeInputField(ModelDimension modelDimension)
    {
        if(_widthInputField)
        {
            _widthInputField.text = modelDimension.width.ToString();
        }
        if(_heightInputField)
        {
            _heightInputField.text = modelDimension.height.ToString();
        }
        if(_lengthInputField)
        {
            _lengthInputField.text = modelDimension.length.ToString();
        }
    }

    public ModelDimension GetSizeInputField()
    {
        ModelDimension dimension = new ModelDimension();
        if(_widthInputField && float.TryParse(_widthInputField.text, out float width))
        {
            dimension.width = width;
        }
        if(_heightInputField && float.TryParse(_heightInputField.text, out float height))
        {
            dimension.height = height;
        }
        if(_lengthInputField && float.TryParse(_lengthInputField.text, out float length))
        {
            dimension.length = length;
        }
        return dimension;
    }

    public void SetWebsiteInputField(string content)
    {
        if(_webSiteInputField)
        {
            _webSiteInputField.text = content;
        }
    }

    public string GetWebsiteInputField()
    {
        if(_webSiteInputField)
        {
            return _webSiteInputField.text;
        }
        return string.Empty;
    }

    public void SetNameInputField(string content)
    {
        if(_nameInputField)
        {
            _nameInputField.text = content;
        }
    }

    public string GetNameInputField()
    {
        if(_nameInputField)
        {
            return _nameInputField.text;
        }
        return string.Empty;
    }

    public void SetDescriptionInputField(string content)
    {
        if(_descriptionInputField)
        {
            _descriptionInputField.text = content;
        }
    }

    public string GetDescriptionInputField()
    {
        if(_descriptionInputField)
        {
            return _descriptionInputField.text;
        }
        return string.Empty;
    }

    public void SetIsPublicToggle(bool isOn)
    {
        if(_isPublicToggle)
        {
            _isPublicToggle.isOn = isOn;
        }
    }

    public bool GetIsPublicToggle()
    {
        if(_isPublicToggle)
        {
            return _isPublicToggle.isOn;
        }
        return false;
    }

    public void SetCategoryButtonSelected(string selectedCategory)
    {
        foreach(var item in _categoryButtons)
        {
            if(item.bgImage) item.bgImage.color = Color.white; // 선택 해제 색상으로 초기화
            if(item.button)
            {
                var text = item.button.GetComponentInChildren<TextMeshProUGUI>();
                if(text) text.color = Color.black; // 선택 해제 텍스트 색상으로 초기화
            }
        }

        var find = _categoryButtons.Find(item => item.categoryName == selectedCategory);
        if (find.bgImage && find.button)
        {
            find.bgImage.color = _selectedCategoryBGColor;
            find.button.GetComponentInChildren<TextMeshProUGUI>().color = Color.white; // 선택된 카테고리 텍스트 색상 변경
        }
        
    }

    //--- Website Capture ---//
    public bool OpenWebsiteCapture(string url, Action<byte[]> onResult)
    {
        if (_websiteCapturePrefab == null)
        {
            Debug.LogError("[ProjectInspectView] _websiteCapturePrefab is not assigned.");
            return false;
        }
        Transform parent = _websiteCaptureParent != null ? _websiteCaptureParent : transform;
        var instance = Instantiate(_websiteCapturePrefab, parent);
        instance.Open(url, onResult);
        return true;
    }

    //--- Failed Page ---//
    public void SetFailReason(string reason)
    {
        if (_generateFailReasonText == null) return;
        _generateFailReasonText.text = reason;
    }

    private void ScaleToUnitSize(GameObject target, float targetScale)
    {
        // 1. 모든 자식 Renderer의 Bounds를 합쳐서 전체 영역 계산
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds combinedBounds = renderers[0].bounds;
        foreach (Renderer render in renderers)
        {
            combinedBounds.Encapsulate(render.bounds);
        }

        // 2. 가장 긴 축의 길이 찾기 ($x, y, z$ 중 max)
        float maxDimension = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);

        if (maxDimension <= 0) return;

        // 3. 현재 스케일을 기준으로 1이 되도록 비율 계산
        // 1 / maxDimension 을 현재 로컬 스케일에 곱해줌
        float scaleFactor = targetScale / maxDimension;
        target.transform.localScale *= scaleFactor;

        // (선택 사항) 피벗이 바닥이 아니라 중앙이라면 위치 보정이 필요할 수도 있어!
    }

    private void UpdateModelRotation()
    {
        // 모델이 존재하고 자동 회전이 활성화된 경우에만 회전
        if (_spawnedModel != null && _enableAutoRotation)
        {
            _spawnedModel.transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateShadowPlanePosition(GameObject target)
    {
        if (_shadowCatchPlane == null) return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // 1. 모델의 전체 Bounds 계산 (Scale된 상태 기준)
        Bounds combinedBounds = renderers[0].bounds;
        foreach (Renderer render in renderers)
        {
            combinedBounds.Encapsulate(render.bounds);
        }

        // 2. Bounds의 최하단 y값 찾기
        float bottomY = combinedBounds.min.y;

        // 3. 섀도우 플레인의 위치를 모델의 바닥 높이로 설정 (x, z는 스폰 위치 유지)
        Vector3 planePos = _shadowCatchPlane.transform.position;
        planePos.y = bottomY + 0.001f; // Z-Fighting 방지를 위해 살짝 띄움
        _shadowCatchPlane.transform.position = planePos;
    }

    private void ApplyDefaultPBRSettings(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                // Metallic 설정 (0 ~ 1)
                if(mat.HasProperty("metallicFactor"))
                    mat.SetFloat("metallicFactor", 0.0f);

                // Roughness 설정 (0 ~ 1)
                if(mat.HasProperty("roughnessFactor"))
                    mat.SetFloat("roughnessFactor", 0.5f);
            }
        }
    }
}
