using Newtonsoft.Json;
using UnityEngine;
using System.Threading.Tasks;
using System;

public class AuthPresenter
{
    private readonly AuthView _view;
    private readonly AuthService _service;

    public AuthPresenter(AuthView view, AuthService service)
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
            await WebsocketController.Instance?.ConnectToServer();
            Utils.SceneHistory.ChangeScene("Home");
        }
        else
        {
            if(await isEmailExists(_view.Email))
            {
                _view.ShowPWWrongIndicator();
            }
            else
            {
                _view.ShowIDWrongIndicator();
            }
            Debug.Log($"로그인 실패! 상태 코드: {code}");
        }
    }

    public async void OnRegisterClicked()
    {
        if (_view.Password != _view.PasswordConfirm)
        {
            _view.ShowPWCIncorrect();
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
        else if (await isEmailExists(regData.email)) _view.ShowEmailExist();
        else if (code == 400) _view.ShowRegisterIndicator("입력하신 내용을 확인해주세요.", false);
        else
        {
            PopupView.Instance.ShowMessage($"서버와 연결할 수 없습니다. 네트워크 상태를 확인해주세요.");
            Debug.Log($"회원가입 실패! 상태 코드: {code}");
        }
    }

    public async void OnEmailCheckClicked(string email)
    {
        if(await isEmailExists(email))
        {
            _view.ShowEmailExist();
        }
        else
        {
            _view.ShowEmailOkay();
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

    private async Task<bool> isEmailExists(string email)
    {
        try
        {
            long responseCode;
            string jsonBody;
            (responseCode, jsonBody) = await _service.GetExists(email);

            ExistsResponse response = JsonConvert.DeserializeObject<ExistsResponse>(jsonBody);

            if (responseCode == 200 && response.exists)
            {
                return true;
            }
        }
        catch(Exception ex)
        {
            Debug.LogException(ex);
        }
        return false;
    }
}