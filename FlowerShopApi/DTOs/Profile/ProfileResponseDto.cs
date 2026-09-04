namespace FlowerShopApi.DTOs.Profile;

public class ProfileResponseDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public string? StreetAddress { get; set; }
    public string? PostalCode { get; set; }
    public string? Role { get; set; }
}