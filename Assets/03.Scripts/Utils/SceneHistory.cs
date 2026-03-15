using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Utils
{
    public static class SceneHistory
    {
        // 씬 이름을 차곡차곡 쌓을 스택
        private static Stack<string> history = new Stack<string>();
        private const string HOME_SCENE = "Home"; 

        // 1) 씬 이동: 현재 씬을 기록하고 새로운 씬으로 이동
        public static void ChangeScene(string sceneName)
        {
            // 현재 활성화된 씬의 이름을 스택에 저장
            string currentScene = SceneManager.GetActiveScene().name;
            history.Push(currentScene);

            // 새로운 씬으로 이동
            SceneManager.LoadScene(sceneName);
        }

        // 2) 이전 화면 이동: 기록된 씬이 있다면 되돌아감
        public static void BackToPrevious()
        {
            if (history.Count > 0)
            {
                // 스택의 가장 위(최근 씬)를 꺼내서 이동
                string previousScene = history.Pop();
                SceneManager.LoadScene(previousScene);
            }
            else
            {
                // 이전 기록이 없을 때의 예외 처리 (보통 메인 로비로 보내거나 로그를 남김)
                UnityEngine.Debug.LogWarning("No more scene history to go back to!");
                SceneManager.LoadScene(HOME_SCENE);
            }
        }

        // 히스토리 초기화 (로그아웃이나 초기 화면으로 갈 때 사용)
        public static void ClearHistory()
        {
            history.Clear();
        }
    }
}