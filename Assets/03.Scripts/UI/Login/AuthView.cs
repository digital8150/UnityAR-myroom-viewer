using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AuthView : MonoBehaviour, IAuthView
{
    [Header("Panels")]
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject loadingBlockPanel;
    [SerializeField] private GameObject popupPanel;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField regName, regEmail, regPass, regPassConfirm;
    [SerializeField] private TMP_InputField loginEmail, loginPass;

    [Header("Buttons")]
    [SerializeField] private Button regBtn, loginBtn, toLoginBtn, toRegBtn, closeMessage;

    [Header("Popup")]
    [SerializeField] private TMP_Text popupMsg;

    public string UserName => regName.text;
    public string Email => loginPanel.activeSelf ? loginEmail.text : regEmail.text;
    public string Password => loginPanel.activeSelf ? loginPass.text : regPass.text;
    public string PasswordConfirm => regPassConfirm.text;

    private AuthPresenter _presenter;

    //--- Unity Lifecycle ---// 
    void Awake()
    {
        _presenter = new AuthPresenter(this, new AuthService());
        regBtn?.onClick.AddListener(_presenter.OnRegisterClicked);
        loginBtn?.onClick.AddListener(_presenter.OnLoginClicked);
        toLoginBtn?.onClick.AddListener(_presenter.OnToLoginClicked);
        toRegBtn?.onClick.AddListener(_presenter.OnToRegisterClicked);
        closeMessage?.onClick.AddListener(_presenter.OnCloseMessageClicked);
    }

    void Start()
    {
        ShowLoginPanel();
    }

    private void OnDestroy()
    {
        regBtn?.onClick.RemoveListener(_presenter.OnRegisterClicked);
        loginBtn?.onClick.RemoveListener(_presenter.OnLoginClicked);
        toLoginBtn?.onClick.RemoveListener(_presenter.OnToLoginClicked);
        toRegBtn?.onClick.RemoveListener(_presenter.OnToRegisterClicked);
        closeMessage?.onClick.RemoveListener(_presenter.OnCloseMessageClicked);
    }

    //--- Public Methods ---//
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

    public void SetLoading(bool isLoading)
    {
        loadingBlockPanel?.SetActive(isLoading);
    }

    public void ShowLoginPanel()
    { 
        registerPanel?.SetActive(false);
        loginPanel?.SetActive(true);
    }

    public void ShowRegisterPanel() {
        registerPanel?.SetActive(true);
        loginPanel?.SetActive(false);
    }
}