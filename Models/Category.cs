namespace StoreYourStuffAPI.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string HexColor { get; set; } = "d2d2d2";
        public bool Private { get; set; } = false;
        public int? OwnerId { get; set; }

        // Navigation Properties
        // Owner of this category (null if it is from the system)
        public User? Owner { get; set; }
        // Links with this category
        public ICollection<LinkCategory> LinkCategories { get; set; } = [];
    }
}
