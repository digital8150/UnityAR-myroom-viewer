#if UNITY_ANDROID
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Utils
{
    public class AndroidBackHandler : MonoBehaviour
    {
        private const string HOME_SCENE = "Home";
        private const string LOGIN_SCENE = "Login";

        private static AndroidBackHandler _instance;
        private bool _isQuitConfirmShowing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Application.platform != RuntimePlatform.Android) return;
            if (_instance != null) return;

            var go = new GameObject(nameof(AndroidBackHandler));
            _instance = go.AddComponent<AndroidBackHandler>();
            DontDestroyOnLoad(go);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (!keyboard.escapeKey.wasPressedThisFrame) return;
            HandleBackPressed();
        }

        private void HandleBackPressed()
        {
            if (_isQuitConfirmShowing) return;

            string currentScene = SceneManager.GetActiveScene().name;

            // 홈/로그인 씬에서 돌아갈 곳이 없거나, 돌아갈 곳이 로그인 씬뿐이면 종료 확인 팝업을 띄움
            bool isTerminalScene = currentScene == HOME_SCENE || currentScene == LOGIN_SCENE;
            string previous = SceneHistory.PeekPrevious();
            bool noMeaningfulBack = previous == null || previous == LOGIN_SCENE;
            if (isTerminalScene && noMeaningfulBack)
            {
                ShowQuitConfirm();
                return;
            }

            SceneHistory.BackToPrevious();
        }

        private void ShowQuitConfirm()
        {
            var popup = PopupView.Instance;
            if (popup == null || popup.Presenter == null)
            {
                Application.Quit();
                return;
            }

            _isQuitConfirmShowing = true;
            popup.Presenter.ShowYesNo(
                "앱을 종료하시겠습니까?",
                onYes: () =>
                {
                    _isQuitConfirmShowing = false;
                    Application.Quit();
                },
                onNo: () =>
                {
                    _isQuitConfirmShowing = false;
                });
        }
    }
}
#endif
