using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;

//Websocket 메세지
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

//모델 API 조회
[Serializable]
public class ModelData
{
    public int id;
    public string name;
    public string createdAt;
    public string link;
    public int creatorId;
    public bool is_shared;
    public string description;
    public string thumbnailUrl;
    public bool is_vector_db_trained;
    public string status;
    public string errorMessage;
    public string shopPageLink;
}

//모델 저장 DTO
public class ModelUpdateData
{
    public string name;
    public bool is_shared;
    public string description;
    public string link;
    public string shopPageLink;
}

[Serializable]
public class  ModelDimension
{
    public float width;
    public float length;
    public float height;

    public ModelDimension() { }

    public ModelDimension(float width = 0.0f, float length = 0.0f, float height=0.0f)
    {
        this.width = width;
        this.length = length;
        this.height = height;
    }

}

[Serializable]
public class PageableInfo
{
    public int offset;
    public int pageNumber;
    public int pageSize;
    public bool paged;
}

[Serializable]
public class ModelSearchResponse
{
    public List<ModelData> content; // 여기에 실제 데이터 리스트가 들어옴!
    public bool last;               // 무한 스크롤 끝인지 확인할 때 필수 ✨
    public int totalElements;
    public int totalPages;
    public int number;              // 현재 페이지 번호
    public int size;
    public bool empty;
    public PageableInfo pageable;
}
