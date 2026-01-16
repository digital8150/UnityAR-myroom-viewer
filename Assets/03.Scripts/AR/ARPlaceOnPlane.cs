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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (arRaycastManager != null && infoText != null)
        {
            LogText("ARRaycast Manager Init.");
        }
    }

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

    public void OnLoadBtnClicked()
    {
        if (NativeFilePicker.IsFilePickerBusy())
        {
            LogText("파일 선택기가 바쁨");
            return;
        }

        LogText("파일 선택기 열기...");

        try
        {
            NativeFilePicker.PickFile((path) =>
            {
                if (path == null)
                {
                    LogText("파일 선택 취소함");
                }
                else if (!path.ToLower().EndsWith(".gltf") && !path.ToLower().EndsWith(".glb"))
                {
                    LogText("지원하지 않는 파일 형식: " + path);
                }
                else
                {
                    // glTFast는 로컬 경로 앞에 "file://" 붙여주는 게 안전함
                    currentModelPath = "file://" + path;
                    LogText("파일 선택됨: " + currentModelPath);

                    // (선택사항) 기존 모델이 있으면 지우기
                    if (activeModel != null) Destroy(activeModel);
                    activeModel = null;
                }
            });
        }
        catch (System.Exception e)
        {
            LogText("파일 선택기 오류: " + e.Message);
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