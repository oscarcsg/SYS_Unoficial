using System.Security.Claims;

namespace StoreYourStuffAPI.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        // Get the current user id (token)
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var userString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;

            if (!int.TryParse(userString, out int userId))
                throw new UnauthorizedAccessException("Invalid token or without ID.");

            return userId;
        }
    }
}
