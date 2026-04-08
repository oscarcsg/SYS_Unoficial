using System.ComponentModel.DataAnnotations;

namespace StoreYourStuffAPI.DTOs.Friendship
{
    public class FriendshipUpdateDTO
    {
        [Required(ErrorMessage = "Status is mandatory.")]
        [Range(0, 3, ErrorMessage = "Status must be between 0 and 3.")]
        public byte Status { get; set; }
    }
}
