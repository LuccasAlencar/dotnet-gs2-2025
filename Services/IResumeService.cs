using Microsoft.AspNetCore.Http;
using dotnet_gs2_2025.Models.DTOs;

namespace dotnet_gs2_2025.Services;

public interface IResumeService
{
    Task<SkillExtractionResult> ExtractSkillsAsync(IFormFile resumeFile, CancellationToken cancellationToken = default);

    Task<JobMatchResponseDto> CalculateJobMatchAsync(
        List<string> candidateSkills,
        List<string> jobRequirements,
        CancellationToken cancellationToken = default);

    Task<OccupationInferenceResponseDto> InferOccupationsAsync(
        string resumeText,
        int topK = 5,
        float threshold = 0.65f,
        CancellationToken cancellationToken = default);

    Task<PrimaryOccupationResponseDto> InferPrimaryOccupationAsync(
        string resumeText,
        float threshold = 0.65f,
        CancellationToken cancellationToken = default);

    Task<ResumeAnalysisResponseDto> AnalyzeResumeAsync(
        string resumeText,
        float thresholdOccupation = 0.65f,
        float thresholdSkills = 0.75f,
        int topKOccupations = 3,
        CancellationToken cancellationToken = default);
}
