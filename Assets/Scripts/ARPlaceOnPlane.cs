using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using GLTFast;



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
        
    }

    // Update is called once per frame
    void Update()
    {
        if (string.IsNullOrEmpty(currentModelPath)) return;

        if (Input.touchCount == 0) return;
        Touch touch = Input.GetTouch(0);

        if(touch.phase == TouchPhase.Began)
        {
            if(arRaycastManager.Raycast(touch.position, hits, TrackableType.Planes))
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
        }

    }


    public void OnLoadBtnClicked()
    {
        string[] fileTypes = new string[] { "glb", "gltf" };
        NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
            {
                LogText("파일 선택 취소함");
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
        }, fileTypes);
    }

    private async void LoadModelAndPlace(Vector3 position, Quaternion rotation)
    {
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
