using dotnet_gs2_2025.Models;

namespace dotnet_gs2_2025.Services;

public interface IHuggingFaceService
{
    Task<ResumeExtraction> ExtractResumeEntitiesAsync(string text, CancellationToken cancellationToken = default);
}

