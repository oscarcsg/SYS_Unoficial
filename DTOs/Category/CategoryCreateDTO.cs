using System.ComponentModel.DataAnnotations;

namespace StoreYourStuffAPI.DTOs.Category
{
    public class CategoryCreateDTO
    {
        [Required(ErrorMessage = "Name is mandatory.")]
        [MaxLength(50, ErrorMessage = "Name can't be larger than 50 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hex Color is mandatory.")]
        [RegularExpression("^[0-9A-Fa-f]{6}$", ErrorMessage = "Invalid hex color format.")]
        public string HexColor { get; set; } = "d2d2d2";

        public bool IsPrivate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid Owner ID is mandatory (min 1, max int.max)")]
        public int OwnerId { get; set; }
    }
}
