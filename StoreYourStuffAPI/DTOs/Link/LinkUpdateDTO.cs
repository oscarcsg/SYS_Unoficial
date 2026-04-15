namespace StoreYourStuffAPI.DTOs.Link
{
    public class LinkUpdateDTO
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public bool? IsPrivate { get; set; }
        public List<int>? CategoriesIds { get; set; }
    }
}
