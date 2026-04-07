using System.ComponentModel.DataAnnotations.Schema;

namespace StoreYourStuffAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Alias { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } // DDBB sets this automatically
        public DateTime? LastSignIn { get; set; }

        // Navigation Properties
        // Links created by this user
        public ICollection<Link> Links { get; set; } = [];
        // Categories created by this user
        public ICollection<Category> Categories { get; set; } = [];
        // Links shared to other users
        public ICollection<SharedLink> SharedLinks { get; set; } = [];
    }
}
