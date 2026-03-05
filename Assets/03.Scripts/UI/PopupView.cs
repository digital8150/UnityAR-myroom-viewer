using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

public class PopupContext
{
    public string Content;
    public Sprite Icon;
    public Color BackgroundColor;

    public PopupContext(string content, Sprite icon, Color backgroundColor)
    {
        Content = content;
        Icon = icon;
        BackgroundColor = backgroundColor;
    }

    public PopupContext(string content, Sprite icon = null)
    {
        Content = content;
        Icon = icon;
        BackgroundColor = new Color(0xDF, 0xDE, 0xEA, 0xFF);
    }

    public PopupContext()
    {
        Content = "";
        Icon = null;
        BackgroundColor = new Color(0xDF, 0xDE, 0xEA, 0xFF);
    }
}


public class PopupView : MonoBehaviour
{
    public static PopupView Instance { get; private set; }
    private static Queue<PopupContext> _popupQueue = new Queue<PopupContext>();


    [Header("Pannels")]
    [SerializeField] private GameObject _backgroundBlockerPanel;
    [SerializeField] private GameObject _popupPanel;
    [SerializeField] private GameObject _okPanel;
    [SerializeField] private GameObject _yesNoPanel;

    [Header("Texts")]
    [SerializeField] private TMP_Text _popupMsg;
    [SerializeField] private TMP_Text _popupMsgYesNo;

    [Header("Buttons")]
    [SerializeField] private Button _closeMsgBtn;
    [SerializeField] private Button _yesBtn;
    [SerializeField] private Button _noBtn;

    [Header("Notification")]
    [SerializeField] private ProceduralImage _notificationBackground;
    [SerializeField] private TextMeshProUGUI _notificationText;
    [SerializeField] private Image _notificationIcon;
    [SerializeField] private RectTransform _notificationRect;

    [Header("Position Settings")]
    [SerializeField]
    private float _hiddenPosY = 100;
    [SerializeField]
    private float _shownPosY = -8;

    [Header("Timing Settings")]
    [SerializeField]
    private float _showDuration = 2.0f;
    [SerializeField]
    private float _animDuration = 0.3f;

    [Header("Notification Common Icons")]
    public Sprite GreenCheckCircle;

    private static bool _isShowingPopup = false;

    private PopupPresenter _presenter;
    public PopupPresenter Presenter => _presenter;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        _presenter = new PopupPresenter(this);
        _closeMsgBtn?.onClick.AddListener(_presenter.OnCloseMessageClicked);
        _yesBtn?.onClick.AddListener(_presenter.OnYesClicked);
        _noBtn?.onClick.AddListener(_presenter.OnNoClicked);

    }

    private void Start()
    {
        CloseMessage();
        ShowPopupAt(100, new PopupContext());
        WebsocketController.Instance.OnModel3DGenerated += GenerationSuccess;
        WebsocketController.Instance.OnModel3DGenerateFailed += GenerationFailed;

    }

    private void Update()
    {
        if (_popupQueue.Count > 0 && !_isShowingPopup)
        {
            StartCoroutine(ShowPopupRoutine());
        }
    }

    private void OnDestroy()
    {
        _closeMsgBtn?.onClick.RemoveAllListeners();
        _yesBtn?.onClick.RemoveAllListeners();
        _noBtn?.onClick.RemoveAllListeners();
    }

    public void ShowMessage(string msg)
    {
        if(_backgroundBlockerPanel || _popupPanel || _okPanel || _popupMsg )
        {
            _backgroundBlockerPanel.SetActive(true);
            _popupPanel.SetActive(true);
            _okPanel.SetActive(true);
            _popupMsg.text = msg;
            return;
        }
        Debug.LogError("PopupView: ShowMessage - One or more UI components are not assigned in the inspector.");
    }

    public void CloseMessage()
    {
        if (_popupPanel || _okPanel || _yesNoPanel || _backgroundBlockerPanel)
        {
            _popupPanel.SetActive(false);
            _okPanel.SetActive(false);
            _yesNoPanel.SetActive(false);
            _backgroundBlockerPanel.SetActive(false);
            return;
        }
        Debug.LogError("PopupView: CloseMessage - One or more UI components are not assigned in the inspector.");
    }

    public void ShowYesNoMessage(string msg)
    {
        if (_backgroundBlockerPanel || _popupPanel || _yesNoPanel || _popupMsgYesNo)
        {
            _backgroundBlockerPanel.SetActive(true);
            _popupPanel.SetActive(true);
            _yesNoPanel.SetActive(true);
            _popupMsgYesNo.text = msg;
            return;
        }
        Debug.LogError("PopupView: ShowYesNoMessage - One or more UI components are not assigned in the inspector.");
    }

    /// <summary>
    /// 플레이어 HUD에 표출할 알림을 등록합니다.
    /// </summary>
    /// <param name="context"></param>
    public static void AddPopup(PopupContext context)
    {
        if (context == null)
        {
            return;
        }
        _popupQueue.Enqueue(context);
    }

    /// <summary>
    /// 플레이어 HUD에 표출할 알림을 등록합니다.
    /// </summary>
    /// <param name="content">표출할 내용입니다.</param>
    /// <param name="icon">표출할 아이콘입니다.</param>
    /// <param name="backgroundColor">알림 팝업의 배경 색상입니다.</param>
    public static void AddPopup(string content, Sprite icon, Color backgroundColor)
    {
        var context = new PopupContext(content, icon, backgroundColor);
        AddPopup(context);
    }

    private IEnumerator MovePopup(float start, float end, float animDuration)
    {
        if (!_notificationRect)
        {
            yield break;
        }

        Vector2 startVector = new Vector2(_notificationRect.anchoredPosition.x, start);
        Vector2 endVector = new Vector2(_notificationRect.anchoredPosition.x, end);

        float elapsedTime = 0f;
        while (elapsedTime < animDuration)
        {
            if (!_notificationRect)
            {
                yield break;
            }
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / animDuration);
            float curveT = t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
            _notificationRect.anchoredPosition = Vector2.Lerp(startVector, endVector, curveT);
            yield return null;
        }

        if (!_notificationRect)
        {
            yield break;
        }
        _notificationRect.anchoredPosition = endVector;
    }

    private void ShowPopupAt(float posY, PopupContext context)
    {
        if (context == null || !_notificationText || !_notificationBackground || !_notificationIcon || !_notificationRect)
        {
            return;
        }

        _notificationText.text = context.Content;
        _notificationBackground.color = context.BackgroundColor;
        _notificationIcon.sprite = context.Icon;
        _notificationRect.anchoredPosition = new Vector2(_notificationRect.anchoredPosition.x, posY);
    }

    private IEnumerator ShowPopupRoutine()
    {
        _isShowingPopup = true;

        // 1단계: 등장 (Slide Down)
        PopupContext currentContext = _popupQueue.Dequeue();
        ShowPopupAt(_hiddenPosY, currentContext);
        yield return StartCoroutine(MovePopup(_hiddenPosY, _shownPosY, _animDuration));

        // 2단계: 대기
        yield return new WaitForSecondsRealtime(_showDuration);

        // 3단계: 퇴장 (Slide Up)
        yield return StartCoroutine(MovePopup(_shownPosY, _hiddenPosY, _animDuration));
        _isShowingPopup = false;
    }

    private async void GenerationSuccess(string msg)
    {
        try
        {
            ModelGenerationResponse wsData = JsonConvert.DeserializeObject<ModelGenerationResponse>(msg);
            string content = $"<b>모델 생성 완료</b>\n<size=95%>업로드하신 가구의 3D 모델이 정상적으로 생성되었어요.";
            AddPopup(new PopupContext(content, await Utils.ImageUtils.LoadSpriteFromUrl(wsData.thumbnailUrl)));

        }catch(Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private async void GenerationFailed(string msg)
    {
        try
        {
            ModelGenerationResponse wsData = JsonConvert.DeserializeObject<ModelGenerationResponse>(msg);
            string content = $"<b>모델 생성 실패</b>\n<size=95%>업로드 하신 가구의 3D 모델을 생성하지 못했습니다.";
            AddPopup(new PopupContext(content, await Utils.ImageUtils.LoadSpriteFromUrl(wsData.thumbnailUrl)));

        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
}
