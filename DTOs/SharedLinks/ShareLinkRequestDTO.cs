using System.ComponentModel.DataAnnotations;

namespace StoreYourStuffAPI.DTOs.SharedLinks
{
    public class ShareLinkRequestDTO
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid User ID is mandatory.")]
        public int TargetUserId { get; set; }
    }
}
