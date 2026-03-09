using GLTFast;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ARPlaceCore : MonoBehaviour
{
    [SerializeField] private ARRaycastManager _arRaycastManager;
    [SerializeField] private ARPlaneManager _arPlaneManager;
    [SerializeField] private float _rotationSensitivity = 0.5f;
    [SerializeField] private float _heightSensitivity = 0.001f; // 높이 조절 감도

    public static string CurrentModelPath;
    public static ModelDimension CurrentModelDimension;
    private GameObject _activeModel;
    private List<ARRaycastHit> _hits = new List<ARRaycastHit>();

    // 로딩 중 중복 터치 및 중복 생성 방지
    private bool _isModelLoading = false;
    private int _loggingDelayFrames = 5;
    private int _currentDelayFrames = 0;
    private float _initialMidpointX;
    private Quaternion _initialRotation;
    private float _initialMidpointY;
    private float _initialHeight;
    private float _yOffsetFromPlane = 0f;

    void OnEnable() { EnhancedTouchSupport.Enable(); }
    void OnDisable() { EnhancedTouchSupport.Disable(); }

    private void Awake()
    {

    }

    private void Start()
    {

    }

    private void Update()
    {
        HandleTouchInput();
        DebugModelPosition();
    }

    private void DebugModelPosition()
    {
        if (!_activeModel) return;

        if (_currentDelayFrames < _loggingDelayFrames)
        {
            _currentDelayFrames++;
        }
        else
        {
            Debug.Log($"[ARPlaceCore] Position: {_activeModel.transform.position} | Rotation : {_activeModel.transform.rotation.eulerAngles}");
            _currentDelayFrames = 0;
        }
    }

    private void HandleTouchInput()
    {
        if (string.IsNullOrEmpty(CurrentModelPath) || _isModelLoading) return;

        int touchCount = ETouch.activeFingers.Count;
        if (touchCount == 0) return;

        // --- [1. 한 손가락: 위치 이동 (회전/높이 유지)] ---
        if (touchCount == 1)
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
                            // 평면의 Y값 + 사용자가 세 손가락으로 조절했던 오프셋
                            float finalY = lowestPosition.y + _yOffsetFromPlane;
                            _activeModel.transform.position = new Vector3(lowestPosition.x, finalY, lowestPosition.z);
                        }
                    }
                }
            }
        }
        // --- [2. 두 손가락: Y축 회전] ---
        else if (touchCount == 2 && _activeModel != null)
        {
            Finger f1 = ETouch.activeFingers[0];
            Finger f2 = ETouch.activeFingers[1];

            if (f1.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began ||
                f2.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                _initialMidpointX = (f1.screenPosition.x + f2.screenPosition.x) / 2f;
                _initialRotation = _activeModel.transform.rotation;
            }
            else if (f1.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                     f2.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                float currentMidpointX = (f1.screenPosition.x + f2.screenPosition.x) / 2f;
                float deltaX = currentMidpointX - _initialMidpointX;
                _activeModel.transform.rotation = _initialRotation * Quaternion.Euler(0, -deltaX * _rotationSensitivity, 0);
            }
        }
        // --- [3. 세 손가락: 높이(Y) 미세 조정] ---
        else if (touchCount == 3 && _activeModel != null)
        {
            Finger f1 = ETouch.activeFingers[0];
            Finger f2 = ETouch.activeFingers[1];
            Finger f3 = ETouch.activeFingers[2];

            // 세 손가락 중 하나라도 막 닿았을 때 기준 높이 저장
            if (f1.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began ||
                f2.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began ||
                f3.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                _initialMidpointY = (f1.screenPosition.y + f2.screenPosition.y + f3.screenPosition.y) / 3f;
                _initialHeight = _activeModel.transform.position.y;
            }
            // 위아래로 움직일 때
            else if (f1.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                     f2.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                     f3.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                float currentMidpointY = (f1.screenPosition.y + f2.screenPosition.y + f3.screenPosition.y) / 3f;
                float deltaY = currentMidpointY - _initialMidpointY;

                float newY = _initialHeight + (deltaY * _heightSensitivity);
                _activeModel.transform.position = new Vector3(_activeModel.transform.position.x, newY, _activeModel.transform.position.z);

                // [중요] 조절된 높이와 현재 감지된 평면 사이의 간격을 업데이트
                // 이동 시 이 간격을 유지하기 위함입니다.
                if (_arRaycastManager.Raycast(new Vector2(Screen.width / 2, Screen.height / 2), _hits, TrackableType.PlaneWithinPolygon))
                {
                    _yOffsetFromPlane = _activeModel.transform.position.y - _hits[0].pose.position.y;
                }
            }
        }
    }

    private async void LoadModelAndPlace(Vector3 position, Quaternion rotation)
    {
        _isModelLoading = true; // 로딩 시작 플래그 고정

        GameObject parentObj = new GameObject("AR_Model_Instance");
        parentObj.transform.position = position;
        parentObj.transform.rotation = rotation;

        var gltf = new GltfImport();

        // glTFast 로드 시 Built-in 전용 셰이더가 사용되도록 처리됨
        bool success = await gltf.Load(CurrentModelPath);

        if (success)
        {
            // 인스턴스화 완료까지 대기
            bool instantSuccess = await gltf.InstantiateMainSceneAsync(parentObj.transform);
            if (instantSuccess)
            {
                ApplyRealScale(parentObj);
                _activeModel = parentObj;
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

        _isModelLoading = false; // 로딩 해제
    }

    private void ApplyRealScale(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // 1. 스케일 적용 전 원본 바운드 계산
        Bounds combinedBounds = renderers[0].bounds;
        foreach (var renderer in renderers)
        {
            combinedBounds.Encapsulate(renderer.bounds);
        }

        // 2. 피벗 보정: 모델의 바닥 중앙(bottomCenter)을 부모(root) 위치로 맞추기 위한 오프셋 계산
        Vector3 bottomCenter = new Vector3(combinedBounds.center.x, combinedBounds.min.y, combinedBounds.center.z);
        Vector3 offset = root.transform.position - bottomCenter;

        // 자식들의 월드 위치를 이동시켜 피벗부터 찰떡같이 맞춤
        foreach (Transform child in root.transform)
        {
            child.position += offset;
        }

        // 3. 스케일 계산 및 적용 (바운드 크기는 이동해도 안 변하니까 그대로 씀)
        float targetMaxMeter = Mathf.Max(CurrentModelDimension.width, CurrentModelDimension.height, CurrentModelDimension.length) * 0.01f;
        float currentMax = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
        float scaleFactor = (currentMax > 0) ? (targetMaxMeter / currentMax) : 1.0f;

        root.transform.localScale = Vector3.one * scaleFactor;

        // 4. 콜라이더 추가
        BoxCollider box = root.AddComponent<BoxCollider>();
        // 이미 모델 바닥이 root의 0,0,0에 있으므로, center Y는 사이즈의 절반만 올리면 됨!
        box.center = new Vector3(0, combinedBounds.size.y / 2f, 0);
        box.size = combinedBounds.size;
    }

    private void OnDrawGizmos()
    {
        if (_activeModel == null) return;

        // 1. 모델의 전체 바운드 시각화 (하늘색)
        Renderer[] renderers = _activeModel.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combinedBounds = renderers[0].bounds;
            foreach (var r in renderers) combinedBounds.Encapsulate(r.bounds);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(combinedBounds.center, combinedBounds.size);
        }

        // 2. 피벗 포인트(Root Position) 시각화 (빨간 구체)
        // 이 구체가 모델의 정중앙 바닥에 위치해야 성공입니다.
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_activeModel.transform.position, 0.02f);

        // 3. 정면 방향 표시 (파란 선)
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(_activeModel.transform.position, _activeModel.transform.forward * 0.2f);
    }
}