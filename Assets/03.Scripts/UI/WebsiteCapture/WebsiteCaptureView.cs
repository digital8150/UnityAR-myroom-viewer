using System;
using System.Reflection;
using Gree.UnityWebView;
using UnityEngine;
using UnityEngine.UI;

public class WebsiteCaptureView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button _captureButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private RectTransform _webViewArea;     // gree WebViewObject 의 마진 계산용
    [SerializeField] private GameObject _loadingIndicator;

    [Header("Capture Settings")]
    [SerializeField] private int _jpegQuality = 85;

    private WebViewObject _webViewObject;
    private Action<byte[]> _onResult;
    private bool _resolved;

    public void Open(string url, Action<byte[]> onResult)
    {
        _onResult = onResult;
        _resolved = false;

        if (_captureButton) _captureButton.onClick.AddListener(OnCaptureClicked);
        if (_closeButton) _closeButton.onClick.AddListener(OnCloseClicked);

        InitWebView(url);
    }

    private void InitWebView(string url)
    {
        var go = new GameObject("WebViewObject");
        go.transform.SetParent(transform, false);
        _webViewObject = go.AddComponent<WebViewObject>();

        _webViewObject.Init(
            cb: (msg) => { /* page->unity msg */ },
            err: (msg) => Debug.LogError($"[WebsiteCaptureView] WebView error: {msg}"),
            httpErr: (msg) => Debug.LogError($"[WebsiteCaptureView] WebView http error: {msg}"),
            started: (msg) => { if (_loadingIndicator) _loadingIndicator.SetActive(true); },
            ld: (msg) => { if (_loadingIndicator) _loadingIndicator.SetActive(false); }
        );

        ApplyMarginsFromRect();
        _webViewObject.SetVisibility(true);
        _webViewObject.LoadURL(url);
    }

    private void ApplyMarginsFromRect()
    {
        if (_webViewObject == null) return;

        int left = 0, top = 0, right = 0, bottom = 0;

        if (_webViewArea != null)
        {
            Vector3[] corners = new Vector3[4];
            _webViewArea.GetWorldCorners(corners);
            // corners: 0=BL, 1=TL, 2=TR, 3=BR (screen space when canvas is overlay)
            float blX = corners[0].x, blY = corners[0].y;
            float trX = corners[2].x, trY = corners[2].y;

            left = Mathf.RoundToInt(blX);
            right = Mathf.RoundToInt(Screen.width - trX);
            top = Mathf.RoundToInt(Screen.height - trY);
            bottom = Mathf.RoundToInt(blY);
        }
        _webViewObject.SetMargins(left, top, right, bottom);
    }

    private void OnCaptureClicked()
    {
        if (_resolved) return;
        byte[] bytes = CaptureWebView();
        if (bytes == null || bytes.Length == 0)
        {
            Debug.LogError("[WebsiteCaptureView] capture returned empty bytes");
            return;
        }
        Resolve(bytes);
    }

    private void OnCloseClicked()
    {
        if (_resolved) return;
        Resolve(null);
    }

    private void Resolve(byte[] bytes)
    {
        _resolved = true;
        var cb = _onResult;
        _onResult = null;
        try { cb?.Invoke(bytes); }
        finally { Destroy(gameObject); }
    }

    private byte[] CaptureWebView()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return CaptureAndroid();
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        return CaptureWindows();
#else
        Debug.LogWarning("[WebsiteCaptureView] WebView capture is not implemented on this platform.");
        return null;
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private byte[] CaptureAndroid()
    {
        try
        {
            // gree WebViewObject 내부의 Java plugin 객체 ('webView' 필드, AndroidJavaObject)
            var field = typeof(WebViewObject).GetField(
                "webView",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                Debug.LogError("[WebsiteCaptureView] gree WebViewObject 'webView' field not found");
                return null;
            }
            var pluginObj = field.GetValue(_webViewObject) as AndroidJavaObject;
            if (pluginObj == null)
            {
                Debug.LogError("[WebsiteCaptureView] gree plugin AndroidJavaObject is null");
                return null;
            }

            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var captureCls = new AndroidJavaClass("com.myroom.webviewcapture.WebViewCapture"))
            {
                return captureCls.CallStatic<byte[]>("capture", activity, pluginObj, _jpegQuality);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebsiteCaptureView] CaptureAndroid failed: {e}");
            return null;
        }
    }
#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private byte[] CaptureWindows()
    {
        try
        {
            // Windows 백엔드는 gree 내부에서 'texture' 필드(Texture2D)에 WebView를 렌더링한다.
            var field = typeof(WebViewObject).GetField(
                "texture",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                Debug.LogError("[WebsiteCaptureView] gree WebViewObject 'texture' field not found");
                return null;
            }
            var tex = field.GetValue(_webViewObject) as Texture2D;
            if (tex == null)
            {
                Debug.LogError("[WebsiteCaptureView] gree texture is null (WebView가 아직 준비되지 않았을 수 있음)");
                return null;
            }
            return tex.EncodeToJPG(_jpegQuality);
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebsiteCaptureView] CaptureWindows failed: {e}");
            return null;
        }
    }
#endif

    private void OnDestroy()
    {
        if (_captureButton) _captureButton.onClick.RemoveListener(OnCaptureClicked);
        if (_closeButton) _closeButton.onClick.RemoveListener(OnCloseClicked);

        if (_webViewObject != null)
        {
            _webViewObject.SetVisibility(false);
            Destroy(_webViewObject.gameObject);
        }

        if (!_resolved)
        {
            // 비정상 종료 시(외부 Destroy 등) 콜백 누락 방지
            _resolved = true;
            try { _onResult?.Invoke(null); } catch { /* swallow */ }
            _onResult = null;
        }
    }
}
