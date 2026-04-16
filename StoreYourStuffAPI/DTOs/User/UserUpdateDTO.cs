using System.ComponentModel.DataAnnotations;

namespace StoreYourStuffAPI.DTOs.User
{
    public class UserUpdateDTO
    {
        [Required(ErrorMessage = "Alias is mandatory.")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Alias must be between 1 and 20 characters long.")]
        public string Alias { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is mandatory.")]
        [EmailAddress(ErrorMessage = "Email format not valid.")]
        public string Email { get; set; } = string.Empty;
    }
}
