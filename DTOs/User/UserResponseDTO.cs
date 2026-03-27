using StoreYourStuffAPI.DTOs.Link;

namespace StoreYourStuffAPI.DTOs.User
{
    public class UserResponseDTO
    {
        public int Id { get; set; }
        public string Alias { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSignIn { get; set; }
        public List<LinkResponseDTO> Links { get; set; } = [];
    }
}
