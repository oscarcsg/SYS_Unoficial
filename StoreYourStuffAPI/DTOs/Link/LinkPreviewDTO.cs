using Microsoft.Identity.Client;
using StoreYourStuffAPI.DTOs.Category;

namespace StoreYourStuffAPI.DTOs.Link
{
    public class LinkPreviewDTO
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPrivate { get; set; }
        public int OwnerId { get; set; }
        public List<CategoryResponseDTO> Categories { get; set; } = [];
    }
}
