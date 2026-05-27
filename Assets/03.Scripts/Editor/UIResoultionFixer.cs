using UnityEngine;
using UnityEditor;

public class UIResolutionFixer : EditorWindow
{
    private Vector2 oldResolution = new Vector2(1920, 1080); // 기존 잘못 설정했던 해상도
    private Vector2 newResolution = new Vector2(2560, 1440); // 바꾸려는 올바른 해상도
    private Canvas targetCanvas;

    [MenuItem("Tools/UI Resolution Fixer")]
    public static void ShowWindow()
    {
        GetWindow<UIResolutionFixer>("UI Res Fixer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Reference Resolution 비율 보정기", EditorStyles.boldLabel);

        targetCanvas = (Canvas)EditorGUILayout.ObjectField("Target Canvas", targetCanvas, typeof(Canvas), true);
        oldResolution = EditorGUILayout.Vector2Field("Old Resolution", oldResolution);
        newResolution = EditorGUILayout.Vector2Field("New Resolution", newResolution);

        if (GUILayout.Button("보정 실행 (Fix RectTransforms)"))
        {
            if (targetCanvas != null)
            {
                FixResolution();
            }
            else
            {
                Debug.LogWarning("Canvas를 할당해주세요!");
            }
        }
    }

    private void FixResolution()
    {
        Vector2 ratio = new Vector2(newResolution.x / oldResolution.x, newResolution.y / oldResolution.y);
        RectTransform[] allRects = targetCanvas.GetComponentsInChildren<RectTransform>(true);

        // Undo(Ctrl+Z)를 위해 기록
        Undo.RecordObjects(allRects, "Fix UI Resolution");

        foreach (RectTransform rect in allRects)
        {
            // 캔버스 자체의 RectTransform은 건너뜀
            if (rect == targetCanvas.GetComponent<RectTransform>()) continue;

            // 앵커 포지션과 사이즈 델타에 비율을 곱해줌
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x * ratio.x, rect.anchoredPosition.y * ratio.y);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x * ratio.x, rect.sizeDelta.y * ratio.y);
        }

        Debug.Log("UI 보정이 완료되었습니다! 변경된 Canvas Scaler 해상도를 확인해보세요.");
    }
}