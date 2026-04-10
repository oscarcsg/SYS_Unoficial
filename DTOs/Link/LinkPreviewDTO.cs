using Microsoft.Identity.Client;

namespace StoreYourStuffAPI.DTOs.Link
{
    public class LinkPreviewDTO
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPrivate { get; set; }
        public int OwnerId { get; set; }
    }
}
