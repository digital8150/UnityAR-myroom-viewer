using TMPro;
using UnityEngine;

public class ARDebugView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;

    private void Update()
    {
        _text.text = $"" +
            $"[LightManager | AR Light Estimation]\n" +
            $"감지한 조명 색상 : {LightManager.LightColor}\n" +
            $"감지한 조명 강도 : {LightManager.LightIntensity}\n" +
            $"감지한 조명 각도 : {LightManager.LightRotationEuler}";
    }


}
