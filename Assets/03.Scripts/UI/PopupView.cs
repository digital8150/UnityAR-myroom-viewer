using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IPopupView
{
    void ShowMessage(string msg);
    void CloseMessage();
}

public class PopupView : MonoBehaviour, IPopupView
{
    public static PopupView Instance { get; private set; }

    [Header("Pannels")]
    [SerializeField] private GameObject _popupPanel, _okPanel, _yesNoPanel;

    [Header("Texts")]
    [SerializeField] private TMP_Text _popupMsg, _popupMsgYesNo;

    [Header("Buttons")]
    [SerializeField] private Button _closeMsgBtn, _yesBtn, _noBtn;

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

    private void OnDestroy()
    {
        _closeMsgBtn?.onClick.RemoveAllListeners();
        _yesBtn?.onClick.RemoveAllListeners();
        _noBtn?.onClick.RemoveAllListeners();
    }

    public void ShowMessage(string msg)
    {
        _popupPanel?.SetActive(true);
        _okPanel?.SetActive(true);
        if (_popupMsg != null)
        {
            _popupMsg.text = msg;
        }
    }

    public void CloseMessage()
    {
        _popupPanel?.SetActive(false);
        _okPanel?.SetActive(false);
        _yesNoPanel?.SetActive(false);
    }

    public void ShowYesNoMessage(string msg)
    {
        _popupPanel?.SetActive(true);
        _yesNoPanel?.SetActive(true);
        if (_popupMsgYesNo != null)
        {
            _popupMsgYesNo.text = msg;
        }
    }
}
