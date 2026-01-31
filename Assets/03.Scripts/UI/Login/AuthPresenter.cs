using UnityEngine;
using UnityEngine.SceneManagement;

public class AuthPresenter
{
    private readonly IAuthView _view;
    private readonly AuthService _service;

    public AuthPresenter(IAuthView view, AuthService service)
    {
        _view = view;
        _service = service;
    }

    public async void OnLoginClicked()
    {
        var loginData = new LoginRequest
        {
            email = _view.Email,
            password = _view.Password
        };

        _view.SetLoading(true);
        var (code, token) = await _service.Login(loginData);
        _view.SetLoading(false);

        if (code == 200)
        {
            JWTToken.Token = token;
            SceneManager.LoadScene("Home");
        }
        else
        {
            _view.ShowMessage($"아이디와 비밀번호를 확인하세요");
            Debug.Log($"로그인 실패! 상태 코드: {code}");
        }
    }

    public async void OnRegisterClicked()
    {
        if (_view.Password != _view.PasswordConfirm)
        {
            _view.ShowMessage("비밀번호 확인이 일치하지 않습니다.");
            return;
        }

        var regData = new RegisterRequest
        {
            name = _view.UserName,
            email = _view.Email,
            password = _view.Password
        };

        _view.SetLoading(true);
        var code = await _service.Register(regData);
        _view.SetLoading(false);

        if (code == 200) _view.ShowLoginPanel();
        else if (code == 409) _view.ShowMessage("이미 존재하는 이메일입니다.");
        else
        {
            _view.ShowMessage($"잘못된 요청입니다.");
            Debug.Log($"회원가입 실패! 상태 코드: {code}");
        }
    }

    public void OnToLoginClicked()
    {
        _view.ShowLoginPanel();
    }

    public void OnToRegisterClicked()
    {
        _view.ShowRegisterPanel();
    }

    public void OnCloseMessageClicked()
    {
        _view.CloseMessage();
    }


}