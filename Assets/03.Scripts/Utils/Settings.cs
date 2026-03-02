using UnityEngine;

namespace Utils
{
    public static class Settings
    {
        public static string Hostname => "3.34.99.4";

        public static string BaseUrl => $"http://{Hostname}";

        public static string ReplaceLocalhost(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            return url.Replace("localhost", Hostname);
        }
    }
}