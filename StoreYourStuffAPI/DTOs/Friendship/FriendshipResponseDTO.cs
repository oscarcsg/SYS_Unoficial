using StoreYourStuffAPI.DTOs.User;

namespace StoreYourStuffAPI.DTOs.Friendship
{
    public class FriendshipResponseDTO
    {
        public int RequesterId { get; set; }
        public int AddresseeId { get; set; }
        public byte Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public UserResponseDTO? Requester { get; set; }
        public UserResponseDTO? Addressee { get; set; }
    }
}
