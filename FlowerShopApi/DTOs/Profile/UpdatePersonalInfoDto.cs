using System.ComponentModel.DataAnnotations;

namespace FlowerShopApi.DTOs.Profile;

public class UpdatePersonalInfoDto
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }
}