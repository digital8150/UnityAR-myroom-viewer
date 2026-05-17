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
    public Action<float> OnPinch;

    private const float TAP_MAX_MOVE_PX = 12f;
    private const float TWO_FINGER_LOCK_THRESHOLD_PX = 16f;

    private bool _isTapping = false;
    private Vector2 _tapStartPos;
    private bool _singleOwned = false;

    private enum TwoFingerMode { None, Pinch, Drag }
    private TwoFingerMode _twoFingerMode = TwoFingerMode.None;
    private float _accumPinch = 0f;
    private Vector2 _accumCenter = Vector2.zero;
    private bool _twoFingerOwned = false;

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

            // 새 단일 터치 시퀀스가 시작될 때 소유 여부를 결정
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                bool blocked = IsPointerOverBlockingUI(touch.screenPosition);
                _singleOwned = overViewport && !blocked;
                Debug.Log($"[TouchView] Began pos={touch.screenPosition} overViewport={overViewport} blocked={blocked} owned={_singleOwned}");
            }

            switch (touch.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    if (_singleOwned)
                    {
                        _isTapping = true;
                        _tapStartPos = touch.screenPosition;
                    }
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                    if (!_singleOwned) break;

                    if (_isTapping && Vector2.Distance(touch.screenPosition, _tapStartPos) > TAP_MAX_MOVE_PX)
                        _isTapping = false;

                    OnSingleDrag?.Invoke(touch.delta);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    // 손가락이 움직이지 않는 동안 탭 유지
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                    if (_singleOwned && _isTapping && overViewport)
                    {
                        Debug.Log($"[TouchView] OnTap fired at {touch.screenPosition}");
                        OnTap?.Invoke(touch.screenPosition);
                    }
                    _isTapping = false;
                    _singleOwned = false;
                    break;

                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    _isTapping = false;
                    _singleOwned = false;
                    break;
            }
        }
        else if (activeTouches.Count >= 2)
        {
            _isTapping = false;

            var t1 = activeTouches[0];
            var t2 = activeTouches[1];

            // 두 손가락 시퀀스의 시작 시점에 소유 여부 결정(둘 다 뷰포트 위여야 함)
            if (_twoFingerMode == TwoFingerMode.None && _accumPinch == 0f && _accumCenter == Vector2.zero)
            {
                bool t1Over = IsPointerOverViewport(t1.screenPosition) && !IsPointerOverBlockingUI(t1.screenPosition);
                bool t2Over = IsPointerOverViewport(t2.screenPosition) && !IsPointerOverBlockingUI(t2.screenPosition);
                _twoFingerOwned = t1Over && t2Over;
            }

            if (!_twoFingerOwned) return;

            if (t1.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                t2.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                Vector2 currentCenter = (t1.screenPosition + t2.screenPosition) * 0.5f;
                Vector2 previousCenter = ((t1.screenPosition - t1.delta) + (t2.screenPosition - t2.delta)) * 0.5f;
                Vector2 centerDelta = currentCenter - previousCenter;

                Vector2 prevT1 = t1.screenPosition - t1.delta;
                Vector2 prevT2 = t2.screenPosition - t2.delta;
                float pinchDelta = Vector2.Distance(t1.screenPosition, t2.screenPosition)
                                 - Vector2.Distance(prevT1, prevT2);

                // 제스처가 시작될 때 의도(핀치 vs 드래그)를 판별해 한 모드로 잠금
                if (_twoFingerMode == TwoFingerMode.None)
                {
                    _accumPinch += pinchDelta;
                    _accumCenter += centerDelta;

                    float pinchScore = Mathf.Abs(_accumPinch);
                    float dragScore = _accumCenter.magnitude;

                    if (pinchScore >= TWO_FINGER_LOCK_THRESHOLD_PX && pinchScore > dragScore)
                        _twoFingerMode = TwoFingerMode.Pinch;
                    else if (dragScore >= TWO_FINGER_LOCK_THRESHOLD_PX && dragScore >= pinchScore)
                        _twoFingerMode = TwoFingerMode.Drag;
                }

                if (_twoFingerMode == TwoFingerMode.Pinch)
                    OnPinch?.Invoke(pinchDelta);
                else if (_twoFingerMode == TwoFingerMode.Drag)
                    OnDoubleDrag?.Invoke(centerDelta, centerDelta.x);
            }
        }
        else
        {
            _twoFingerMode = TwoFingerMode.None;
            _accumPinch = 0f;
            _accumCenter = Vector2.zero;
            _twoFingerOwned = false;
        }
    }

    private bool IsPointerOverViewport(Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            GetComponent<RectTransform>(), screenPos, null);
    }

    private static readonly System.Collections.Generic.List<RaycastResult> _raycastResults = new System.Collections.Generic.List<RaycastResult>();

    // ScrollRect 등 뷰포트를 가리고 있는 다른 UI 위에서 시작된 터치인지 검사
    private bool IsPointerOverBlockingUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        var pointer = new PointerEventData(EventSystem.current) { position = screenPos };
        _raycastResults.Clear();
        EventSystem.current.RaycastAll(pointer, _raycastResults);

        if (_raycastResults.Count == 0)
        {
            Debug.Log("[TouchView] Raycast: no hits → not blocked");
            return false;
        }

        var self = transform;
        var top = _raycastResults[0];
        var topT = top.gameObject != null ? top.gameObject.transform : null;

        // 디버그용 상위 히트 목록 덤프
        var sb = new System.Text.StringBuilder();
        sb.Append("[TouchView] Raycast hits: ");
        for (int i = 0; i < _raycastResults.Count && i < 5; i++)
            sb.Append(_raycastResults[i].gameObject != null ? _raycastResults[i].gameObject.name : "null").Append(" | ");
        Debug.Log(sb.ToString());

        if (topT == null) return false;

        // 최상단 히트가 self이거나 self의 자식/부모 계통이면 통과(=뷰포트 본인 또는 뷰포트 RawImage가 위/아래에 있는 케이스)
        if (topT == self) return false;
        if (topT.IsChildOf(self)) return false;
        if (self.IsChildOf(topT)) return false;

        Debug.Log($"[TouchView] Blocked by '{topT.name}' (self='{self.name}')");
        return true;
    }
}