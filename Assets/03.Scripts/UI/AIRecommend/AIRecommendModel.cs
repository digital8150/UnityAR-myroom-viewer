using System;
using System.Collections.Generic;

/// <summary>
/// AI 가구 추천 웹소켓 응답
/// </summary>
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
    public List<string> DetectedFurniture { get; set; }
    public int DetectedCount { get; set; }
    public List<DetailedDetectionDto> DetailedDetections { get; set; }
}

[Serializable]
public class DetailedDetectionDto
{
    public string Name { get; set; }
    public double Confidence { get; set; }
    public List<List<double>> Bbox { get; set; } // [[x, y], [x, y]] 형태
}

[Serializable]
public class RecommendationDto
{
    public string TargetCategory { get; set; }
    public string Reasoning { get; set; }
    public string SearchQuery { get; set; }
    public List<FurnitureResultDto> Results { get; set; }
    public int ResultCount { get; set; }
}

[Serializable]
public class FurnitureResultDto
{
    public int Rank { get; set; }
    public double Score { get; set; }
    public string FurnitureType { get; set; }
    public int Model3dId { get; set; }
    public string ImagePath { get; set; }
    public string Filename { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}