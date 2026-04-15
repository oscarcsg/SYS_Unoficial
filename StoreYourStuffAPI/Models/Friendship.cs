using System.ComponentModel.DataAnnotations.Schema;

namespace StoreYourStuffAPI.Models
{
    public class Friendship
    {
        public int RequesterId { get; set; }
        public int AddresseeId { get; set; }

        // status: 0 = pending, 1 = accepted, 2 = declined, 3 = blocked
        public byte Status { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } // Db automatically sets it

        // Navigation Properties
        public User Requester { get; set; } = null!;
        public User Addressee { get; set; } = null!;
    }
}
