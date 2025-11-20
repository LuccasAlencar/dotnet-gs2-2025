namespace dotnet_gs2_2025.Models.DTOs;

/// <summary>
/// Request para calcular matching entre skills do candidato e requisitos da vaga
/// </summary>
public class JobMatchRequestDto
{
    /// <summary>
    /// Skills que o candidato possui
    /// </summary>
    public List<string> CandidateSkills { get; set; } = new();

    /// <summary>
    /// Skills requeridas para a vaga
    /// </summary>
    public List<string> JobRequirements { get; set; } = new();

    /// <summary>
    /// Peso para matches exatos (padrão: 0.7)
    /// </summary>
    public float WeightMatch { get; set; } = 0.7f;

    /// <summary>
    /// Peso para similaridade semântica (padrão: 0.3)
    /// </summary>
    public float WeightSimilarity { get; set; } = 0.3f;
}
