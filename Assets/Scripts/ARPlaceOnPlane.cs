using GLTFast;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ARPlaceOnPlane : MonoBehaviour
{
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private Text infoText;

    public string currentModelPath;
    private GameObject activeModel;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // 로딩 중 중복 터치 및 중복 생성 방지
    private bool isModelLoading = false;

    void OnEnable() { EnhancedTouchSupport.Enable(); }
    void OnDisable() { EnhancedTouchSupport.Disable(); }

    void Update()
    {
        // 1. 경로가 없거나 현재 로딩 중이면 터치 무시
        if (string.IsNullOrEmpty(currentModelPath) || isModelLoading) return;

        if (ETouch.activeFingers.Count == 0) return;

        Finger finger = ETouch.activeFingers[0];

        // 2. 터치를 시작했을 때만 레이캐스트 시도
        if (finger.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            if (arRaycastManager.Raycast(finger.currentTouch.screenPosition, hits, TrackableType.Planes))
            {
                Pose hitPose = hits[0].pose;

                if (activeModel == null)
                {
                    LoadModelAndPlace(hitPose.position, hitPose.rotation);
                }
                else
                {
                    LogText("모델 위치 이동");
                    activeModel.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
                }
            }
        }
    }

    private async void LoadModelAndPlace(Vector3 position, Quaternion rotation)
    {
        isModelLoading = true; // 로딩 시작 플래그 고정
        LogText("모델 데이터를 불러오는 중...");

        GameObject parentObj = new GameObject("AR_Model_Instance");
        parentObj.transform.position = position;
        parentObj.transform.rotation = rotation;

        var gltf = new GltfImport();

        // glTFast 로드 시 Built-in 전용 셰이더가 사용되도록 처리됨
        bool success = await gltf.Load(currentModelPath);

        if (success)
        {
            // 인스턴스화 완료까지 대기
            bool instantSuccess = await gltf.InstantiateMainSceneAsync(parentObj.transform);
            if (instantSuccess)
            {
                activeModel = parentObj;
                LogText("모델 배치 완료");
            }
            else
            {
                LogText("인스턴스 생성 실패");
                Destroy(parentObj);
            }
        }
        else
        {
            LogText("GLB 로드 실패");
            Destroy(parentObj);
        }

        isModelLoading = false; // 로딩 해제
    }

    private void LogText(string content)
    {
        Debug.Log(content);
        if (infoText != null) infoText.text = content;
    }
}