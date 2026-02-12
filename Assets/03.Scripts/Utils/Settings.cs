using UnityEngine;

namespace Utils
{
    public static class Settings
    {
        public static readonly string BaseUrl = "http://home.codingbot.kr:8080";

        public static string ReplaceLocalhost(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            return url.Replace("localhost", "home.codingbot.kr");
        }
    }
}