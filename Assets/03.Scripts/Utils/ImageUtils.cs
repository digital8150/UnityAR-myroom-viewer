using System;
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

            // 1. Texture 요청 생성
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                // 2. 요청 보내고 완료될 때까지 대기
                var operation = request.SendWebRequest();

                // UnityWebRequest는 Task가 아니므로 완료될 때까지 비동기 루프를 돌아줌
                while (!operation.isDone)
                {
                    await Task.Yield(); // 다음 프레임까지 양보
                }

                // 3. 에러 체크
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[ImageLoad] 이미지 로드 실패: {request.error} (URL: {url})");
                    return null;
                }

                // 4. Texture2D 추출
                Texture2D texture = DownloadHandlerTexture.GetContent(request);

                // 5. Sprite로 변환 (Pivot은 정중앙 0.5f로 설정)
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );

                return sprite;
            }
        }
    }
}