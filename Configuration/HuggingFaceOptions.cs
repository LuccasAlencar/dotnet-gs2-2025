namespace dotnet_gs2_2025.Configuration;

public class HuggingFaceOptions
{
    public const string SectionName = "HuggingFace";

    public string SkillsModel { get; set; } = "microsoft/DialoGPT-medium";

    public string LocationsModel { get; set; } = "dslim/bert-base-NER";

    public string? Token { get; set; }

    public double MinScore { get; set; } = 0.9;

    public string PythonSkillsApiUrl { get; set; } = "http://localhost:5001/api/v1/skills";
}

