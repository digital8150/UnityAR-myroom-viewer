using UnityEngine;
using System;

[Serializable]
public class ModelGenerationResponse
{
    public string notificationType;      // MODEL_GENERATION_SUCCESS
    public int memberId;                 // 사용자 ID
    public string originalImageUrl;      // 원본 이미지 URL
    public string model3dUrl;           // ⭐ 실제 .glb 모델 파일 다운로드 주소
    public string thumbnailUrl;         // 썸네일 이미지 URL
    public string status;                // SUCCESS / FAIL
    public string message;               // 서버 메시지
    public int processingTimeSeconds;    // 소요 시간 (초)
    public long timestamp;               // 생성 시간 (Unix Timestamp)
}