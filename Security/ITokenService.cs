using StoreYourStuffAPI.Models;

namespace StoreYourStuffAPI.Security
{
    public interface ITokenService
    {
        string CreateToken(User user);
    }
}
