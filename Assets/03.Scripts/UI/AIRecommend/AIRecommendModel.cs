using System;
using System.Collections.Generic;

/// <summary>
/// AI 가구 추천 웹소켓 응답 (JSON Key 매핑 일치 버전)
/// </summary>
[Serializable]
public class RoomAnalysisResponseDto
{
    public int memberId { get; set; }
    public string status { get; set; }
    public long timestamp { get; set; }
    public RoomAnalysisDto roomAnalysis { get; set; }
    public RecommendationDto recommendation { get; set; }
}

[Serializable]
public class RoomAnalysisDto
{
    public string style { get; set; }
    public string color { get; set; }
    public string material { get; set; }
    public List<string> detectedFurniture { get; set; }
    public int detectedCount { get; set; }
    public List<DetailedDetectionDto> detailedDetections { get; set; }
}

[Serializable]
public class DetailedDetectionDto
{
    public string name { get; set; }
    public double confidence { get; set; }
    public List<List<double>> bbox { get; set; }
}

[Serializable]
public class RecommendationDto
{
    public string targetCategory { get; set; }
    public string reasoning { get; set; }
    public string searchQuery { get; set; }
    public List<FurnitureResultDto> results { get; set; }
    public int resultCount { get; set; }
}

[Serializable]
public class FurnitureResultDto
{
    public int rank { get; set; }
    public double score { get; set; }
    public string furniture_type { get; set; } // snake_case 유지
    public int model3d_id { get; set; }       // snake_case 유지
    public string image_path { get; set; }      // snake_case 유지
    public string filename { get; set; }
    public Dictionary<string, object> metadata { get; set; }
}