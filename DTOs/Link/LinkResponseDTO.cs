using StoreYourStuffAPI.DTOs.Category;

namespace StoreYourStuffAPI.DTOs.Link
{
    public class LinkResponseDTO
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Url { get; set; } = string.Empty;
        public bool IsSecure { get; set; }
        public int OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CategoryResponseDTO> Categories { get; set; } = [];
    }
}
