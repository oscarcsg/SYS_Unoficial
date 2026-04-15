using System.ComponentModel.DataAnnotations;

namespace StoreYourStuffAPI.DTOs.Friendship
{
    public class FriendshipCreateDTO
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid Requester ID is mandatory.")]
        public int RequesterId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid Addressee ID is mandatory.")]
        public int AddresseeId { get; set; }
    }
}
