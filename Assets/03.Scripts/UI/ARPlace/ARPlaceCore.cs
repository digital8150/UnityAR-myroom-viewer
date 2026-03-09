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
                            // 이동 시에도 현재 모델의 Y값(미세 조정된 값)은 유지하고 싶다면:
                            float currentY = _activeModel.transform.position.y;
                            _activeModel.transform.position = new Vector3(lowestPosition.x, currentY, lowestPosition.z);
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

                // 기존 높이에서 변화량만큼 더함 (감도 조절 필수)
                float newY = _initialHeight + (deltaY * _heightSensitivity);
                _activeModel.transform.position = new Vector3(_activeModel.transform.position.x, newY, _activeModel.transform.position.z);
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

    // 1. 전체 바운드 계산
    Bounds combinedBounds = renderers[0].bounds;
    foreach (var renderer in renderers)
    {
        combinedBounds.Encapsulate(renderer.bounds);
    }

    // 2. 스케일 계산 (목표 크기 m 단위)
    float targetMaxMeter = Mathf.Max(CurrentModelDimension.width, 
                                     CurrentModelDimension.height, 
                                     CurrentModelDimension.length) * 0.01f;
    float currentMax = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
    float scaleFactor = (currentMax > 0) ? (targetMaxMeter / currentMax) : 1.0f;

    // 3. 스케일 적용 (부모인 root에 바로 적용하는 것이 관리하기 편합니다)
    root.transform.localScale = Vector3.one * scaleFactor;

    // 4. 피벗 보정 (모델의 바닥을 root의 위치(0,0,0)로 맞춤)
    // root 내부의 모든 자식을 담고 있는 '인스턴스' 자체를 이동시킵니다.
    // glTFast가 생성한 최상위 자식 오브젝트를 찾아서 이동
    foreach (Transform child in root.transform)
    {
        // 바운드의 중심점에서 바닥까지의 거리(extents.y)를 계산하여 오프셋 결정
        // 주의: world space 바운드를 사용하므로 scale 적용 전/후 계산 순서가 중요합니다.
        float bottomOffset = combinedBounds.min.y - root.transform.position.y;
        child.position -= new Vector3(0, bottomOffset, 0);
    }
}
}