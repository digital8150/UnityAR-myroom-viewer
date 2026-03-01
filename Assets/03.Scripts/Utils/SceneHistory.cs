using UnityEngine.SceneManagement;

namespace Utils
{
    public static class SceneHistory
    {
        public static string PreviousScene { get; set; }

        public static void MarkCurrentScene()
        {
            Utils.SceneHistory.PreviousScene = SceneManager.GetActiveScene().name;
        }
    }
}