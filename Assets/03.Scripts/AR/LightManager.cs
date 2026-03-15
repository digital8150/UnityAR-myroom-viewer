using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class LightManager : MonoBehaviour
{
    [Header("AR Components")]
    [Tooltip("AR 카메라 매니저 연결")]
    [SerializeField] private ARCameraManager arCameraManager;

    [Tooltip("제어할 메인 조명 연결")]
    [SerializeField] private Light directionalLight;

    void OnEnable()
    {
        // 이벤트 구독: 카메라 프레임이 업데이트될 때마다 OnCameraFrameReceived 실행
        if (arCameraManager != null)
        {
            arCameraManager.frameReceived += OnCameraFrameReceived;
        }
    }

    void OnDisable()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (arCameraManager != null)
        {
            arCameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        // 1. 기본 밝기(Ambient Intensity) 반영
        if (args.lightEstimation.averageBrightness.HasValue)
        {
            directionalLight.intensity = args.lightEstimation.averageBrightness.Value;
        }

        // 2. 색온도/색상 보정(Color Correction) 반영
        if (args.lightEstimation.colorCorrection.HasValue)
        {
            directionalLight.color = args.lightEstimation.colorCorrection.Value;
        }

        // 3. 메인 조명 방향 반영 (그림자 방향이 현실과 일치하게 됨!)
        if (args.lightEstimation.mainLightDirection.HasValue)
        {
            directionalLight.transform.rotation = Quaternion.LookRotation(args.lightEstimation.mainLightDirection.Value);
        }

        // 4. (Environmental HDR 지원 기기용) 메인 조명 색상 직접 반영
        if (args.lightEstimation.mainLightColor.HasValue)
        {
            directionalLight.color = args.lightEstimation.mainLightColor.Value;
        }
    }
}