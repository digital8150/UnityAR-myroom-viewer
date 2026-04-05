using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class RoomAnalysisResponseDto
{
    public int MemberId { get; set; }
    public string Status { get; set; }
    public long Timestamp { get; set; }
    public RoomAnalysisDto RoomAnalysis { get; set; }
    public RecommendationDto Recommendation { get; set; }
}

[Serializable]
public class RoomAnalysisDto
{
    public string Style { get; set; }
    public string Color { get; set; }
    public string Material { get; set; }

    [JsonProperty("detected_furniture")]
    public List<string> DetectedFurniture { get; set; }

    [JsonProperty("detected_count")]
    public int DetectedCount { get; set; }

    [JsonProperty("detailed_detections")]
    public List<DetailedDetectionDto> DetailedDetections { get; set; }
}

[Serializable]
public class DetailedDetectionDto
{
    public string Name { get; set; }

    [JsonProperty("name_en")]
    public string NameEn { get; set; }

    public double Confidence { get; set; }

    [JsonProperty("bbox")]
    public List<List<double>> BoundingBox { get; set; } // bbox도 명확하게 컨벤션 맞춰서 변경 추천
}

[Serializable]
public class RecommendationDto
{
    [JsonProperty("target_category")] // 혹시 몰라서 추가
    public string TargetCategory { get; set; }

    public string Reasoning { get; set; }

    [JsonProperty("search_query")] // 혹시 몰라서 추가
    public string SearchQuery { get; set; }

    public List<FurnitureResultDto> Results { get; set; }

    [JsonProperty("result_count")]
    public int ResultCount { get; set; }
}

[Serializable]
public class FurnitureResultDto
{
    public int Rank { get; set; }
    public double Score { get; set; }

    [JsonProperty("furniture_type")]
    public string FurnitureType { get; set; }

    [JsonProperty("model3d_id")]
    public int Model3dId { get; set; }

    [JsonProperty("image_path")]
    public string ImagePath { get; set; }

    public string Filename { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}