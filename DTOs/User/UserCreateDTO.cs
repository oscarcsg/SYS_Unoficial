using System.ComponentModel.DataAnnotations;

namespace StoreYourStuffAPI.DTOs.User
{
    public class UserCreateDTO
    {
        [Required(ErrorMessage = "Alias is mandatory.")]
        [MaxLength(20, ErrorMessage = "Alias can't be larger than 20 characters.")]
        public string Alias { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is mandatory.")]
        [EmailAddress(ErrorMessage = "Email format is not valid.")]
        [MaxLength(321, ErrorMessage = "Email can't be larger than 321 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is mandatory.")]
        [MinLength(6, ErrorMessage = "Password must be, at least, 6 characters.")]
        [MaxLength(255, ErrorMessage = "Password can't be larger than 255 characters.")]
        public string Password { get; set; } = string.Empty;
    }
}
