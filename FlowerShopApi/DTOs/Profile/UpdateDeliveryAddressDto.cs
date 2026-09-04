using System.ComponentModel.DataAnnotations;

namespace FlowerShopApi.DTOs.Profile;

public class UpdateDeliveryAddressDto
{
    [MaxLength(100)]
    public string? Region { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(255)]
    public string? StreetAddress { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }
}