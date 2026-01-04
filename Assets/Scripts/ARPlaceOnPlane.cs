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
    [SerializeField]
    private ARRaycastManager arRaycastManager;

    [SerializeField]
    private Text infoText;

    public string currentModelPath;
    private GameObject activeModel; // 씬에 배치된 모델
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(arRaycastManager != null && infoText != null)
        {
            LogText("ARRaycast Manager Init.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (string.IsNullOrEmpty(currentModelPath)) return;

        if(ETouch.activeTouches.Count == 0) return;

        Finger finger = ETouch.activeFingers[0];

        LogText("화면 터치 감지됨, 위치 확인 중...");
        if (arRaycastManager.Raycast(finger.currentTouch.screenPosition, hits, TrackableType.Planes))
        {
            Pose hitPose = hits[0].pose;
            if (activeModel == null)
            {
                LoadModelAndPlace(hitPose.position, hitPose.rotation);
            }
            else
            {

                activeModel.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
            }
        }
        else
        {
            LogText("평면을 찾지 못함");
        }

    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }


    public void OnLoadBtnClicked()
    {
        if(NativeFilePicker.IsFilePickerBusy())
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
                else if(!path.ToLower().EndsWith(".gltf") && !path.ToLower().EndsWith(".glb"))
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
        LogText("glb 런타임 로드 중...");

        // 빈 오브젝트 생성
        GameObject parentObj = new GameObject("AR_Model_Instance");
        parentObj.transform.position = position;
        parentObj.transform.rotation = rotation;

        var gltf = new GltfImport();

        // 로컬 파일 로드
        bool success = await gltf.Load(currentModelPath);

        if (success)
        {
            await gltf.InstantiateMainSceneAsync(parentObj.transform);
            activeModel = parentObj;
            LogText("모델 배치 완료");
        }
        else
        {
            LogText("모델 로드 실패");
            Destroy(parentObj);
        }
    }

    private void LogText(string content)
    {
#if UNITY_EDITOR
        Debug.Log(content); 
#endif
        if (infoText != null)
        {
            infoText.text = content;
        }
    }
}
