using GLTFast;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using UnityEngine.UI;
using Utils;

public class ReadonlyProjectInspectView : MonoBehaviour
{
    [Header("Showcase Settings")]
    [SerializeField] private float _targetMaxScale = 1.4f;
    [SerializeField] private bool _enableAutoRotation = true;
    [SerializeField] private float _rotationSpeed = 30f;
    [SerializeField] private Transform _doneModelSpawnPos;
    [SerializeField] private GameObject _shadowCatchPlane;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private Image _profileImage;
    [SerializeField] private TextMeshProUGUI _userNameText;
    [SerializeField] private TextMeshProUGUI _infoText;
    [SerializeField] private TextMeshProUGUI _categoryBadgeText;
    [SerializeField] private TextMeshProUGUI _contentText;
    [SerializeField] private Sprite _defaultProfilePic;
    [SerializeField] private Button _bookmarkButton;
    [SerializeField] private TextMeshProUGUI _bookmarkButtonText;

    private GameObject _spawnedModel;
    private ReadonlyProjectInspectPresenter _presenter;

    private void Awake()
    {
        _presenter = new ReadonlyProjectInspectPresenter(this);
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        Touch.onFingerDown += OnFingerDown;
    }

    private void OnDisable()
    {
        Touch.onFingerDown -= OnFingerDown;
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        UpdateModelRotation();
    }

    private void OnFingerDown(Finger finger)
    {
        if (_contentText == null) return;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(_contentText, finger.screenPosition, null);
        if (linkIndex >= 0)
        {
            Application.OpenURL(_contentText.textInfo.linkInfo[linkIndex].GetLinkID());
        }
    }

    public async void SetContent(ModelDimension modelDimension, ModelData modelData)
    {
        if (_titleText) _titleText.text = modelData.name;
        if (_infoText) _infoText.text = TranslateInfo(modelData);
        if (_userNameText) _userNameText.text = $"{await MemberService.GetMemberUsernameByMemberId(modelData.creatorId)}";

        Sprite userProfilePic = await ImageUtils.LoadSpriteFromUrlAsync(await MemberService.GetMemberProfilePicUrlByMemberId(modelData.creatorId));
        if (userProfilePic == null) userProfilePic = _defaultProfilePic;

        if (_profileImage) _profileImage.sprite = userProfilePic;
        if (_contentText)
        {
            _contentText.text =
                $"크기\r\n" +
                $"<size=80%><color=#585757><font=\"Pretendard-Medium SDF\">{modelDimension} cm</font></color></size>\r\n\r\n" +
                $"상품 페이지\r\n" +
                $"<size=80%><color=#0269E2><link=\"{modelData.shopPageLink}\"><font=\"Pretendard-Medium SDF\">{modelData.shopPageLink}</font></link></color></size>\r\n\r\n" +
                $"설명\r\n" +
                $"<size=80%><color=#585757><font=\"Pretendard-Medium SDF\">{modelData.description}</font></color></size>";
        }
        if (_categoryBadgeText) _categoryBadgeText.text = TranslateCategory(modelData.furniture_type);

        _presenter.Initialize(modelData.id);
    }

    public void SetBookmarkText(bool isBookmarked)
    {
        if (_bookmarkButtonText) _bookmarkButtonText.text = isBookmarked ? "저장됨" : "저장";
    }

    public void SetBookmarkButtonListener(Action action)
    {
        if (_bookmarkButton == null) return;
        _bookmarkButton.onClick.RemoveAllListeners();
        _bookmarkButton.onClick.AddListener(() => action());
    }

    public void Cleanup()
    {
        if (_spawnedModel) Destroy(_spawnedModel);
        _spawnedModel = null;
        if (_shadowCatchPlane) _shadowCatchPlane.SetActive(false);
        if (_bookmarkButton) _bookmarkButton.onClick.RemoveAllListeners();
    }

    public async void SpawnModel3D(string localModelPath)
    {
        if (_spawnedModel) Destroy(_spawnedModel);

        _spawnedModel = new GameObject("AR_Model_Instance");
        _spawnedModel.transform.position = _doneModelSpawnPos.position;
        _spawnedModel.transform.rotation = _doneModelSpawnPos.rotation;

        var gltf = new GltfImport();
        bool success = await gltf.Load(localModelPath);

        if (success)
        {
            bool instantSuccess = await gltf.InstantiateMainSceneAsync(_spawnedModel.transform);
            if (instantSuccess)
            {
                ScaleToUnitSize(_spawnedModel, _targetMaxScale);
                UpdateShadowPlanePosition(_spawnedModel);
                ApplyDefaultPBRSettings(_spawnedModel);
                Debug.Log("[ProjectInspectView] Successfully loaded and scaled model for preview");
            }
            else { Destroy(_spawnedModel); }
        }
        else { Destroy(_spawnedModel); }
    }

    private string TranslateInfo(ModelData modelData)
    {
        return GetRelativeTime(modelData.createdAt);
    }

    private string TranslateCategory(string category)
    {
        return category?.ToLower() switch
        {
            "shelf" => "선반",
            "sofa" => "소파",
            "storage" => "수납장",
            "chair" => "의자",
            "lighting" => "조명",
            "desk" => "책상",
            "bed" => "침대",
            "table" => "테이블",
            "others" => "카테고리 미지정",
            _ => category
        };
    }

    #region private methods
    private void UpdateModelRotation()
    {
        if (_spawnedModel != null && _enableAutoRotation)
        {
            _spawnedModel.transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
        }
    }

    private void ScaleToUnitSize(GameObject target, float targetScale)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds combinedBounds = renderers[0].bounds;
        foreach (Renderer render in renderers)
            combinedBounds.Encapsulate(render.bounds);

        float maxDimension = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
        if (maxDimension <= 0) return;

        target.transform.localScale *= targetScale / maxDimension;
    }

    private void UpdateShadowPlanePosition(GameObject target)
    {
        if (_shadowCatchPlane == null) return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds combinedBounds = renderers[0].bounds;
        foreach (Renderer render in renderers)
            combinedBounds.Encapsulate(render.bounds);

        Vector3 planePos = _shadowCatchPlane.transform.position;
        planePos.y = combinedBounds.min.y + 0.001f;
        _shadowCatchPlane.transform.position = planePos;
    }

    private string GetRelativeTime(string isoDateTime)
    {
        if (string.IsNullOrEmpty(isoDateTime)) return "시간 정보 없음";
        if (!DateTime.TryParse(isoDateTime, out DateTime dateTime)) return "알 수 없음";

        TimeSpan timeSpan = DateTime.Now - dateTime;

        if (timeSpan.TotalSeconds < 60) return "방금 전";
        if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}분 전";
        if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}시간 전";
        if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays}일 전";
        if (timeSpan.TotalDays < 31) return $"{(int)Math.Ceiling(timeSpan.TotalDays / 7)}주 전";

        return dateTime.ToString("yyyy-MM-dd");
    }

    private void ApplyDefaultPBRSettings(GameObject root)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("metallicFactor")) mat.SetFloat("metallicFactor", 0.0f);
                if (mat.HasProperty("roughnessFactor")) mat.SetFloat("roughnessFactor", 0.5f);
            }
        }
    }
    #endregion
}
