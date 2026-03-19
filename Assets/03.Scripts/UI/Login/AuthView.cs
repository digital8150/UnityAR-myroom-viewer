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
    [SerializeField] private Button loginBtn, toLoginBtn, toRegBtn, _emailCheckBtn;

    [Header("Wrong Indicator")]
    [SerializeField] private Color _defaultOutlineColor;
    [SerializeField] private Color _wrongOutlineColor;
    [SerializeField] private ProceduralImage _idFieldOutline;
    [SerializeField] private ProceduralImage _pwFieldOutline;
    [SerializeField] private GameObject _idWrongIndicator;
    [SerializeField] private GameObject _pwWrongIndicator;

    [Header("Register Wrong Text")]
    [SerializeField] private Color _wrongTextColor;
    [SerializeField] private Color _okayTextColor;
    [SerializeField] private TextMeshProUGUI _registerIndicatorText;
    [SerializeField] private string _emailExist;
    [SerializeField] private string _pwcIncorrect;
    [SerializeField] private string _emailOkay;


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
        if (_emailCheckBtn) _emailCheckBtn.onClick.AddListener(() => _presenter.OnEmailCheckClicked(regEmail.text));
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
        if (_emailCheckBtn) _emailCheckBtn.onClick.RemoveAllListeners();
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
        SetRegisterIndicatorTextEnabled(false);
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

    public void SetRegisterIndicatorTextEnabled(bool enabled)
    {
        if (_registerIndicatorText) _registerIndicatorText.gameObject.SetActive(enabled);
    }

    public void ShowEmailExist()
    {
        SetRegisterIndicatorTextEnabled(true);
        if (_registerIndicatorText)
        {
            _registerIndicatorText.text = _emailExist;
            _registerIndicatorText.color = _wrongTextColor;
        }
    }

    public void ShowPWCIncorrect()
    {
        SetRegisterIndicatorTextEnabled(true);
        if (_registerIndicatorText)
        {
            _registerIndicatorText.text = _pwcIncorrect;
            _registerIndicatorText.color = _wrongTextColor;
        }
    }

    public void ShowEmailOkay()
    {
        SetRegisterIndicatorTextEnabled(true);
        if (_registerIndicatorText) {
            _registerIndicatorText.text = _emailOkay;
            _registerIndicatorText.color = _okayTextColor;
        }
    }

    public void ShowRegisterIndicator(string text, bool isOkay)
    {
        SetRegisterIndicatorTextEnabled(true);
        if(_registerIndicatorText)
        {
            _registerIndicatorText.text = text;
            _registerIndicatorText.color = isOkay? _okayTextColor : _wrongTextColor;
        }
    }

    private void ResetWrongIndicator()
    {
        _idFieldOutline.color = _defaultOutlineColor;
        _pwFieldOutline.color = _defaultOutlineColor;
        _idWrongIndicator.SetActive(false);
        _pwWrongIndicator.SetActive(false);
    }
}