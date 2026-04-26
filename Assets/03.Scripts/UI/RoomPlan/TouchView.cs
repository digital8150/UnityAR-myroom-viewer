using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch; // 기존 Touch와 혼동 방지
using System;
using UnityEngine.EventSystems;

public class TouchView : MonoBehaviour
{
    public Action<Vector2> OnSingleDrag;
    public Action<Vector2, float> OnDoubleDrag;

    private void OnEnable()
    {
        // ETouch 활성화
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        // ETouch 비활성화
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        var activeTouches = ETouch.activeTouches;

        // 1) 한 손가락 터치 드래그
        if (activeTouches.Count == 1)
        {
            var touch = activeTouches[0];
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                // UI 영역 안에 있는지 체크 (선택 사항)
                if (IsPointerOverUI(touch.screenPosition))
                {
                    OnSingleDrag?.Invoke(touch.delta);
                }
            }
        }
        // 2) 두 손가락 터치 드래그 (중심점 이동)
        else if (activeTouches.Count >= 2)
        {
            var t1 = activeTouches[0];
            var t2 = activeTouches[1];

            if (t1.phase == UnityEngine.InputSystem.TouchPhase.Moved || t2.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                // 두 손가락의 중심점 계산
                Vector2 currentCenter = (t1.screenPosition + t2.screenPosition) * 0.5f;
                Vector2 previousCenter = ((t1.screenPosition - t1.delta) + (t2.screenPosition - t2.delta)) * 0.5f;
                Vector2 centerDelta = currentCenter - previousCenter;

                // X축 변화량을 회전값으로 전달
                OnDoubleDrag?.Invoke(centerDelta, centerDelta.x);
            }
        }
    }

    // 특정 좌표가 RawImage 영역 내부인지 판별하는 헬퍼 함수
    private bool IsPointerOverUI(Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            GetComponent<RectTransform>(), screenPos, null);
    }
}