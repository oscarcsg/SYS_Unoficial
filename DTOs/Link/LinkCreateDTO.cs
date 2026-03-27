using System.ComponentModel.DataAnnotations;

namespace StoreYourStuffAPI.DTOs.Link
{
    public class LinkCreateDTO
    {
        [Required(ErrorMessage = "Title is mandatory.")]
        [MaxLength(100, ErrorMessage = "Title can't be larger than 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Description can't be larger than 1000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "URL is mandatory.")]
        [MaxLength(500, ErrorMessage = "URL can't be larger than 500 characters.")]
        [Url(ErrorMessage = "URL format is not valid.")]
        public string Url { get; set; } = string.Empty;

        public bool IsSecure { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid Owner ID is mandatory (min 1, max int.max)")]
        public int OwnerId { get; set; }

        public List<int> CategoriesIds { get; set; } = [];
    }
}
