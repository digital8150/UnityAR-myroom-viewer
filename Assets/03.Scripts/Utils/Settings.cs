using UnityEngine;

namespace Utils
{
    public static class Settings
    {
        private static string hostname = "home.codingbot.kr";

        public static string BaseUrl => $"http://{hostname}:8080";

        public static string ReplaceLocalhost(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            return url.Replace("localhost", hostname);
        }
    }
}