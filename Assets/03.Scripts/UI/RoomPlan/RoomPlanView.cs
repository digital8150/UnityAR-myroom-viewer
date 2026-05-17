using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RoomPlanView : MonoBehaviour
{
    [Serializable]
    internal class CategoryButton
    {
        public string CategoryName;
        public Button Button;
    }

    [Header("Page1 : ProjectList")]
    [SerializeField] private GameObject _projectListPage;
    [SerializeField] private ProjectCardView _projectCardPrefab;
    [SerializeField] private Transform _projectCardContainer;
    [SerializeField] private GameObject _projectListEmpty;
    [SerializeField] private Button _newProjectButton;
    [Space(10)]
    [SerializeField] private GameObject _modalPanel;
    [SerializeField] private Button _modalCancleButton;
    [SerializeField] private Button _modalSelectDefaultRoomButton;
    [SerializeField] private Button _modalLoadFloorButton;

    [Header("PlayGround : Edit")]
    [SerializeField] private GameObject _editPage;
    [SerializeField] private RawImage _viewportImage;
    [SerializeField] private TouchView _viewportTouch;
    [SerializeField] private Camera _viewportCamera;
    [SerializeField] private Transform _viewportCameraTarget;
    [SerializeField] private List<CategoryButton> _categoryButtons;
    [SerializeField] private Button _backButton2;
    [SerializeField] private Material _defaultWallMaterial;
    [SerializeField] private GameObject _defaultRoomPrefab;
    [SerializeField] private GameObject _floorPlane;
    [SerializeField] private Sprite _defaultProjectCardThumbnail;

    [Header("PlayGround : Model List")]
    [SerializeField] private ScrollRect _modelScrollRect;
    [SerializeField] private RoomPlanModelButtonView _modelButtonPrefab;
    [SerializeField] private Transform _modelButtonContainer;
    [SerializeField] private GameObject _modelListLoadingIndicator;
    [SerializeField] private GameObject _placementHint;
    [SerializeField] private Color _selectedCategoryColor = new Color(0.2f, 0.6f, 1f);

    [Header("PlayGround : Furniture Controls")]
    [SerializeField] private Button _deleteFurnitureButton;

    [Header("Name Input Modal")]
    [SerializeField] private GameObject _nameInputModal;
    [SerializeField] private TMP_Text _nameInputTitle;
    [SerializeField] private TMP_InputField _nameInputField;
    [SerializeField] private Button _nameInputConfirmButton;
    [SerializeField] private Button _nameInputCancelButton;

    [Header("Common")]
    [SerializeField] private Button _backButton;
    [SerializeField] private Material _selectionIndicatorMaterial;

    public Material SelectionIndicatorMaterial => _selectionIndicatorMaterial;
    public Sprite DefaultProjectCardThumbnail => _defaultProjectCardThumbnail;

    private RoomPlanPresenter _presenter;
    private RenderTexture _viewportRenderTexture;
    private UnityAction<Vector2> _onModelScroll;
    private GameObject _defaultRoomInstance;


    #region Unity Life Cycle
    private void Awake()
    {
        _presenter = new RoomPlanPresenter(this, _viewportTouch, _defaultWallMaterial);
    }

    private void OnEnable()
    {
        if (_modelScrollRect)
            _modelScrollRect.onValueChanged.AddListener(OnModelScrollChanged);
    }

    private void OnDisable()
    {
        if (_modelScrollRect)
            _modelScrollRect.onValueChanged.RemoveListener(OnModelScrollChanged);
    }

    private void OnDestroy()
    {
        if (_viewportRenderTexture != null)
        {
            _viewportRenderTexture.Release();
            _viewportRenderTexture = null;
        }
    }
    #endregion


    #region Public Methods
    public void SetActiveProjectListPage(bool isActive)
    {
        if (_projectListPage == null)
        {
            Debug.LogWarning("RoomPlanView: _projectListPage is null.");
            return;
        }
        _projectListPage.SetActive(isActive);
    }

    public void SetActiveEditPage(bool isActive)
    {
        if (_editPage == null)
        {
            Debug.LogWarning("RoomPlanView: _editPage is null.");
            return;
        }
        _editPage.SetActive(isActive);

        if (isActive)
        {
            Canvas.ForceUpdateCanvases();
            InitViewportRenderTexture();
        }
    }

    public ProjectCardView CreateProjectCard()
    {
        if (_projectCardPrefab == null || _projectCardContainer == null)
        {
            Debug.LogWarning("RoomPlanView: Prefab or Container is missing.");
            return null;
        }
        return Instantiate(_projectCardPrefab, _projectCardContainer);
    }

    public void ClearProjectCards()
    {
        if (_projectCardContainer == null) return;

        foreach (Transform child in _projectCardContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void SetActiveProjectListEmpty(bool isActive)
    {
        if (_projectListEmpty != null)
            _projectListEmpty.SetActive(isActive);
    }

    public void SetActiveModal(bool isActive)
    {
        if (_modalPanel == null)
        {
            Debug.LogWarning("RoomPlanView: _modalPanel is null.");
            return;
        }
        _modalPanel.SetActive(isActive);
    }

    public void SetButtonActions(UnityAction onNewProject, UnityAction onBack, UnityAction onEditBack)
    {
        SetButton(_newProjectButton, onNewProject);
        SetButton(_backButton, onBack);
        SetButton(_backButton2, onEditBack);
    }

    public void SetModalActions(UnityAction onCancel, UnityAction onSelectDefault, UnityAction onLoadFloor)
    {
        SetButton(_modalCancleButton, onCancel);
        SetButton(_modalSelectDefaultRoomButton, onSelectDefault);
        SetButton(_modalLoadFloorButton, onLoadFloor);
    }

    public void SetRenderTextureWithBind(RenderTexture renderTexture)
    {
        if (_viewportImage == null)
        {
            Debug.LogWarning("RoomPlanView: _viewportImage is null.");
            return;
        }
        _viewportImage.texture = renderTexture;

        if(_viewportCamera == null)
        {
            Debug.LogWarning("RoomPlanView: _viewportCamera is null.");
            return;
        }
        _viewportCamera.targetTexture = renderTexture;
    }

    public void SetCategoryButtonAction(string category, UnityAction onClick)
    {
        Button button = _categoryButtons.Find(c => c.CategoryName == category)?.Button;
        if (button == null)
        {
            Debug.LogWarning($"RoomPlanView: No button found for category '{category}'.");
            return;
        }
        SetButton(button, onClick);
    }

    public IReadOnlyList<string> GetCategoryNames()
    {
        var names = new List<string>(_categoryButtons.Count);
        foreach (var c in _categoryButtons) names.Add(c.CategoryName);
        return names;
    }

    public void SetSelectedCategoryVisual(string selectedCategory)
    {
        foreach (var item in _categoryButtons)
        {
            if (item.Button == null) continue;
            var text = item.Button.GetComponentInChildren<TextMeshProUGUI>();
            if (text == null) continue;
            text.color = item.CategoryName == selectedCategory ? _selectedCategoryColor : Color.black;
        }
    }

    // ── Furniture Controls ──────────────────────────────────

    public void SetDeleteFurnitureButtonAction(UnityAction onDelete)
    {
        SetButton(_deleteFurnitureButton, onDelete);
    }

    public void SetDeleteFurnitureButtonActive(bool isActive)
    {
        if (_deleteFurnitureButton != null)
            _deleteFurnitureButton.gameObject.SetActive(isActive);
    }

    // ── Name Input Modal ───────────────────────────────────

    public void SetActiveNameInputModal(bool isActive, string title = null, string initialName = "")
    {
        if (_nameInputModal == null)
        {
            Debug.LogWarning("RoomPlanView: _nameInputModal is null.");
            return;
        }
        _nameInputModal.SetActive(isActive);
        if (!isActive) return;

        if (_nameInputTitle != null && title != null) _nameInputTitle.text = title;
        if (_nameInputField != null) _nameInputField.text = initialName ?? "";
    }

    public string GetNameInputValue()
    {
        return _nameInputField != null ? _nameInputField.text : string.Empty;
    }

    public void SetNameInputActions(UnityAction onConfirm, UnityAction onCancel)
    {
        SetButton(_nameInputConfirmButton, onConfirm);
        SetButton(_nameInputCancelButton, onCancel);
    }

    // ── Default Room / Floor Plane ─────────────────────────

    public void SetFloorPlaneActive(bool isActive)
    {
        if (_floorPlane != null) _floorPlane.SetActive(isActive);
    }

    public GameObject InstantiateDefaultRoom()
    {
        DestroyDefaultRoom();
        if (_defaultRoomPrefab == null)
        {
            Debug.LogWarning("RoomPlanView: _defaultRoomPrefab is null.");
            return null;
        }
        _defaultRoomInstance = Instantiate(_defaultRoomPrefab);
        return _defaultRoomInstance;
    }

    public void DestroyDefaultRoom()
    {
        if (_defaultRoomInstance != null)
        {
            Destroy(_defaultRoomInstance);
            _defaultRoomInstance = null;
        }
    }

    public void SetViewPortCameraPosition(Vector3 position)
    {
        if(!_viewportCameraTarget)
        {
            Debug.LogError("Error : RoomPlanView: _viewportCameraTarget reference is missing.");
            return;
        }
        _viewportCameraTarget.position = position;
    }

    public void SetViewPortCameraRotation(Vector3 rotation)
    {
        if (!_viewportCameraTarget)
        {
            Debug.LogError("Error : RoomPlanView: _viewportCameraTarget reference is missing.");
            return;
        }
        _viewportCameraTarget.rotation = Quaternion.Euler(rotation);
    }

    public float GetViewPortCameraFov()
    {
        if (!_viewportCamera)
        {
            Debug.LogError("Error : RoomPlanView: _viewportCamera reference is missing.");
            return 60f;
        }
        return _viewportCamera.fieldOfView;
    }

    public void SetViewPortCameraFov(float fov)
    {
        if (!_viewportCamera)
        {
            Debug.LogError("Error : RoomPlanView: _viewportCamera reference is missing.");
            return;
        }
        _viewportCamera.fieldOfView = fov;
    }

    public (Vector3 position, Vector3 rotation) GetViewPortCameraTransform()
    {
        if (!_viewportCameraTarget)
        {
            Debug.LogError("Error : RoomPlanView: _viewportCameraTarget reference is missing.");
            return (Vector3.zero, Vector3.zero);
        }
        return (_viewportCameraTarget.position, _viewportCameraTarget.rotation.eulerAngles);
    }

    // ── Placement Hint ─────────────────────────────────────

    public void SetPlacementHintActive(bool isActive)
    {
        if (_placementHint) _placementHint.SetActive(isActive);
    }

    // ── Model List ──────────────────────────────────────────

    public void SetModelScrollListener(UnityAction<Vector2> onScroll)
    {
        _onModelScroll = onScroll;
    }

    public RoomPlanModelButtonView CreateModelButton()
    {
        if (_modelButtonPrefab == null || _modelButtonContainer == null)
        {
            Debug.LogWarning("RoomPlanView: Model button prefab or container is missing.");
            return null;
        }
        return Instantiate(_modelButtonPrefab, _modelButtonContainer);
    }

    public void ClearModelButtons()
    {
        if (_modelButtonContainer == null) return;

        foreach (Transform child in _modelButtonContainer)
            Destroy(child.gameObject);
    }

    public void SetModelListLoading(bool isLoading)
    {
        if (_modelListLoadingIndicator) _modelListLoadingIndicator.SetActive(isLoading);
    }

    // ── Viewport Raycast Helper ─────────────────────────────

    public bool TryGetViewportRay(Vector2 screenPoint, out Ray ray)
    {
        ray = default;
        if (_viewportImage == null || _viewportCamera == null)
        {
            Debug.LogWarning("[TryGetViewportRay] _viewportImage or _viewportCamera is null.");
            return false;
        }

        // RawImage의 4 모서리를 스크린 좌표로 변환
        var corners = new Vector3[4]; // [0]=BL, [1]=TL, [2]=TR, [3]=BR
        _viewportImage.rectTransform.GetWorldCorners(corners);

        Canvas canvas = _viewportImage.canvas;
        Camera uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;

        Vector2 screenBL = uiCamera == null
            ? (Vector2)corners[0]
            : RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
        Vector2 screenTR = uiCamera == null
            ? (Vector2)corners[2]
            : RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);

        float w = screenTR.x - screenBL.x;
        float h = screenTR.y - screenBL.y;
        if (Mathf.Approximately(w, 0f) || Mathf.Approximately(h, 0f)) return false;

        float u = (screenPoint.x - screenBL.x) / w;
        float v = (screenPoint.y - screenBL.y) / h;

        Debug.Log($"[TryGetViewportRay] screen={screenPoint} | BL={screenBL} TR={screenTR} | uv=({u:F3},{v:F3})");

        if (u < 0f || u > 1f || v < 0f || v > 1f) return false;

        ray = _viewportCamera.ViewportPointToRay(new Vector3(u, v, 0f));
        Debug.DrawRay(ray.origin, ray.direction * 200f, Color.cyan, 4f);
        return true;
    }

    #endregion

    #region Private Methods

    private void OnModelScrollChanged(Vector2 pos)
    {
        _onModelScroll?.Invoke(pos);
    }

    private void InitViewportRenderTexture()
    {
        if (_viewportImage == null || _viewportCamera == null) return;

        Canvas canvas = _viewportImage.canvas;
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;

        Rect rect = _viewportImage.rectTransform.rect;
        int width = Mathf.Max(1, Mathf.RoundToInt(rect.width * scaleFactor));
        int height = Mathf.Max(1, Mathf.RoundToInt(rect.height * scaleFactor));

        if (_viewportRenderTexture != null)
            _viewportRenderTexture.Release();

        _viewportRenderTexture = new RenderTexture(width, height, 24)
        {
            antiAliasing = 4
        };
        _viewportRenderTexture.Create();

        _viewportCamera.targetTexture = _viewportRenderTexture;
        _viewportImage.texture = _viewportRenderTexture;
    }

    private void SetButton(Button button, UnityAction action)
    {
        if (button == null)
        {
            Debug.LogWarning($"RoomPlanView: {button} reference is missing.");
            return;
        }

        button.onClick.RemoveAllListeners();
        if (action != null)
        {
            button.onClick.AddListener(action);
        }
    }

    #endregion
}
