using System.ComponentModel.DataAnnotations;

namespace FlowerShopApi.DTOs.Users
{
    public class ChangeRoleDto
    {
        [Required]
        public int RoleId { get; set; }
    }
}