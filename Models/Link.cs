namespace StoreYourStuffAPI.Models
{
    public class Link
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Url { get; set; } = string.Empty;
        public bool IsSecure { get; set; } = false;
        public int OwnerId { get; set; }
        public DateTime CreatedAt { get; set; } // DDBB set it automatically

        // Navigation Properties
        // The owner of this link
        public User Owner { get; set; } = null!;
        // This link's categories
        public ICollection<LinkCategory> LinkCategories { get; set; } = [];
        // This link's shared users
        public ICollection<SharedLink> SharedLinks { get; set; } = [];
    }
}
