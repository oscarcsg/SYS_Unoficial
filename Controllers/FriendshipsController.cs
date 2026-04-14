using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreYourStuffAPI.Data;
using StoreYourStuffAPI.DTOs.User;
using StoreYourStuffAPI.Extensions;

namespace StoreYourStuffAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FriendshipsController : ControllerBase
    {
        #region Attributes
        private readonly AppDbContext _context;
        #endregion

        #region Constructors
        // Dependences injection: program will give automatically the DDBB translator to this controller
        public FriendshipsController(AppDbContext context) { _context = context; }
        #endregion

        #region GET
        // Get a preview of all the accepted friends, token needed (GET /api/friendships) (status = 1)
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserPreviewDTO>>> GetFriends()
        {
            var userId = User.GetUserId();

            var friends = await _context.Friendships
                .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == 1)
                // If the user is the requester, get the addressee and viceversa
                .Select(f => f.RequesterId == userId ? f.Addressee : f.Requester)
                .Select(user => new UserPreviewDTO
                {
                    Id = user.Id,
                    Alias = user.Alias,
                    Email = user.Email,
                })
                .ToListAsync();

            return Ok(friends);
        }

        // Get a preview of all the pending friendship requests people sent the user, token needed (GET /api/friendships/pending/recieved) (status = 0)
        // Only the pendings the user has RECIEVED
        [Authorize]
        [HttpGet("pending/recieved")]
        public async Task<ActionResult<IEnumerable<UserPreviewDTO>>> GetPendingRecieved()
        {
            var userId = User.GetUserId();

            var pending = await _context.Friendships
                .Where(f => f.AddresseeId == userId && f.Status == 0)
                .Select(f => f.Requester)
                .Select(user => new UserPreviewDTO
                {
                    Id = user.Id,
                    Alias = user.Alias,
                    Email = user.Email,
                })
                .ToListAsync();

            return Ok(pending);
        }

        // Get a preview of all the pending friendship requests the user sent, token needed (GET /api/friendships/pending/sent) (status = 0)
        // Only the pendings the user has SENT
        [Authorize]
        [HttpGet("pending/sent")]
        public async Task<ActionResult<IEnumerable<UserPreviewDTO>>> GetPendingSent()
        {
            var userId = User.GetUserId();

            var pending = await _context.Friendships
                .Where(f => f.RequesterId == userId && f.Status == 0)
                .Select(f => f.Addressee)
                .Select(user => new UserPreviewDTO
                {
                    Id = user.Id,
                    Alias = user.Alias,
                    Email = user.Email,
                })
                .ToListAsync();

            return Ok(pending);
        }
        #endregion

        #region POST
        #endregion

        #region PUT
        #endregion

        #region DELETE
        #endregion

        #region Methods
        #endregion
    }
}
