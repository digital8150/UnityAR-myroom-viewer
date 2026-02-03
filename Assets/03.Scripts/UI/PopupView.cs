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

    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_Text popupMsg;
    [SerializeField] private Button closeMsgBtn;

    private PopupPresenter _presenter;

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
        closeMsgBtn?.onClick.AddListener(_presenter.OnCloseMessageClicked);
    }

    private void OnDestroy()
    {
        closeMsgBtn?.onClick.RemoveAllListeners();
    }

    public void ShowMessage(string msg)
    {
        popupPanel?.SetActive(true);
        if (popupMsg != null)
        {
            popupMsg.text = msg;
        }
    }

    public void CloseMessage()
    {
        popupPanel?.SetActive(false);
    }
}
