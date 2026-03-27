namespace StoreYourStuffAPI.DTOs.Category
{
    public class CategoryResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string HexColor { get; set; } = "d2d2d2";
        public bool Private { get; set; }
        public int? OwnerId { get; set; }
    }
}
