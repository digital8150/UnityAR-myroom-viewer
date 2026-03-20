using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.UI.ProceduralImage;

public class AuthView : MonoBehaviour
{
    [Header("1. Shared & Panels")] // 공통 패널 및 연출용 요소
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject registerCompletedPanel;
    [SerializeField] private GameObject loadingBlockPanel;

    [Header("2. Login Page Elements")] // 로그인 페이지 관련
    [SerializeField] private TMP_InputField loginEmail;
    [SerializeField] private TMP_InputField loginPass;
    [SerializeField] private Button loginBtn;
    [SerializeField] private Button toRegBtn; 
    [SerializeField] private Toggle _autoLoginToggle;
    [Space(5)]
    [SerializeField] private ProceduralImage _idFieldOutline;
    [SerializeField] private ProceduralImage _pwFieldOutline;
    [SerializeField] private GameObject _idWrongIndicator;
    [SerializeField] private GameObject _pwWrongIndicator;
    [SerializeField] private Color _defaultOutlineColor;
    [SerializeField] private Color _wrongOutlineColor;

    [Header("3. Register Page Elements")] // 회원가입 페이지 관련
    [SerializeField] private TMP_InputField regName;
    [SerializeField] private TMP_InputField regEmail;
    [SerializeField] private TMP_InputField regPass;
    [SerializeField] private TMP_InputField regPassConfirm;
    [SerializeField] private Button _emailCheckBtn;
    [SerializeField] private Button regBtn;
    [SerializeField] private Button toLoginBtn; // 로그인 창으로 이동
    [Space(5)]
    [SerializeField] private TextMeshProUGUI _registerIndicatorText;
    [SerializeField] private Color _wrongTextColor;
    [SerializeField] private Color _okayTextColor;
    [SerializeField] private string _emailExist;
    [SerializeField] private string _pwcIncorrect;
    [SerializeField] private string _emailOkay;

    [Header("4. Register Completed Page Elements")] // 완료 페이지 관련
    [SerializeField] private Button toLoginBtn2; // 완료 후 로그인 창으로 이동

    public string UserName => regName.text;
    public string Email => loginPanel.activeSelf ? loginEmail.text : regEmail.text;
    public string Password => loginPanel.activeSelf ? loginPass.text : regPass.text;
    public string PasswordConfirm => regPassConfirm.text;

    private AuthPresenter _presenter;

    //--- Unity Lifecycle ---// 
    void Awake()
    {
        _presenter = new AuthPresenter(this);
        if(regBtn) regBtn.onClick.AddListener(_presenter.OnRegisterClicked);
        if(loginBtn) loginBtn.onClick.AddListener(_presenter.OnLoginClicked);
        if(toLoginBtn) toLoginBtn.onClick.AddListener(_presenter.OnToLoginClicked);
        if(toRegBtn) toRegBtn.onClick.AddListener(_presenter.OnToRegisterClicked);
        if (_emailCheckBtn) _emailCheckBtn.onClick.AddListener(() => _presenter.OnEmailCheckClicked(regEmail.text));
        if (toLoginBtn2) toLoginBtn2.onClick.AddListener(_presenter.OnToLoginClicked);
    }

    void Start()
    {
        ShowLoginPanel();
        _presenter.TryLogonWithRefreshToken();
    }

    private void OnDestroy()
    {
        if(regBtn) regBtn.onClick.RemoveListener(_presenter.OnRegisterClicked);
        if(loginBtn) loginBtn.onClick.RemoveListener(_presenter.OnLoginClicked);
        if(toLoginBtn) toLoginBtn.onClick.RemoveListener(_presenter.OnToLoginClicked);
        if(toRegBtn) toRegBtn.onClick.RemoveListener(_presenter.OnToRegisterClicked);
        if(_emailCheckBtn) _emailCheckBtn.onClick.RemoveAllListeners();
        if(toLoginBtn2) toLoginBtn2.onClick.RemoveAllListeners();
    }

    //--- Public Methods ---//

    public void SetLoading(bool isLoading)
    {
        loadingBlockPanel?.SetActive(isLoading);
    }

    public void ShowLoginPanel()
    {
        ResetWrongIndicator();
        HideAllPanels();
        if(loginPanel) loginPanel.SetActive(true);
    }

    public void ShowRegisterPanel() {
        SetRegisterIndicatorTextEnabled(false);
        HideAllPanels();
        if(registerPanel) registerPanel.SetActive(true);
    }

    public void ShowRegisterCompletedPanel()
    {
        HideAllPanels();
        if(registerCompletedPanel) registerCompletedPanel.SetActive(true);
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
        if (_registerIndicatorText && !enabled) _registerIndicatorText.text = string.Empty;
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

    public bool GetAutoLoginToggle()
    {
        if (_autoLoginToggle) return _autoLoginToggle.isOn;
        return false;
    }

    private void ResetWrongIndicator()
    {
        _idFieldOutline.color = _defaultOutlineColor;
        _pwFieldOutline.color = _defaultOutlineColor;
        _idWrongIndicator.SetActive(false);
        _pwWrongIndicator.SetActive(false);
    }

    private void HideAllPanels()
    {
        if (loginPanel) loginPanel.SetActive(false);
        if (registerPanel) registerPanel.SetActive(false);
        if (registerCompletedPanel) registerCompletedPanel.SetActive(false);
    }
}