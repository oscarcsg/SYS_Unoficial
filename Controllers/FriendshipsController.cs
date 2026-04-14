using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreYourStuffAPI.Data;
using StoreYourStuffAPI.DTOs.Friendship;
using StoreYourStuffAPI.DTOs.User;
using StoreYourStuffAPI.Extensions;
using StoreYourStuffAPI.Models;

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
        // Sends a fiendship request to other user, needs token (POST /api/fiendships/request/{addresseeId})
        [Authorize]
        [HttpPost("request/{addresseeId}")]
        public async Task<ActionResult<UserPreviewDTO>> SendFriendRequest(int addresseeId)
        {
            var userId = User.GetUserId();

            if (userId == addresseeId)
                return BadRequest(new { message = "You cannot send a friend request to yourself." });

            // Check addressee exists
            var targetUser = await _context.Users.FindAsync(addresseeId);
            if (targetUser == null)
                return NotFound(new { message = "User not found." });

            // Check if this relation already exists (no matter who sent)
            var existingFriendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.RequesterId == userId && f.AddresseeId == addresseeId) ||
                    (f.RequesterId == addresseeId && f.AddresseeId == userId)
                );

            if (existingFriendship != null)
            {
                // If friends
                if (existingFriendship.Status == 1)
                    return BadRequest(new { message = "You are already friends." });

                // If pending
                if (existingFriendship.Status == 0)
                    return BadRequest(new { message = "There is already a pending request between you two." });

                // If blocked
                if (existingFriendship.Status == 3)
                    return BadRequest(new { message = "Blocked friendship." });

                // At this point, it is a "declined", so delete it so it an be resent
                _context.Friendships.Remove(existingFriendship);
            }

            // Create the request
            var newFriendship = new Friendship
            {
                RequesterId = userId,
                AddresseeId = addresseeId,
                Status = 0, // Pending
                CreatedAt = DateTime.UtcNow
            };

            _context.Friendships.Add(newFriendship);
            await _context.SaveChangesAsync();

            // Response, return the user just been requested
            var responseDto = new UserPreviewDTO
            {
                Id = targetUser.Id,
                Alias = targetUser.Alias,
                Email = targetUser.Email
            };

            return Ok(responseDto);
        }
        #endregion

        #region PUT
        // Updates a friendship using the user' token and the id of the other user (PUT /api/friendships/respond/{requesterId})
        [Authorize]
        [HttpPut("respond/{requesterId}")]
        public async Task<IActionResult> UpdateFriendship([FromBody] FriendshipUpdateDTO updateData, int requesterId)
        {
            var userId = User.GetUserId(); // This is the addressee

            // Search the relation where requester is from url and addressee from token
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.RequesterId == requesterId && f.AddresseeId == userId);
            if (friendship == null)
                return NotFound(new { message = "Friend request not found." });

            // Don't waste resources if the status is the same
            if (friendship.Status == updateData.Status)
                return NoContent();

            if (updateData.Status == 2) // Reject
                _context.Friendships.Remove(friendship); // Delete the relation
            else friendship.Status = updateData.Status;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion

        #region DELETE
        // Deletes a relationship with a current friend (DELETE /api/friendships/remove/{friendId})
        [Authorize]
        [HttpDelete("remove/{friendId}")]
        public async Task<IActionResult> RemoveFriend(int friendId)
        {
            var userId = User.GetUserId();

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    ((f.RequesterId == userId && f.AddresseeId == friendId) ||
                    (f.RequesterId == friendId && f.AddresseeId == userId)) &&
                    f.Status == 1
                );
            if (friendship == null)
                return NotFound(new { message = "Friendship not found. You may not be friends yet." });

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion
    }
}
