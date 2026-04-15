namespace StoreYourStuffAPI.DTOs.User
{
    public class LoginDTO
    {
        public string? Alias { get; set; } = null;
        public string? Email { get; set; } = null;
        public string Password { get; set; } = string.Empty;
    }
}
