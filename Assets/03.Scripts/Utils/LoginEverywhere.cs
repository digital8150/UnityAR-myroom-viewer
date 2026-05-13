using Newtonsoft.Json;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class LoginEverywhere : MonoBehaviour
{
#if UNITY_EDITOR
    private static LoginEverywhere _instance;

    [Header("Debug Settings")]
    [SerializeField] private string debugEmail = "test@test.com";
    [SerializeField] private string debugPassword = "password123";
    [SerializeField] private bool autoLoginInEditor = true;

    private void Awake()
    {
        // 싱글톤 처리
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);


        if (autoLoginInEditor)
        {
            CheckAndPerformDebugLogin();
        }

        
    }


    private async void CheckAndPerformDebugLogin()
    {
        if (!string.IsNullOrEmpty(JWTToken.Token)) return;

        Debug.Log("<color=yellow>[Debug] 에디터 자동 로그인 시도 중...</color>");

        var loginData = new LoginRequest { email = debugEmail, password = debugPassword };
        var (code, jsonBody) = await AuthService.Login(loginData);

        if (code == 200)
        {
            try
            {
                LoginResponse loginResponse = JsonConvert.DeserializeObject<LoginResponse>(jsonBody);
                JWTToken.Token = loginResponse.token;
                JWTToken.RefreshToken = loginResponse.refreshToken;

                // 웹소켓 연결까지 기다림
                if (WebsocketController.Instance != null)
                {
                    await WebsocketController.Instance.ConnectToServer();
                    Debug.Log("<color=cyan>[Debug] 웹소켓 연결 완료. 씬을 리프레쉬합니다.</color>");
                }

                // --- 씬 리프레쉬 로직 ---
                // 현재 활성화된 씬의 빌드 인덱스를 가져와서 다시 로드해
                Scene currentScene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(currentScene.buildIndex);
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        else
        {
            Debug.LogError($"[Debug] 로그인 실패! 상태 코드: {code}");
        }
    }
#endif
}