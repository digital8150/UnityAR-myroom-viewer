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
        }
    }

    private void Update()
    {
        HandleTouchInput();

        if (_activeModel != null && _showDimensions)
        {
            UpdateDimensionPositions();
        }
        else if (_uiContainer != null && _uiContainer.activeSelf != _showDimensions)
        {
            _uiContainer.SetActive(_showDimensions);
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
            _gestureMode = 0;
        }
        else if (f1.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                 f2.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
        {
            if (_gestureMode == 0)
            {
                float deltaDist = Mathf.Abs(currentDistance - _initialDistance);
                float deltaMidX = Mathf.Abs(currentMidpointX - _initialMidpointX);

                if (deltaDist > _gestureThreshold) _gestureMode = 2;
                else if (deltaMidX > _gestureThreshold * 0.75f) _gestureMode = 1;
            }

            if (_gestureMode == 1)
            {
                float deltaX = currentMidpointX - _initialMidpointX;
                _activeModel.transform.rotation = _initialRotation * Quaternion.Euler(0, -deltaX * _rotationSensitivity, 0);
            }
            else if (_gestureMode == 2 && _allowModelScaling)
            {
                float scaleFactor = currentDistance / _initialDistance;
                Vector3 newScale = _initialScale * scaleFactor;
                float minS = 0.1f, maxS = 5.0f;
                _activeModel.transform.localScale = new Vector3(
                    Mathf.Clamp(newScale.x, minS, maxS),
                    Mathf.Clamp(newScale.y, minS, maxS),
                    Mathf.Clamp(newScale.z, minS, maxS)
                );
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

        Vector3 worldSize = Vector3.Scale(box.size, _activeModel.transform.localScale);
        Vector3 center = _activeModel.transform.TransformPoint(box.center);

        // 텍스트 위치 잡기 (바닥에서 살짝 위)
        _widthText.text = $"W: {(worldSize.x * 100f):F1}cm";
        _widthText.transform.position = center + _activeModel.transform.forward * (worldSize.z / 2f) + Vector3.up * 0.02f;

        _heightText.text = $"H: {(worldSize.y * 100f):F1}cm";
        _heightText.transform.position = center + _activeModel.transform.right * (worldSize.x / 2f) + _activeModel.transform.forward * (worldSize.z / 2f);

        _lengthText.text = $"L: {(worldSize.z * 100f):F1}cm";
        _lengthText.transform.position = center + _activeModel.transform.right * (worldSize.x / 2f) + Vector3.up * 0.02f;

        // 빌보드 (카메라 바라보기)
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