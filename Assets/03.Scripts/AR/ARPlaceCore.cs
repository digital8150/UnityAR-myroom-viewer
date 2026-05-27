using GLTFast;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TMPro;

public class ARPlaceCore : MonoBehaviour
{
    [Header("AR Managers")]
    [SerializeField] private ARRaycastManager _arRaycastManager;
    [SerializeField] private ARPlaneManager _arPlaneManager;
    [SerializeField] private AROcclusionManager _occlusionManager;
    [SerializeField] private Camera _arCamera;

    [Header("Sensitivity Settings")]
    [SerializeField] private float _rotationSensitivity = 0.5f;
    [SerializeField] private float _heightSensitivity = 0.001f;

    [Header("Dimension UI")]
    [SerializeField] private bool _showDimensions = true;
    [SerializeField] private GameObject _dimensionTextPrefab;
    [SerializeField] private LineRenderer _widthLine;
    [SerializeField] private LineRenderer _lengthLine;
    [SerializeField] private LineRenderer _heightLine;

    [Header("New Gesture Settings")]
    [SerializeField] private float _rotationDeadzone = 10f;
    [SerializeField] private float _pinchDeadzone = 20f;

    [Header("Outline")]
    [SerializeField] private Material _outlineMaterial;

    [Header("Furniture Picking")]
    [SerializeField] private LayerMask _furnitureLayer = ~0;

    private const float DimensionValidThreshold = 0.05f;
    private const float DefaultDimensionCm = 50f;

    // Static Data (하위 호환: 진입 직전에 세팅되면 첫 배치에 사용)
    public static string CurrentModelPath;
    public static ModelDimension CurrentModelDimension;

    public event Action OnFirstFurniturePlaced;

    // Internal State
    private readonly List<PlacedFurniture> _placedFurnitures = new List<PlacedFurniture>();
    private PlacedFurniture _selectedFurniture;
    private bool _isModelLoading;
    private bool _defaultModelPlaced;
    private bool _hasDefaultEntry;

    // Touch States
    private List<ARRaycastHit> _hits = new List<ARRaycastHit>();
    private float _initialMidpointX;
    private float _initialMidpointY;
    private float _initialDistance;
    private float _initialHeight;
    private float _yOffsetFromPlane;
    private Quaternion _initialRotation;
    private Vector3 _initialScale;

    // Single-touch translation state
    private bool _singleTouchActsOnSelected;

    // Dimension UI references
    private TMP_Text _widthText, _heightText, _lengthText;
    private GameObject _uiContainer;

    public bool ShowDimensions { get => _showDimensions; set => _showDimensions = value; }
    public bool HasAnyFurniture => _placedFurnitures.Count > 0;

    private class PlacedFurniture
    {
        public GameObject Root;
        public BoxCollider Collider;
        public ModelDimension Dimension;
        public bool AllowScaling;
        public float YOffsetFromPlane;
        public Renderer[] Renderers;
        public Material[][] OriginalMaterials;
        public bool OutlineApplied;
    }

    #region Unity Lifecycle

    void OnEnable() => EnhancedTouchSupport.Enable();
    void OnDisable() => EnhancedTouchSupport.Disable();

    private void Start()
    {
        _hasDefaultEntry = !string.IsNullOrEmpty(CurrentModelPath);

        if (_occlusionManager != null
            && _occlusionManager.descriptor != null
            && _occlusionManager.descriptor.environmentDepthImageSupported == Supported.Unsupported)
        {
            PopupView.AddPopup(new PopupContext("이 기기는 AR 뎁스 기능을 지원하지 않습니다. 일부 기능이 제대로 동작하지 않을 수 있습니다."));
        }

        SetupDimensionUI();
        SetDimensionVisible(false);
    }

    private void Update()
    {
        HandleTouchInput();

        bool dimsActive = _showDimensions && _selectedFurniture != null;

        if (dimsActive)
        {
            UpdateDimensionPositions(_selectedFurniture);
        }

        if (_uiContainer != null && _uiContainer.activeSelf != dimsActive)
        {
            _uiContainer.SetActive(dimsActive);
        }

        if (_widthLine && _heightLine && _lengthLine)
        {
            _widthLine.gameObject.SetActive(dimsActive);
            _heightLine.gameObject.SetActive(dimsActive);
            _lengthLine.gameObject.SetActive(dimsActive);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// 가구 목록 등 외부에서 새 가구를 추가 배치할 때 호출.
    /// 화면 중앙 평면에 자동 배치하고 즉시 선택 상태로 둔다.
    /// </summary>
    public async void PlaceFurnitureFromCatalogue(string modelPath, ModelDimension dimension)
    {
        if (_isModelLoading || string.IsNullOrEmpty(modelPath)) return;

        Vector3 placePos;
        if (TryGetCenterPlanePosition(out var centerPos))
        {
            placePos = centerPos;
        }
        else if (_arCamera != null)
        {
            placePos = _arCamera.transform.position + _arCamera.transform.forward * 1.0f;
        }
        else
        {
            placePos = transform.position;
        }

        var placed = await LoadModelAndPlace(modelPath, dimension, placePos, Quaternion.identity);
        if (placed != null) SelectFurniture(placed);
    }

    #endregion

    #region Touch Logic

    private void HandleTouchInput()
    {
        if (_isModelLoading) return;

        int touchCount = ETouch.activeFingers.Count;
        if (touchCount == 0) return;

        if (IsAnyTouchOverUI()) return;

        if (touchCount == 1)
        {
            HandleSingleTouch();
        }
        else if (touchCount == 2 && _selectedFurniture != null)
        {
            HandleDoubleTouch();
        }
        else if (touchCount == 3 && _selectedFurniture != null)
        {
            HandleTripleTouch();
        }
    }

    private void HandleSingleTouch()
    {
        Finger finger = ETouch.activeFingers[0];
        var phase = finger.currentTouch.phase;

        if (phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            _singleTouchActsOnSelected = false;

            // 1) 가구 콜라이더 픽 시도
            var picked = PickFurnitureAtScreen(finger.screenPosition);
            if (picked != null)
            {
                SelectFurniture(picked);
                _singleTouchActsOnSelected = true;
                return;
            }

            // 2) 평면 picking
            if (_arRaycastManager.Raycast(finger.screenPosition, _hits, TrackableType.PlaneWithinPolygon))
            {
                if (TryGetLowestHorizontalUp(out var lowestPos))
                {
                    // 기본 진입(legacy) — 첫 1회 자동 배치
                    if (_hasDefaultEntry && !_defaultModelPlaced)
                    {
                        _defaultModelPlaced = true;
                        var dim = CurrentModelDimension;
                        // dimension이 유효하지 않으면 50x50x50으로 fallback
                        if (!IsDimensionValid(dim))
                        {
                            dim = new ModelDimension(DefaultDimensionCm, DefaultDimensionCm, DefaultDimensionCm);
                        }
                        _ = PlaceAndSelect(CurrentModelPath, dim, lowestPos);
                        return;
                    }

                    // 그 외 — 빈 평면을 탭 = 선택 해제
                    Deselect();
                }
            }
            else
            {
                Deselect();
            }
        }
        else if (phase == UnityEngine.InputSystem.TouchPhase.Moved)
        {
            if (!_singleTouchActsOnSelected || _selectedFurniture == null) return;

            if (_arRaycastManager.Raycast(finger.screenPosition, _hits, TrackableType.PlaneWithinPolygon))
            {
                if (TryGetLowestHorizontalUp(out var lowestPos))
                {
                    var t = _selectedFurniture.Root.transform;
                    float finalY = lowestPos.y + _selectedFurniture.YOffsetFromPlane;
                    t.position = new Vector3(lowestPos.x, finalY, lowestPos.z);
                }
            }
        }
    }

    private async System.Threading.Tasks.Task PlaceAndSelect(string path, ModelDimension dim, Vector3 pos)
    {
        var placed = await LoadModelAndPlace(path, dim, pos, Quaternion.identity);
        if (placed != null) SelectFurniture(placed);
    }

    private void HandleDoubleTouch()
    {
        Finger f1 = ETouch.activeFingers[0];
        Finger f2 = ETouch.activeFingers[1];

        float currentDistance = Vector2.Distance(f1.screenPosition, f2.screenPosition);
        float currentMidpointX = (f1.screenPosition.x + f2.screenPosition.x) / 2f;

        var sel = _selectedFurniture;

        if (f1.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began ||
            f2.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            _initialMidpointX = currentMidpointX;
            _initialDistance = currentDistance;
            _initialRotation = sel.Root.transform.rotation;
            _initialScale = sel.Root.transform.localScale;
        }
        else if (f1.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                 f2.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
        {
            float deltaMidX = currentMidpointX - _initialMidpointX;

            if (Mathf.Abs(deltaMidX) > _rotationDeadzone)
            {
                float effectiveDeltaX = deltaMidX - (Mathf.Sign(deltaMidX) * _rotationDeadzone);
                sel.Root.transform.rotation = _initialRotation * Quaternion.Euler(0, -effectiveDeltaX * _rotationSensitivity, 0);
            }

            if (sel.AllowScaling)
            {
                float deltaDist = currentDistance - _initialDistance;
                if (Mathf.Abs(deltaDist) > _pinchDeadzone && _initialDistance > 0)
                {
                    float effectiveDeltaDist = deltaDist - (Mathf.Sign(deltaDist) * _pinchDeadzone);
                    float scaleFactor = (_initialDistance + effectiveDeltaDist) / _initialDistance;
                    Vector3 newScale = _initialScale * scaleFactor;

                    float minS = 0.1f;
                    float maxS = 5.0f;

                    sel.Root.transform.localScale = new Vector3(
                        Mathf.Clamp(newScale.x, minS, maxS),
                        Mathf.Clamp(newScale.y, minS, maxS),
                        Mathf.Clamp(newScale.z, minS, maxS)
                    );
                }
            }
        }
    }

    private void HandleTripleTouch()
    {
        Finger f1 = ETouch.activeFingers[0];
        Finger f2 = ETouch.activeFingers[1];
        Finger f3 = ETouch.activeFingers[2];

        float currentMidpointY = (f1.screenPosition.y + f2.screenPosition.y + f3.screenPosition.y) / 3f;

        var sel = _selectedFurniture;

        if (f1.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began ||
            f2.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began ||
            f3.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            _initialMidpointY = currentMidpointY;
            _initialHeight = sel.Root.transform.position.y;
        }
        else if (f1.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                 f2.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                 f3.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
        {
            float deltaY = currentMidpointY - _initialMidpointY;
            float newY = _initialHeight + (deltaY * _heightSensitivity);
            sel.Root.transform.position = new Vector3(sel.Root.transform.position.x, newY, sel.Root.transform.position.z);

            if (_arRaycastManager.Raycast(new Vector2(Screen.width / 2f, Screen.height / 2f), _hits, TrackableType.PlaneWithinPolygon)
                && _hits.Count > 0)
            {
                sel.YOffsetFromPlane = sel.Root.transform.position.y - _hits[0].pose.position.y;
            }
        }
    }

    private bool IsAnyTouchOverUI()
    {
        if (EventSystem.current == null) return false;
        foreach (var f in ETouch.activeFingers)
        {
            if (EventSystem.current.IsPointerOverGameObject(f.index)) return true;
        }
        return false;
    }

    private bool TryGetLowestHorizontalUp(out Vector3 pos)
    {
        bool found = false;
        Vector3 lowest = Vector3.zero;
        foreach (var hit in _hits)
        {
            var plane = _arPlaneManager.GetPlane(hit.trackableId);
            if (plane != null && plane.alignment == PlaneAlignment.HorizontalUp)
            {
                if (!found || hit.pose.position.y < lowest.y)
                {
                    lowest = hit.pose.position;
                    found = true;
                }
            }
        }
        pos = lowest;
        return found;
    }

    private bool TryGetCenterPlanePosition(out Vector3 pos)
    {
        if (_arRaycastManager.Raycast(new Vector2(Screen.width / 2f, Screen.height / 2f), _hits, TrackableType.PlaneWithinPolygon)
            && TryGetLowestHorizontalUp(out var p))
        {
            pos = p;
            return true;
        }
        pos = Vector3.zero;
        return false;
    }

    private PlacedFurniture PickFurnitureAtScreen(Vector2 screenPos)
    {
        var cam = _arCamera != null ? _arCamera : Camera.main;
        if (cam == null) return null;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, _furnitureLayer))
        {
            // hit.collider가 placed 중 하나의 BoxCollider인지 확인
            for (int i = 0; i < _placedFurnitures.Count; i++)
            {
                if (_placedFurnitures[i].Collider == hit.collider)
                    return _placedFurnitures[i];
            }
        }
        return null;
    }

    #endregion

    #region Selection / Outline

    private void SelectFurniture(PlacedFurniture furniture)
    {
        if (_selectedFurniture == furniture) return;

        if (_selectedFurniture != null) RemoveOutline(_selectedFurniture);
        _selectedFurniture = furniture;
        if (_selectedFurniture != null) ApplyOutline(_selectedFurniture);
    }

    private void Deselect()
    {
        if (_selectedFurniture == null) return;
        RemoveOutline(_selectedFurniture);
        _selectedFurniture = null;
    }

    private void ApplyOutline(PlacedFurniture f)
    {
        if (_outlineMaterial == null || f.OutlineApplied || f.Renderers == null) return;

        for (int i = 0; i < f.Renderers.Length; i++)
        {
            var r = f.Renderers[i];
            if (r == null) continue;
            var original = f.OriginalMaterials[i];
            var extended = new Material[original.Length + 1];
            Array.Copy(original, extended, original.Length);
            extended[original.Length] = _outlineMaterial;
            r.materials = extended;
        }
        f.OutlineApplied = true;
    }

    private void RemoveOutline(PlacedFurniture f)
    {
        if (!f.OutlineApplied || f.Renderers == null) return;

        for (int i = 0; i < f.Renderers.Length; i++)
        {
            var r = f.Renderers[i];
            if (r == null) continue;
            r.materials = f.OriginalMaterials[i];
        }
        f.OutlineApplied = false;
    }

    #endregion

    #region Model Loading & Scaling

    private async System.Threading.Tasks.Task<PlacedFurniture> LoadModelAndPlace(string modelPath, ModelDimension dimension, Vector3 position, Quaternion rotation)
    {
        _isModelLoading = true;

        GameObject parentObj = new GameObject("AR_Furniture_Instance");
        parentObj.transform.position = position;
        parentObj.transform.rotation = rotation;
        parentObj.transform.SetParent(this.transform);

        var gltf = new GltfImport();
        bool success = await gltf.Load(modelPath);

        PlacedFurniture placed = null;
        if (success)
        {
            bool instantSuccess = await gltf.InstantiateMainSceneAsync(parentObj.transform);
            if (instantSuccess)
            {
                bool allowScaling = !IsDimensionValid(dimension);
                if (!IsDimensionValid(dimension))
                {
                    dimension = new ModelDimension(DefaultDimensionCm, DefaultDimensionCm, DefaultDimensionCm);
                }

                var box = ApplyRealScale(parentObj, dimension);
                ApplyDefaultPBRSettings(parentObj);

                var renderers = parentObj.GetComponentsInChildren<Renderer>();
                var originalMats = new Material[renderers.Length][];
                for (int i = 0; i < renderers.Length; i++)
                {
                    originalMats[i] = renderers[i].sharedMaterials;
                }

                placed = new PlacedFurniture
                {
                    Root = parentObj,
                    Collider = box,
                    Dimension = dimension,
                    AllowScaling = allowScaling,
                    YOffsetFromPlane = 0f,
                    Renderers = renderers,
                    OriginalMaterials = originalMats,
                };

                _placedFurnitures.Add(placed);
                if (_placedFurnitures.Count == 1)
                {
                    OnFirstFurniturePlaced?.Invoke();
                }
            }
            else
            {
                Destroy(parentObj);
            }
        }
        else
        {
            Destroy(parentObj);
        }

        _isModelLoading = false;
        return placed;
    }

    private BoxCollider ApplyRealScale(GameObject root, ModelDimension dimension)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return null;

        Bounds combinedBounds = renderers[0].bounds;
        foreach (var r in renderers) combinedBounds.Encapsulate(r.bounds);

        Vector3 bottomCenter = new Vector3(combinedBounds.center.x, combinedBounds.min.y, combinedBounds.center.z);
        Vector3 offset = root.transform.position - bottomCenter;

        foreach (Transform child in root.transform) child.position += offset;

        float targetMaxMeter = Mathf.Max(dimension.width, dimension.height, dimension.length) * 0.01f;
        float currentMax = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
        float scaleFactor = (currentMax > 0) ? (targetMaxMeter / currentMax) : 1.0f;

        root.transform.localScale = Vector3.one * scaleFactor;

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0, combinedBounds.size.y / 2f, 0);
        box.size = combinedBounds.size;
        return box;
    }

    private void ApplyDefaultPBRSettings(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("metallicFactor"))
                    mat.SetFloat("metallicFactor", 0.0f);

                if (mat.HasProperty("roughnessFactor"))
                    mat.SetFloat("roughnessFactor", 0.5f);
            }
        }
    }

    #endregion

    #region Dimension UI Logic

    private void SetupDimensionUI()
    {
        if (_dimensionTextPrefab == null) return;
        if (_uiContainer != null) return;

        _uiContainer = new GameObject("Dimension_UI_Container");
        _uiContainer.transform.SetParent(this.transform);

        Canvas canvas = _uiContainer.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        _widthText = CreateText("Width_Text");
        _heightText = CreateText("Height_Text");
        _lengthText = CreateText("Length_Text");
    }

    private TMP_Text CreateText(string name)
    {
        GameObject go = Instantiate(_dimensionTextPrefab, _uiContainer.transform);
        go.name = name;
        return go.GetComponent<TMP_Text>();
    }

    private void SetDimensionVisible(bool visible)
    {
        if (_uiContainer != null) _uiContainer.SetActive(visible);
        if (_widthLine) _widthLine.gameObject.SetActive(visible);
        if (_heightLine) _heightLine.gameObject.SetActive(visible);
        if (_lengthLine) _lengthLine.gameObject.SetActive(visible);
    }

    private void UpdateDimensionPositions(PlacedFurniture f)
    {
        if (f.Collider == null || _widthText == null) return;

        Vector3 size = Vector3.Scale(f.Collider.size, f.Root.transform.localScale);
        Vector3 center = f.Root.transform.position;
        Transform modelTransform = f.Root.transform;

        float textMargin = 0.1f;
        float lineOffset = 0.02f;

        Vector3 halfRight = modelTransform.right * (size.x / 2f);
        Vector3 halfForward = modelTransform.forward * (size.z / 2f);
        Vector3 upFull = modelTransform.up * size.y;

        Vector3 widthStart = center - halfRight + halfForward + (modelTransform.forward * lineOffset);
        Vector3 widthEnd = center + halfRight + halfForward + (modelTransform.forward * lineOffset);

        if (_widthLine != null)
        {
            _widthLine.SetPosition(0, widthStart);
            _widthLine.SetPosition(1, widthEnd);
        }

        _widthText.text = $"{(size.x * 100f):F0} cm";
        _widthText.transform.position = Vector3.Lerp(widthStart, widthEnd, 0.5f) + (modelTransform.forward * textMargin);

        Vector3 depthStart = center + halfRight + halfForward + (modelTransform.right * lineOffset);
        Vector3 depthEnd = center + halfRight - halfForward + (modelTransform.right * lineOffset);

        if (_lengthLine != null)
        {
            _lengthLine.SetPosition(0, depthStart);
            _lengthLine.SetPosition(1, depthEnd);
        }

        _lengthText.text = $"{(size.z * 100f):F0} cm";
        _lengthText.transform.position = Vector3.Lerp(depthStart, depthEnd, 0.5f) + (modelTransform.right * textMargin);

        Vector3 heightStart = center - halfRight + halfForward - (modelTransform.right * lineOffset);
        Vector3 heightEnd = heightStart + upFull;

        if (_heightLine != null)
        {
            _heightLine.SetPosition(0, heightStart);
            _heightLine.SetPosition(1, heightEnd);
        }

        _heightText.text = $"{(size.y * 100f):F0} cm";
        _heightText.transform.position = Vector3.Lerp(heightStart, heightEnd, 0.5f) - (modelTransform.right * textMargin);

        var cam = _arCamera != null ? _arCamera : Camera.main;
        if (cam != null)
        {
            Quaternion camRot = cam.transform.rotation;
            _widthText.transform.rotation = camRot;
            _heightText.transform.rotation = camRot;
            _lengthText.transform.rotation = camRot;
        }
    }

    #endregion

    private static bool IsDimensionValid(ModelDimension d)
    {
        if (d == null) return false;
        return Mathf.Max(d.width, d.height, d.length) > DimensionValidThreshold;
    }
}
