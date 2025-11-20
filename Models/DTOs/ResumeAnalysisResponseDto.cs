namespace dotnet_gs2_2025.Models.DTOs;

public class ResumeAnalysisResponseDto
{
    public string? Status { get; set; }
    public string? ResumeType { get; set; }
    public OccupationInferenceDto? PrimaryOccupation { get; set; }
    public List<SkillDto> Skills { get; set; } = new();
    public int? TotalSkillsFound { get; set; }
    public int? SuccessfulMatches { get; set; }
    public string? Note { get; set; }
    public double ProcessingTime { get; set; }

    public class SkillDto
    {
        public string? SkillName { get; set; }
        public string? OriginalSkill { get; set; }
        public float Score { get; set; }
        public string? Confidence { get; set; }
    }
}
