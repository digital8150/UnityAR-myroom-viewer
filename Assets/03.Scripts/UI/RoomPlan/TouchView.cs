using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using System;
using UnityEngine.EventSystems;

public class TouchView : MonoBehaviour
{
    public Action<Vector2> OnSingleDrag;
    public Action<Vector2, float> OnDoubleDrag;
    public Action<Vector2> OnTap;

    private const float TAP_MAX_MOVE_PX = 12f;

    private bool _isTapping = false;
    private Vector2 _tapStartPos;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        _isTapping = false;
    }

    private void Update()
    {
        var activeTouches = ETouch.activeTouches;

        if (activeTouches.Count == 1)
        {
            var touch = activeTouches[0];
            bool overViewport = IsPointerOverViewport(touch.screenPosition);

            switch (touch.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    if (overViewport)
                    {
                        _isTapping = true;
                        _tapStartPos = touch.screenPosition;
                    }
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                    if (_isTapping && Vector2.Distance(touch.screenPosition, _tapStartPos) > TAP_MAX_MOVE_PX)
                        _isTapping = false;

                    if (overViewport)
                        OnSingleDrag?.Invoke(touch.delta);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    // 손가락이 움직이지 않는 동안 탭 유지
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                    if (_isTapping && overViewport)
                    {
                        Debug.Log($"[TouchView] OnTap fired at {touch.screenPosition}");
                        OnTap?.Invoke(touch.screenPosition);
                    }
                    _isTapping = false;
                    break;

                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    _isTapping = false;
                    break;
            }
        }
        else if (activeTouches.Count >= 2)
        {
            _isTapping = false;

            var t1 = activeTouches[0];
            var t2 = activeTouches[1];

            if (t1.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                t2.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                Vector2 currentCenter = (t1.screenPosition + t2.screenPosition) * 0.5f;
                Vector2 previousCenter = ((t1.screenPosition - t1.delta) + (t2.screenPosition - t2.delta)) * 0.5f;
                Vector2 centerDelta = currentCenter - previousCenter;

                OnDoubleDrag?.Invoke(centerDelta, centerDelta.x);
            }
        }
    }

    private bool IsPointerOverViewport(Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            GetComponent<RectTransform>(), screenPos, null);
    }
}