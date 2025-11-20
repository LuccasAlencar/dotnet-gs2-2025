namespace dotnet_gs2_2025.Models.DTOs;

public class AuthResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public UserResponseDto? User { get; set; }
    public string? Token { get; set; }
}
