using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Utils
{
    public static class ImageUtils
    {
        public static async Task<Sprite> LoadSpriteFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                // 비동기 요청 시작
                var operation = request.SendWebRequest();
                var tcs = new TaskCompletionSource<bool>();
                operation.completed += _ => tcs.TrySetResult(true);

                await tcs.Task;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[ImageLoad] 실패: {request.error} (URL: {url})");
                    return null;
                }

                Texture2D texture = DownloadHandlerTexture.GetContent(request);

                return Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100.0f,
                    0,
                    SpriteMeshType.FullRect
                );
            }
        }
    }
}