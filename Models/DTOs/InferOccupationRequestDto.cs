namespace dotnet_gs2_2025.Models.DTOs;

public class InferOccupationRequestDto
{
    public string? ResumeText { get; set; }
    public int? TopK { get; set; }
    public float? Threshold { get; set; }
}
