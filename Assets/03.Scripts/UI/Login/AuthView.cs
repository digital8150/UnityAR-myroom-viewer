using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.UI.ProceduralImage;

public class AuthView : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject loadingBlockPanel;


    [Header("Inputs")]
    [SerializeField] private TMP_InputField regName;
    [SerializeField] private TMP_InputField regEmail, regPass, regPassConfirm;
    [SerializeField] private TMP_InputField loginEmail, loginPass;

    [Header("Buttons")]
    [SerializeField] private Button regBtn;
    [SerializeField] private Button loginBtn, toLoginBtn, toRegBtn;

    [Header("Wrong Indicator")]
    [SerializeField] private Color _defaultOutlineColor;
    [SerializeField] private Color _wrongOutlineColor;
    [SerializeField] private ProceduralImage _idFieldOutline;
    [SerializeField] private ProceduralImage _pwFieldOutline;
    [SerializeField] private GameObject _idWrongIndicator;
    [SerializeField] private GameObject _pwWrongIndicator;


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
    }

    //--- Public Methods ---//

    public void SetLoading(bool isLoading)
    {
        loadingBlockPanel?.SetActive(isLoading);
    }

    public void ShowLoginPanel()
    {
        ResetWrongIndicator();
        registerPanel?.SetActive(false);
        loginPanel?.SetActive(true);
    }

    public void ShowRegisterPanel() {
        registerPanel?.SetActive(true);
        loginPanel?.SetActive(false);
    }

    public void ShowIDWrongIndicator()
    {
        ResetWrongIndicator();
        _idFieldOutline.color = _wrongOutlineColor;
        _idWrongIndicator.SetActive(true);
    }

    public void ShowPWWrongIndicator()
    {
        ResetWrongIndicator();
        _pwFieldOutline.color = _wrongOutlineColor;
        _pwWrongIndicator.SetActive(true);
    }

    private void ResetWrongIndicator()
    {
        _idFieldOutline.color = _defaultOutlineColor;
        _pwFieldOutline.color = _defaultOutlineColor;
        _idWrongIndicator.SetActive(false);
        _pwWrongIndicator.SetActive(false);
    }    
}