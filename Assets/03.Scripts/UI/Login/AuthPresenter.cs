using Newtonsoft.Json;
using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class AuthPresenter
{
    private readonly AuthView _view;
    private float _splashDuration;

    public AuthPresenter(AuthView view)
    {
        _view = view;
    }

    public void Start(float spashDuration)
    {
        _splashDuration = spashDuration;
        _view.InitView();
        _view.StartCoroutine(SplashScreenSequence());
    }

    private IEnumerator SplashScreenSequence()
    {
        // 2. Task 실행
        yield return _view.StartCoroutine(DOTransitionSplash(0.5f, 0f, 0f, 1f));
        var logonTask = TryLogonWithRefreshToken();
        yield return new WaitForSeconds(_splashDuration);

        // 3. Task가 완료될 때까지 코루틴 중단 (WaitUntil 사용)
        yield return new WaitUntil(() => logonTask.IsCompleted);

        if (logonTask.Result)
        {
            Utils.SceneHistory.ChangeScene("Home");
        }
        else
        {
            yield return _view.StartCoroutine(DOTransitionSplash(0.5f, 0f, 1f, 0f));

            _view.SetSplashScreenOpacity(0f);
            _view.SetSplashScreenActive(false);
        }
    }

    private IEnumerator DOTransitionSplash(float duration, float elapsed, float startAlpha, float targetAlpha)
    {

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newOpacity = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            _view.SetSplashScreenOpacity(newOpacity);
            yield return null;
        }
    }

    public async void OnLoginClicked()
    {
        var loginData = new LoginRequest
        {
            email = _view.Email,
            password = _view.Password
        };

        _view.SetLoading(true);
        var (code, body) = await AuthService.Login(loginData);
        _view.SetLoading(false);

        if (code == 200)
        {
            try
            {
                LoginResponse response = JsonConvert.DeserializeObject<LoginResponse>(body);
                JWTToken.Token = response.token;
                if (_view.GetAutoLoginToggle())
                {
                    Utils.Cipher.SecureStorage.Instance.SetValue("RefreshToken", response.refreshToken);
                }
                else
                {
                    Utils.Cipher.SecureStorage.Instance.DeleteValue("RefreshToken");
                }
                await WebsocketController.Instance?.ConnectToServer();
                Utils.SceneHistory.ChangeScene("Home");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                PopupView.Instance.ShowMessage("로그인 처리 중 오류가 발생했습니다.");
            }

        }
        else
        {
            if(await IsEmailExists(_view.Email))
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
        var code = await AuthService.Register(regData);
        _view.SetLoading(false);

        if (code == 200) _view.ShowRegisterCompletedPanel();
        else if (await IsEmailExists(regData.email)) _view.ShowEmailExist();
        else if (code == 400) _view.ShowRegisterIndicator("입력하신 내용을 확인해주세요.", false);
        else
        {
            PopupView.Instance.ShowMessage($"서버와 연결할 수 없습니다. 네트워크 상태를 확인해주세요.");
            Debug.Log($"회원가입 실패! 상태 코드: {code}");
        }
    }

    public async void OnEmailCheckClicked(string email)
    {
        if(await IsEmailExists(email))
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

    public async Task<bool> TryLogonWithRefreshToken()
    {
        if(PlayerPrefs.HasKey("RefreshToken"))
        {
            string refreshToken = Utils.Cipher.SecureStorage.Instance.GetValue("RefreshToken");
#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[에디터 전용 로그]\n" +
                $"불러온 리프레쉬 토큰 : {refreshToken}\n" +
                $"기기 저장소 상 값 : {PlayerPrefs.GetString("RefreshToken")}</color>");
#endif
            RefreshRequest requestData = new RefreshRequest();
            requestData.refreshToken = refreshToken;
            var (responseCode, responseBody) = await AuthService.PostRefrsh(requestData);
            if(responseCode == 200)
            {
                try
                {
                    LoginResponse loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseBody);
                    if (!String.IsNullOrEmpty(loginResponse.token))
                    {
                        JWTToken.Token = loginResponse.token;
                        await WebsocketController.Instance?.ConnectToServer();
                        return true;
                    }

                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

        }

        return false;
    }

    private async Task<bool> IsEmailExists(string email)
    {
        try
        {
            long responseCode;
            string jsonBody;
            (responseCode, jsonBody) = await AuthService.GetExists(email);

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