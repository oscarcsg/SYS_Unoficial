using System.ComponentModel.DataAnnotations;

namespace StoreYourStuffAPI.DTOs.Category
{
    public class CategoryUpdateDTO
    {
        // Opcional, pero si viene, no puede pasar de 50 caracteres.
        [MaxLength(50, ErrorMessage = "Name can't be larger than 50 characters.")]
        public string? Name { get; set; }

        // Opcional, pero si viene, TIENE que cumplir el Regex.
        [RegularExpression("^[0-9A-Fa-f]{6}$", ErrorMessage = "Invalid hex color format.")]
        public string? HexColor { get; set; }

        // Opcional (el bool? es súper importante para distinguir entre "false" y "no enviado")
        public bool? IsPrivate { get; set; }
    }
}
