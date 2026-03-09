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

        // --- [한 손가락: 위치 이동/생성 (최저점 바닥 찾기)] ---
        if (touchCount == 1)
        {
            Finger finger = ETouch.activeFingers[0];

            if (finger.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began ||
                finger.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                if (_arRaycastManager.Raycast(finger.currentTouch.screenPosition, _hits, TrackableType.PlaneWithinPolygon))
                {
                    bool foundValidPlane = false;
                    Pose lowestPose = new Pose();

                    // 1. 부딪힌 모든 평면을 뒤져서 가장 Y값이 낮은 '찐 바닥' 찾기
                    foreach (var hit in _hits)
                    {
                        ARPlane hitPlane = _arPlaneManager.GetPlane(hit.trackableId);

                        if (hitPlane != null && hitPlane.alignment == PlaneAlignment.HorizontalUp)
                        {
                            // 처음 찾은 평면이거나, 기존에 찾은 평면보다 더 낮으면 갱신
                            if (!foundValidPlane || hit.pose.position.y < lowestPose.position.y)
                            {
                                lowestPose = hit.pose;
                                foundValidPlane = true;
                            }
                        }
                    }

                    // 2. 가장 낮은 바닥 평면을 찾았을 때만 생성 또는 이동 처리
                    if (foundValidPlane)
                    {
                        if (finger.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                        {
                            if (_activeModel == null)
                            {
                                LoadModelAndPlace(lowestPose.position, lowestPose.rotation);
                            }
                        }
                        else if (finger.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved && _activeModel != null)
                        {
                            _activeModel.transform.SetPositionAndRotation(lowestPose.position, lowestPose.rotation);
                        }
                    }
                }
            }
        }
        // --- [두 손가락: 스와이프 기반 Y축 회전] ---
        else if (touchCount == 2 && _activeModel != null)
        {
            Finger f1 = ETouch.activeFingers[0];
            Finger f2 = ETouch.activeFingers[1];

            // 1. 두 손가락 중 하나라도 막 닿았을 때 (기준점 잡기)
            if (f1.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began ||
                f2.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                _initialMidpointX = (f1.screenPosition.x + f2.screenPosition.x) / 2f;
                _initialRotation = _activeModel.transform.rotation;
            }
            // 2. 움직일 때 (기준점으로부터의 거리만큼만 회전)
            else if (f1.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                     f2.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                float currentMidpointX = (f1.screenPosition.x + f2.screenPosition.x) / 2f;
                float deltaX = currentMidpointX - _initialMidpointX;

                // Rotate 대신 회전값을 직접 셋팅해서 "누적 현상" 방지
                float angle = deltaX * _rotationSensitivity;
                _activeModel.transform.rotation = _initialRotation * Quaternion.Euler(0, -angle, 0);
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
        // 1. 모델 전체의 Renderer를 훑어서 현재 바운드(영역) 계산
        Bounds combinedBounds = new Bounds();
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
        }
        else return;

        // 2. 스케일 계산 (가장 긴 축 기준)
        // 입력값이 cm이므로 0.01을 곱해 미터(m) 단위로 변환
        float targetMax = Mathf.Max(CurrentModelDimension.width,
                                   CurrentModelDimension.height,
                                   CurrentModelDimension.length) * 0.01f; // 여기서 변환! 📏

        float currentMax = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
        float scaleFactor = (currentMax > 0) ? (targetMax / currentMax) : 1.0f;

        // 3. 자식 오브젝트(실제 모델)들을 루프 돌며 위치와 스케일 수정
        // root(parentObj)는 건드리지 않고, 내부 모델들만 조절해서 피벗 효과를 냄
        foreach (Transform child in root.transform)
        {
            // 스케일 적용
            child.localScale *= scaleFactor;

            // 피벗 보정: 모델의 바닥(min.y)이 부모의 0 위치에 오도록 offset 계산
            // 현재 로컬 좌표계에서 모델의 바닥이 얼마나 내려가 있는지 확인
            float bottomY = combinedBounds.min.y;
            float offset = root.transform.position.y - bottomY;

            // 모델을 위로 올려서 바닥에 맞춤
            child.position += new Vector3(0, offset, 0);
        }
    }
}