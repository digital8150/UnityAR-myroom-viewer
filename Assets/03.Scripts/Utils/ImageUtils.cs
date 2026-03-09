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
                    Debug.LogError($"[ImageLoad] 실패: {request.error} {request.downloadHandler.error} (URL: {url})");
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

        public static async Task<Sprite> LoadSpriteFromUrlAlternative(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                var operation = request.SendWebRequest();
                var tcs = new TaskCompletionSource<bool>();
                operation.completed += _ => tcs.TrySetResult(true);

                await tcs.Task;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[ImageLoad] 통신 실패: {request.error} (URL: {url})");
                    return null;
                }

                byte[] imageData = request.downloadHandler.data;

                // 텍스처 생성 시 포맷을 명시하지 않고 LoadImage에 맡김
                // 하지만 생성 시점에 기본 설정을 잡아주는 게 안정적이야
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                if (texture.LoadImage(imageData))
                {
                    // 가끔 가로세로가 2x2로 고정되는 버그 방지
                    texture.Apply();

                    return Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100.0f
                    );
                }
                else
                {
                    // 여기서 실패한다면 파일 자체가 유니티가 해석 못하는 특수 PNG 포맷인 거야
                    Debug.LogError($"[ImageLoad] 디코딩 실패 - 데이터 크기: {imageData.Length} bytes (URL: {url})");
                    return null;
                }
            }
        }
    }
}