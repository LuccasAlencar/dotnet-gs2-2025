namespace dotnet_gs2_2025.Models.DTOs;

public class OccupationInferenceDto
{
    public string? Titulo { get; set; }
    public string? Codigo { get; set; }
    public float Score { get; set; }
    public string? Confidence { get; set; }
    public string? Error { get; set; }
}

public class OccupationInferenceResponseDto
{
    public string? Status { get; set; }
    public double ProcessingTime { get; set; }
    public List<OccupationInferenceDto> Occupations { get; set; } = new();
    public int OccupationsFound { get; set; }
}

public class PrimaryOccupationResponseDto
{
    public string? Status { get; set; }
    public double ProcessingTime { get; set; }
    public OccupationInferenceDto? PrimaryOccupation { get; set; }
}
