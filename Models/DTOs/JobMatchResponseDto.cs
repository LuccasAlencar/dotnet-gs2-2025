namespace dotnet_gs2_2025.Models.DTOs;

/// <summary>
/// Response com resultado do matching entre candidato e vagas
/// </summary>
public class JobMatchResponseDto
{
    /// <summary>
    /// Score de match entre 0 e 1
    /// </summary>
    public float MatchScore { get; set; }

    /// <summary>
    /// Percentual de match (ex: "80%")
    /// </summary>
    public string MatchPercentage { get; set; } = "0%";

    /// <summary>
    /// Nível de adequação (EXCELENTE, BOM, MODERADO, BAIXO, INSUFICIENTE)
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// Skills do candidato que correspondem aos requisitos
    /// </summary>
    public List<string> MatchedSkills { get; set; } = new();

    /// <summary>
    /// Quantidade de skills que fazem match
    /// </summary>
    public int MatchedCount { get; set; }

    /// <summary>
    /// Skills requeridas que o candidato não possui
    /// </summary>
    public List<string> MissingSkills { get; set; } = new();

    /// <summary>
    /// Quantidade de skills faltantes
    /// </summary>
    public int MissingCount { get; set; }

    /// <summary>
    /// Número total de skills requeridas
    /// </summary>
    public int RequiredCount { get; set; }

    /// <summary>
    /// Análise detalhada do matching
    /// </summary>
    public MatchAnalysis Analysis { get; set; } = new();
}

/// <summary>
/// Análise detalhada do matching
/// </summary>
public class MatchAnalysis
{
    /// <summary>
    /// Pontos fortes do candidato
    /// </summary>
    public string Strengths { get; set; } = string.Empty;

    /// <summary>
    /// Gaps de conhecimento
    /// </summary>
    public string Gaps { get; set; } = string.Empty;

    /// <summary>
    /// Recomendação geral
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;
}
