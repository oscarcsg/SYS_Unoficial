namespace StoreYourStuffAPI.Models
{
    public class SharedLink
    {
        public long LinkId { get; set; }
        public Link Link { get; set; } = null!; // Navigation property

        public int UserId { get; set; }
        public User User { get; set; } = null!; // Navigation property

        public DateTime SharedAt { get; set; } = DateTime.UtcNow;
    }
}
