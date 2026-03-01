using UnityEngine;

namespace Utils
{
    public static class Settings
    {
        public static string Hostname => "localhost";

        public static string BaseUrl => $"http://{Hostname}:8080";

        public static string ReplaceLocalhost(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            return url.Replace("localhost", Hostname);
        }
    }
}