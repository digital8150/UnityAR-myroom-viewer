using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class LightManager : MonoBehaviour
{
    [Header("AR Components")]
    [Tooltip("AR 카메라 매니저 연결")]
    [SerializeField] private ARCameraManager _arCameraManager;

    [Tooltip("제어할 메인 조명 연결")]
    [SerializeField] private Light _directionalLight;

    [Header("Settings")]
    [SerializeField] private float _lightIntensityMult = 1.5f;

    public static Color LightColor;
    public static float LightIntensity;
    public static Vector3 LightRotationEuler;

    void OnEnable()
    {
        // 이벤트 구독: 카메라 프레임이 업데이트될 때마다 OnCameraFrameReceived 실행
        if (_arCameraManager != null)
        {
            _arCameraManager.frameReceived += OnCameraFrameReceived;
        }
    }

    void OnDisable()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (_arCameraManager != null)
        {
            _arCameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        // 1. Ambient Intensity (주변 밝기)
        if (args.lightEstimation.averageBrightness.HasValue)
        {
            _directionalLight.intensity = Mathf.Clamp(args.lightEstimation.averageBrightness.Value * 2.0f, 0.5f, 2.0f);
        }

        // 2. Ambient Color (주변 색상 보정)
        if (args.lightEstimation.colorCorrection.HasValue)
        {
            _directionalLight.color = args.lightEstimation.colorCorrection.Value;
        }

        // 3. Main Light Direction (메인 조명 방향)
        if (args.lightEstimation.mainLightDirection.HasValue)
        {
            _directionalLight.transform.rotation = Quaternion.LookRotation(args.lightEstimation.mainLightDirection.Value);
        }

        // 4. Main Light Intensity & Color (안드로이드 HDR 조명 처리)
        if (args.lightEstimation.mainLightColor.HasValue)
        {
            Color hdrColor = args.lightEstimation.mainLightColor.Value;

            // RGB 값 중에서 가장 큰 값을 조명의 '밝기(Intensity)'로 추출!
            float extractedIntensity = Mathf.Max(hdrColor.r, Mathf.Max(hdrColor.g, hdrColor.b));

            if (extractedIntensity > 0f)
            {
                // 1. 색상 값은 0~1 사이(LDR)로 정규화해서 순수 색상만 세팅
                _directionalLight.color = new Color(hdrColor.r / extractedIntensity, hdrColor.g / extractedIntensity, hdrColor.b / extractedIntensity, 1f);

                // 2. 뽑아낸 가장 큰 값을 밝기에 적용 (너무 어둡거나 밝으면 뒤에 곱하기/나누기로 보정해 주면 됨)
                _directionalLight.intensity = extractedIntensity * _lightIntensityMult;
            }
        }

        if (args.lightEstimation.mainLightIntensityLumens.HasValue)
        {
            // mainLightIntensityMultiplier가 지원되는 기기면 기존 밝기를 덮어씌움!
            _directionalLight.intensity = args.lightEstimation.mainLightIntensityLumens.Value;
        }

        LightColor = _directionalLight.color;
        LightIntensity = _directionalLight.intensity;
        LightRotationEuler = _directionalLight.transform.rotation.eulerAngles;
        
    }
}