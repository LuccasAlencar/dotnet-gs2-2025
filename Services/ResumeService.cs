using dotnet_gs2_2025.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace dotnet_gs2_2025.Services;

public class ResumeService : IResumeService
{
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IHuggingFaceService _huggingFaceService;
    private readonly ILogger<ResumeService> _logger;

    public ResumeService(IPdfTextExtractor pdfTextExtractor, IHuggingFaceService huggingFaceService, ILogger<ResumeService> logger)
    {
        _pdfTextExtractor = pdfTextExtractor ?? throw new ArgumentNullException(nameof(pdfTextExtractor));
        _huggingFaceService = huggingFaceService ?? throw new ArgumentNullException(nameof(huggingFaceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SkillExtractionResult> ExtractSkillsAsync(IFormFile resumeFile, CancellationToken cancellationToken = default)
    {
        if (resumeFile == null)
        {
            throw new ArgumentNullException(nameof(resumeFile));
        }

        if (resumeFile.Length == 0)
        {
            throw new InvalidOperationException("Arquivo de currículo está vazio.");
        }

        _logger.LogInformation("Iniciando extração de habilidades do currículo {FileName} ({FileSize} bytes)", resumeFile.FileName, resumeFile.Length);

        await using var memoryStream = new MemoryStream();
        await resumeFile.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var text = await _pdfTextExtractor.ExtractTextAsync(memoryStream, cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Não foi possível extrair texto do currículo {FileName}", resumeFile.FileName);
            return new SkillExtractionResult
            {
                FileName = resumeFile.FileName,
                FileSizeBytes = resumeFile.Length,
                TextLength = 0,
                Skills = Array.Empty<string>(),
                Locations = Array.Empty<string>(),
                SuggestedLocation = null
            };
        }

        var extraction = await _huggingFaceService.ExtractResumeEntitiesAsync(text, cancellationToken);

        var result = new SkillExtractionResult
        {
            FileName = resumeFile.FileName,
            FileSizeBytes = resumeFile.Length,
            TextLength = text.Length,
            Skills = extraction.Skills,
            Locations = extraction.Locations,
            SuggestedLocation = extraction.Locations.FirstOrDefault()
        };

        _logger.LogInformation("Extração concluída com {SkillCount} habilidades para o currículo {FileName}", result.TotalSkills, resumeFile.FileName);

        return result;
    }

    public async Task<JobMatchResponseDto> CalculateJobMatchAsync(
        List<string> candidateSkills,
        List<string> jobRequirements,
        CancellationToken cancellationToken = default)
    {
        if (candidateSkills == null || candidateSkills.Count == 0)
        {
            throw new ArgumentException("Skills do candidato não podem estar vazios", nameof(candidateSkills));
        }

        if (jobRequirements == null || jobRequirements.Count == 0)
        {
            throw new ArgumentException("Requisitos da vaga não podem estar vazios", nameof(jobRequirements));
        }

        _logger.LogInformation("Calculando matching entre {CandidateSkillCount} skills do candidato e {JobRequirementCount} requisitos", 
            candidateSkills.Count, jobRequirements.Count);

        try
        {
            // Chamar Python API para calcular matching
            var matchResult = await CallPythonMatchApiAsync(candidateSkills, jobRequirements, cancellationToken);

            if (matchResult != null)
            {
                _logger.LogInformation("Matching calculado: {MatchPercentage}", matchResult.MatchPercentage);
                return matchResult;
            }

            _logger.LogWarning("Python API retornou resultado nulo para matching");
            return GetDefaultMatchResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular matching via Python API");
            return GetDefaultMatchResponse();
        }
    }

    private async Task<JobMatchResponseDto> CallPythonMatchApiAsync(
        List<string> candidateSkills,
        List<string> jobRequirements,
        CancellationToken cancellationToken)
    {
        var pythonApiUrl = "http://localhost:5001/api/v1/match-profile";

        var request = new
        {
            candidate_skills = candidateSkills,
            job_requirements = jobRequirements,
            weight_match = 0.7f,
            weight_similarity = 0.3f
        };

        using var httpClient = new HttpClient();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(60)); // 60 segundos para calcular matching

        try
        {
            using var requestContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync(pythonApiUrl, requestContent, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Python API retornou status {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var matchPercentage = root.TryGetProperty("match_percentage", out var percentage)
                ? percentage.GetString() ?? "0%"
                : "0%";

            var matchScore = root.TryGetProperty("match_score", out var score)
                ? score.GetSingle()
                : 0f;

            var level = root.TryGetProperty("level", out var levelElement)
                ? levelElement.GetString() ?? "DESCONHECIDO"
                : "DESCONHECIDO";

            var matchedSkills = ExtractStringArray(root, "matched_skills");
            var missingSkills = ExtractStringArray(root, "missing_skills");
            var matchedCount = root.TryGetProperty("matched_count", out var mc) ? mc.GetInt32() : 0;
            var missingCount = root.TryGetProperty("missing_count", out var mi) ? mi.GetInt32() : 0;
            var requiredCount = root.TryGetProperty("required_count", out var rc) ? rc.GetInt32() : 0;

            // Análise
            var analysis = new MatchAnalysis();
            if (root.TryGetProperty("analysis", out var analysisElement))
            {
                analysis.Strengths = analysisElement.TryGetProperty("strengths", out var strengths)
                    ? strengths.GetString() ?? ""
                    : "";

                analysis.Gaps = analysisElement.TryGetProperty("gaps", out var gaps)
                    ? gaps.GetString() ?? ""
                    : "";

                analysis.Recommendation = analysisElement.TryGetProperty("recommendation", out var recommendation)
                    ? recommendation.GetString() ?? ""
                    : "";
            }

            return new JobMatchResponseDto
            {
                MatchScore = matchScore,
                MatchPercentage = matchPercentage,
                Level = level,
                MatchedSkills = matchedSkills,
                MatchedCount = matchedCount,
                MissingSkills = missingSkills,
                MissingCount = missingCount,
                RequiredCount = requiredCount,
                Analysis = analysis
            };
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("Timeout ao chamar Python API (60s)");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao parsear resposta da Python API de matching");
            return null;
        }
    }

    private List<string> ExtractStringArray(System.Text.Json.JsonElement element, string propertyName)
    {
        var result = new List<string>();

        if (element.TryGetProperty(propertyName, out var arrayElement) && arrayElement.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in arrayElement.EnumerateArray())
            {
                if (item.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var value = item.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        result.Add(value);
                    }
                }
            }
        }

        return result;
    }

    private JobMatchResponseDto GetDefaultMatchResponse()
    {
        return new JobMatchResponseDto
        {
            MatchScore = 0f,
            MatchPercentage = "0%",
            Level = "ERRO",
            MatchedSkills = new(),
            MatchedCount = 0,
            MissingSkills = new(),
            MissingCount = 0,
            RequiredCount = 0,
            Analysis = new()
            {
                Strengths = "Não disponível",
                Gaps = "Erro ao calcular",
                Recommendation = "Tente novamente"
            }
        };
    }

    public async Task<OccupationInferenceResponseDto> InferOccupationsAsync(string resumeText, int topK = 5, float threshold = 0.65f, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resumeText))
        {
            return new OccupationInferenceResponseDto
            {
                Status = "error",
                ProcessingTime = 0,
                OccupationsFound = 0,
                Occupations = new()
            };
        }

        try
        {
            _logger.LogInformation("Inferindo ocupações do currículo");

            var pythonApiUrl = "http://localhost:5001/api/v1/infer-occupation";

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(120);

            var requestBody = new
            {
                resume_text = resumeText.Trim(),
                top_k = topK,
                threshold = threshold
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync(pythonApiUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Python API retornou status {StatusCode} para inferência de ocupação", response.StatusCode);
                return new OccupationInferenceResponseDto
                {
                    Status = "error",
                    ProcessingTime = 0,
                    OccupationsFound = 0,
                    Occupations = new()
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDocument = System.Text.Json.JsonDocument.Parse(responseContent);
            var root = jsonDocument.RootElement;

            var occupations = new List<OccupationInferenceDto>();

            if (root.TryGetProperty("occupations", out var occupationsArray) && occupationsArray.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in occupationsArray.EnumerateArray())
                {
                    var occupation = new OccupationInferenceDto
                    {
                        Titulo = item.TryGetProperty("titulo", out var titulo) ? titulo.GetString() : null,
                        Codigo = item.TryGetProperty("codigo", out var codigo) ? codigo.GetString() : null,
                        Score = item.TryGetProperty("score", out var score) ? score.GetSingle() : 0f,
                        Confidence = item.TryGetProperty("confidence", out var confidence) ? confidence.GetString() : null
                    };

                    occupations.Add(occupation);
                }
            }

            var processingTime = root.TryGetProperty("processing_time", out var pt) ? pt.GetDouble() : 0d;

            return new OccupationInferenceResponseDto
            {
                Status = "success",
                ProcessingTime = processingTime,
                OccupationsFound = occupations.Count,
                Occupations = occupations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inferir ocupações");
            return new OccupationInferenceResponseDto
            {
                Status = "error",
                ProcessingTime = 0,
                OccupationsFound = 0,
                Occupations = new()
            };
        }
    }

    public async Task<PrimaryOccupationResponseDto> InferPrimaryOccupationAsync(string resumeText, float threshold = 0.65f, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resumeText))
        {
            return new PrimaryOccupationResponseDto
            {
                Status = "error",
                ProcessingTime = 0,
                PrimaryOccupation = null
            };
        }

        try
        {
            _logger.LogInformation("Inferindo ocupação primary do currículo");

            var pythonApiUrl = "http://localhost:5001/api/v1/infer-primary-occupation";

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(120);

            var requestBody = new
            {
                resume_text = resumeText.Trim(),
                threshold = threshold
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync(pythonApiUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Python API retornou status {StatusCode} para inferência de ocupação primary", response.StatusCode);
                return new PrimaryOccupationResponseDto
                {
                    Status = "error",
                    ProcessingTime = 0,
                    PrimaryOccupation = null
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDocument = System.Text.Json.JsonDocument.Parse(responseContent);
            var root = jsonDocument.RootElement;

            OccupationInferenceDto? occupation = null;

            if (root.TryGetProperty("primary_occupation", out var occupationElement))
            {
                occupation = new OccupationInferenceDto
                {
                    Titulo = occupationElement.TryGetProperty("titulo", out var titulo) ? titulo.GetString() : null,
                    Codigo = occupationElement.TryGetProperty("codigo", out var codigo) ? codigo.GetString() : null,
                    Score = occupationElement.TryGetProperty("score", out var score) ? score.GetSingle() : 0f,
                    Confidence = occupationElement.TryGetProperty("confidence", out var confidence) ? confidence.GetString() : null,
                    Error = occupationElement.TryGetProperty("error", out var error) ? error.GetString() : null
                };
            }

            var processingTime = root.TryGetProperty("processing_time", out var pt) ? pt.GetDouble() : 0d;

            return new PrimaryOccupationResponseDto
            {
                Status = "success",
                ProcessingTime = processingTime,
                PrimaryOccupation = occupation
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inferir ocupação primary");
            return new PrimaryOccupationResponseDto
            {
                Status = "error",
                ProcessingTime = 0,
                PrimaryOccupation = null
            };
        }
    }

    public async Task<ResumeAnalysisResponseDto> AnalyzeResumeAsync(
        string resumeText,
        float thresholdOccupation = 0.65f,
        float thresholdSkills = 0.75f,
        int topKOccupations = 3,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resumeText))
        {
            return new ResumeAnalysisResponseDto
            {
                Status = "error",
                ResumeType = "unknown",
                ProcessingTime = 0
            };
        }

        try
        {
            var startTime = DateTime.UtcNow;
            
            _logger.LogInformation("Analisando currículo de forma completa");

            var pythonApiUrl = "http://localhost:5001/api/v1/analyze-resume";

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(180);

            var requestBody = new
            {
                resume_text = resumeText.Trim(),
                threshold_occupation = thresholdOccupation,
                threshold_skills = thresholdSkills,
                top_k_occupations = topKOccupations
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync(pythonApiUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Python API retornou status {StatusCode} para análise de currículo", response.StatusCode);
                return new ResumeAnalysisResponseDto
                {
                    Status = "error",
                    ResumeType = "unknown",
                    ProcessingTime = 0
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDocument = System.Text.Json.JsonDocument.Parse(responseContent);
            var root = jsonDocument.RootElement;

            OccupationInferenceDto? primaryOccupation = null;
            var skills = new List<ResumeAnalysisResponseDto.SkillDto>();
            var resumeType = "unknown";
            string? note = null;
            int? totalSkillsFound = null;
            int? successfulMatches = null;

            // Parse primary occupation
            if (root.TryGetProperty("primary_occupation", out var occupationElement))
            {
                primaryOccupation = new OccupationInferenceDto
                {
                    Titulo = occupationElement.TryGetProperty("titulo", out var titulo) ? titulo.GetString() : null,
                    Codigo = occupationElement.TryGetProperty("codigo", out var codigo) ? codigo.GetString() : null,
                    Score = occupationElement.TryGetProperty("score", out var score) ? score.GetSingle() : 0f,
                    Confidence = occupationElement.TryGetProperty("confidence", out var confidence) ? confidence.GetString() : null
                };
            }

            // Parse resume type
            if (root.TryGetProperty("resume_type", out var resumeTypeElement))
            {
                resumeType = resumeTypeElement.GetString() ?? "unknown";
            }

            // Parse skills (if technical)
            if (root.TryGetProperty("skills", out var skillsArray) && skillsArray.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var skillItem in skillsArray.EnumerateArray())
                {
                    var skill = new ResumeAnalysisResponseDto.SkillDto
                    {
                        SkillName = skillItem.TryGetProperty("matched_skill", out var skillName) ? skillName.GetString() : null,
                        OriginalSkill = skillItem.TryGetProperty("original", out var originalSkill) ? originalSkill.GetString() : null,
                        Score = skillItem.TryGetProperty("similarity_score", out var scoreElement) ? scoreElement.GetSingle() : 0f,
                        Confidence = skillItem.TryGetProperty("confidence", out var confElement) ? confElement.GetString() : null
                    };

                    skills.Add(skill);
                }
            }

            // Parse other fields
            if (root.TryGetProperty("total_skills_found", out var totalElement))
            {
                totalSkillsFound = totalElement.GetInt32();
            }

            if (root.TryGetProperty("successful_matches", out var successElement))
            {
                successfulMatches = successElement.GetInt32();
            }

            if (root.TryGetProperty("note", out var noteElement))
            {
                note = noteElement.GetString();
            }

            var processingTime = root.TryGetProperty("processing_time", out var pt) ? pt.GetDouble() : 0d;

            return new ResumeAnalysisResponseDto
            {
                Status = "success",
                ResumeType = resumeType,
                PrimaryOccupation = primaryOccupation,
                Skills = skills,
                TotalSkillsFound = totalSkillsFound,
                SuccessfulMatches = successfulMatches,
                Note = note,
                ProcessingTime = processingTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao analisar currículo");
            return new ResumeAnalysisResponseDto
            {
                Status = "error",
                ResumeType = "unknown",
                ProcessingTime = 0
            };
        }
    }
}

