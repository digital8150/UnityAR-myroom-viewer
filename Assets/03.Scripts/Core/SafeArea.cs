using UnityEngine;

public class SafeArea : MonoBehaviour
{
    RectTransform rectTransform;
    Rect safeArea;
    Vector2 minAnchor;
    Vector2 maxAnchor;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        safeArea = Screen.safeArea;

        if (safeArea != Rect.zero)
        {
            // 앵커를 안전 영역(Safe Area) 비율에 맞춰서 조정
            minAnchor = safeArea.position;
            maxAnchor = minAnchor + safeArea.size;

            minAnchor.x /= Screen.width;
            minAnchor.y /= Screen.height;
            maxAnchor.x /= Screen.width;
            maxAnchor.y /= Screen.height;

            rectTransform.anchorMin = minAnchor;
            rectTransform.anchorMax = maxAnchor;
        }
    }
}