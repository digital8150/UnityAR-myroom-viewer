using GLTFast;
using System.Collections.Generic;
using UnityEngine;
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

    [Header("Sensitivity Settings")]
    [SerializeField] private float _rotationSensitivity = 0.5f;
    [SerializeField] private float _heightSensitivity = 0.001f;
    [SerializeField] private float _gestureThreshold = 20f; // 회전/스케일 판정 임계값

    [Header("Dimension UI")]
    [SerializeField] private bool _showDimensions = true;
    [SerializeField] private GameObject _dimensionTextPrefab; // TMP_Text 프리팹 (World Space)
    [SerializeField] private LineRenderer _widthLine;
    [SerializeField] private LineRenderer _lengthLine;
    [SerializeField] private LineRenderer _heightLine;

    [Header("New Gesture Settings")]
    [SerializeField] private float _rotationDeadzone = 10f; // 회전 시작을 위한 최소 중심점 이동거리 (픽셀)
    [SerializeField] private float _pinchDeadzone = 20f;     // 스케일 변경을 위한 최소 거리 변화 (픽셀)

    private const float DimensionValidThreshold = 0.05f;

    // Static Data
    public static string CurrentModelPath;
    public static ModelDimension CurrentModelDimension;

    // Internal State
    private GameObject _activeModel;
    private List<ARRaycastHit> _hits = new List<ARRaycastHit>();
    private bool _isModelLoading = false;
    private bool _allowModelScaling = false;

    // Touch States
    private int _gestureMode = 0; // 0: None, 1: Rotate, 2: Scale
    private float _initialMidpointX;
    private float _initialMidpointY;
    private float _initialDistance;
    private float _initialHeight;
    private float _yOffsetFromPlane = 0f;
    private Quaternion _initialRotation;
    private Vector3 _initialScale;

    // UI References
    private TMP_Text _widthText, _heightText, _lengthText;
    private GameObject _uiContainer;

    public bool ShowDimensions { get => _showDimensions; set => _showDimensions = value; }

    #region Unity Lifecycle

    void OnEnable() => EnhancedTouchSupport.Enable();
    void OnDisable() => EnhancedTouchSupport.Disable();

    private void Start()
    {
        if (!IsModelDimensionValid())
        {
            _allowModelScaling = true;
            CurrentModelDimension = new ModelDimension(50, 50, 50);
        }
    }

    private void Update()
    {
        HandleTouchInput();

        if (_activeModel != null && _showDimensions)
        {
            UpdateDimensionPositions();
        }
        
        if (_uiContainer != null && _uiContainer.activeSelf != _showDimensions)
        {
            _uiContainer.SetActive(_showDimensions);
        }

        if (_widthLine && _heightLine && _lengthLine)
        {
            _widthLine.gameObject.SetActive(_showDimensions);
            _heightLine.gameObject.SetActive(_showDimensions);
            _lengthLine.gameObject.SetActive(_showDimensions);
        }
    }

    #endregion

    #region Touch Logic

    private void HandleTouchInput()
    {
        if (string.IsNullOrEmpty(CurrentModelPath) || _isModelLoading) return;

        int touchCount = ETouch.activeFingers.Count;
        if (touchCount == 0) return;

        if (touchCount == 1)
        {
            HandleSingleTouch();
        }
        else if (touchCount == 2 && _activeModel != null)
        {
            HandleDoubleTouch();
        }
        else if (touchCount == 3 && _activeModel != null)
        {
            HandleTripleTouch();
        }
    }

    private void HandleSingleTouch()
    {
        Finger finger = ETouch.activeFingers[0];
        if (finger.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began ||
            finger.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
        {
            if (_arRaycastManager.Raycast(finger.currentTouch.screenPosition, _hits, TrackableType.PlaneWithinPolygon))
            {
                bool foundValidPlane = false;
                Vector3 lowestPosition = Vector3.zero;

                foreach (var hit in _hits)
                {
                    ARPlane hitPlane = _arPlaneManager.GetPlane(hit.trackableId);
                    if (hitPlane != null && hitPlane.alignment == PlaneAlignment.HorizontalUp)
                    {
                        if (!foundValidPlane || hit.pose.position.y < lowestPosition.y)
                        {
                            lowestPosition = hit.pose.position;
                            foundValidPlane = true;
                        }
                    }
                }

                if (foundValidPlane)
                {
                    if (finger.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began && _activeModel == null)
                    {
                        LoadModelAndPlace(lowestPosition, Quaternion.identity);
                    }
                    else if (finger.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved && _activeModel != null)
                    {
                        float finalY = lowestPosition.y + _yOffsetFromPlane;
                        _activeModel.transform.position = new Vector3(lowestPosition.x, finalY, lowestPosition.z);
                    }
                }
            }
        }
    }

    private void HandleDoubleTouch()
    {
        Finger f1 = ETouch.activeFingers[0];
        Finger f2 = ETouch.activeFingers[1];

        float currentDistance = Vector2.Distance(f1.screenPosition, f2.screenPosition);
        float currentMidpointX = (f1.screenPosition.x + f2.screenPosition.x) / 2f;

        if (f1.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began ||
            f2.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            _initialMidpointX = currentMidpointX;
            _initialDistance = currentDistance;
            _initialRotation = _activeModel.transform.rotation;
            _initialScale = _activeModel.transform.localScale;

            // _gestureMode 관련 로직 삭제
        }
        else if (f1.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                 f2.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
        {
            // --- 1. 독립적인 Y축 회전 로직 ---
            float deltaMidX = currentMidpointX - _initialMidpointX;

            // 회전 데드존 체크: 중심점의 누적 이동거리가 데드존보다 클 때만 회전 적용
            if (Mathf.Abs(deltaMidX) > _rotationDeadzone)
            {
                // 데드존만큼을 뺀 값으로 회전 계산 (갑작스런 튀는 현상 방지)
                float effectiveDeltaX = deltaMidX - (Mathf.Sign(deltaMidX) * _rotationDeadzone);
                _activeModel.transform.rotation = _initialRotation * Quaternion.Euler(0, -effectiveDeltaX * _rotationSensitivity, 0);
            }

            // --- 2. 독립적인 스케일링 로직 ---
            if (_allowModelScaling)
            {
                float deltaDist = currentDistance - _initialDistance;

                if (Mathf.Abs(deltaDist) > _pinchDeadzone)
                {
                    float effectiveDeltaDist = deltaDist - (Mathf.Sign(deltaDist) * _pinchDeadzone);

                    if (_initialDistance > 0)
                    {
                        float scaleFactor = (_initialDistance + effectiveDeltaDist) / _initialDistance;
                        Vector3 newScale = _initialScale * scaleFactor;

                        // 최소/최대 제한값
                        float minS = 0.1f;
                        float maxS = 5.0f;

                        // Vector3는 직접 Clamp가 안 되므로 Mathf.Clamp로 각각 조절!
                        _activeModel.transform.localScale = new Vector3(
                            Mathf.Clamp(newScale.x, minS, maxS),
                            Mathf.Clamp(newScale.y, minS, maxS),
                            Mathf.Clamp(newScale.z, minS, maxS)
                        );
                    }
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

        if (f1.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began ||
            f2.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began ||
            f3.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            _initialMidpointY = currentMidpointY;
            _initialHeight = _activeModel.transform.position.y;
        }
        else if (f1.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                 f2.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                 f3.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
        {
            float deltaY = currentMidpointY - _initialMidpointY;
            float newY = _initialHeight + (deltaY * _heightSensitivity);
            _activeModel.transform.position = new Vector3(_activeModel.transform.position.x, newY, _activeModel.transform.position.z);

            if (_arRaycastManager.Raycast(new Vector2(Screen.width / 2, Screen.height / 2), _hits, TrackableType.PlaneWithinPolygon))
            {
                _yOffsetFromPlane = _activeModel.transform.position.y - _hits[0].pose.position.y;
            }
        }
    }

    #endregion

    #region Model Loading & Scaling

    private async void LoadModelAndPlace(Vector3 position, Quaternion rotation)
    {
        _isModelLoading = true;
        GameObject parentObj = new GameObject("AR_Model_Instance");
        parentObj.transform.position = position;
        parentObj.transform.rotation = rotation;

        var gltf = new GltfImport();
        bool success = await gltf.Load(CurrentModelPath);

        if (success)
        {
            bool instantSuccess = await gltf.InstantiateMainSceneAsync(parentObj.transform);
            if (instantSuccess)
            {
                ApplyRealScale(parentObj);
                _activeModel = parentObj;
                SetupDimensionUI();
            }
            else { Destroy(parentObj); }
        }
        else { Destroy(parentObj); }

        _isModelLoading = false;
    }

    private void ApplyRealScale(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds combinedBounds = renderers[0].bounds;
        foreach (var r in renderers) combinedBounds.Encapsulate(r.bounds);

        Vector3 bottomCenter = new Vector3(combinedBounds.center.x, combinedBounds.min.y, combinedBounds.center.z);
        Vector3 offset = root.transform.position - bottomCenter;

        foreach (Transform child in root.transform) child.position += offset;

        float targetMaxMeter = Mathf.Max(CurrentModelDimension.width, CurrentModelDimension.height, CurrentModelDimension.length) * 0.01f;
        float currentMax = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
        float scaleFactor = (currentMax > 0) ? (targetMaxMeter / currentMax) : 1.0f;

        root.transform.localScale = Vector3.one * scaleFactor;

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0, combinedBounds.size.y / 2f, 0);
        box.size = combinedBounds.size;
    }

    #endregion

    #region Dimension UI Logic

    private void SetupDimensionUI()
    {
        if (_activeModel == null) return;
        if (_uiContainer != null) Destroy(_uiContainer);

        _uiContainer = new GameObject("Dimension_UI_Container");
        _uiContainer.transform.SetParent(_activeModel.transform);

        // 🚨 핵심: UI 렌더링을 위한 Canvas 세팅 추가!
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

    private void UpdateDimensionPositions()
    {
        BoxCollider box = _activeModel.GetComponent<BoxCollider>();
        if (box == null || _widthText == null) return;

        // 가구의 실제 월드 스케일이 적용된 사이즈
        Vector3 size = Vector3.Scale(box.size, _activeModel.transform.localScale);
        Vector3 center = _activeModel.transform.position; // 가구의 바닥 중심
        Transform modelTransform = _activeModel.transform;

        // 텍스트를 선에서 띄울 간격 & 선을 모델에서 살짝 띄울 간격
        float textMargin = 0.1f;
        float lineOffset = 0.02f;

        // 기준이 되는 절반 크기 벡터
        Vector3 halfRight = modelTransform.right * (size.x / 2f);
        Vector3 halfForward = modelTransform.forward * (size.z / 2f);
        Vector3 upFull = modelTransform.up * size.y;

        // --- 1. 가로 (Width): 정면 바닥 ---
        // 왼쪽 밑에서 오른쪽 밑으로 선 긋기
        Vector3 widthStart = center - halfRight + halfForward + (modelTransform.forward * lineOffset);
        Vector3 widthEnd = center + halfRight + halfForward + (modelTransform.forward * lineOffset);

        if (_widthLine != null)
        {
            _widthLine.SetPosition(0, widthStart);
            _widthLine.SetPosition(1, widthEnd);
        }

        // 텍스트는 선의 중앙에 배치
        _widthText.text = $"{(size.x * 100f):F0} cm";
        _widthText.transform.position = Vector3.Lerp(widthStart, widthEnd, 0.5f) + (modelTransform.forward * textMargin);

        // --- 2. 세로 (Depth): 우측 바닥 ---
        // 앞쪽 밑에서 뒤쪽 밑으로 선 긋기
        Vector3 depthStart = center + halfRight + halfForward + (modelTransform.right * lineOffset);
        Vector3 depthEnd = center + halfRight - halfForward + (modelTransform.right * lineOffset);

        if (_lengthLine != null)
        {
            _lengthLine.SetPosition(0, depthStart);
            _lengthLine.SetPosition(1, depthEnd);
        }

        _lengthText.text = $"{(size.z * 100f):F0} cm";
        _lengthText.transform.position = Vector3.Lerp(depthStart, depthEnd, 0.5f) + (modelTransform.right * textMargin);

        // --- 3. 높이 (Height): 좌측 앞 모서리 ---
        // 왼쪽 앞 바닥에서 위로 선 긋기
        Vector3 heightStart = center - halfRight + halfForward - (modelTransform.right * lineOffset);
        Vector3 heightEnd = heightStart + upFull;

        if (_heightLine != null)
        {
            _heightLine.SetPosition(0, heightStart);
            _heightLine.SetPosition(1, heightEnd);
        }

        _heightText.text = $"{(size.y * 100f):F0} cm";
        _heightText.transform.position = Vector3.Lerp(heightStart, heightEnd, 0.5f) - (modelTransform.right * textMargin);

        // --- 빌보드 & 회전 고정 ---
        Quaternion camRot = Camera.main.transform.rotation;
        _widthText.transform.rotation = camRot;
        _heightText.transform.rotation = camRot;
        _lengthText.transform.rotation = camRot;
    }

    #endregion

    private bool IsModelDimensionValid()
    {
        if (CurrentModelDimension == null) return false;
        return Mathf.Max(CurrentModelDimension.width, CurrentModelDimension.height, CurrentModelDimension.length) > DimensionValidThreshold;
    }
}