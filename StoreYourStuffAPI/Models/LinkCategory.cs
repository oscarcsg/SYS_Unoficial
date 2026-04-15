namespace StoreYourStuffAPI.Models
{
    public class LinkCategory
    {
        public long LinkId { get; set; }
        public Link Link { get; set; } = null!;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
    }
}
