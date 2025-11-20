using System.ComponentModel.DataAnnotations;

namespace dotnet_gs2_2025.Models.DTOs;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    [StringLength(255, MinimumLength = 6, ErrorMessage = "Senha deve ter entre 6 e 255 caracteres")]
    public string Password { get; set; } = string.Empty;
}
