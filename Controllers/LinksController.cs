using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using StoreYourStuffAPI.Data;
using StoreYourStuffAPI.DTOs.Category;
using StoreYourStuffAPI.DTOs.Link;
using StoreYourStuffAPI.Extensions;

namespace StoreYourStuffAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LinksController : ControllerBase
    {
        #region Attributes
        private readonly AppDbContext _context;
        #endregion

        #region Constructors
        // Dependences injection: program will give automatically the DDBB translator to this controller
        public LinksController(AppDbContext context) { _context = context; }
        #endregion

        #region GET
        // GET a preview of all links of the user using the token (GET /api/links)
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LinkPreviewDTO>>> GetAllUserLinks()
        {
            // Get the user from the DDBB
            var userId = User.GetUserId();

            var userLinks = await _context.Links
                .Where(l => l.OwnerId == userId)
                .Select(l => new LinkPreviewDTO
                {
                    Id = l.Id,
                    Title = l.Title,
                    Description = l.Description,
                    IsPrivate = l.IsPrivate,
                    OwnerId = l.OwnerId
                })
                .ToListAsync();

            return Ok(userLinks);
        }

        // GET the detail of a link by its id (GET /api/links/{linkId})
        [HttpGet("{linkId}")]
        public async Task<ActionResult<LinkResponseDTO>> GetLinkDetail(long linkId)
        {
            // Get the link to compare the owner id and privacy flag
            var link = await GetLinkDetails(linkId);

            if (link == null) return NotFound(new { message = $"Link with id {linkId} not found." });

            // Check if the link is public
            if (!link.IsPrivate) return Ok(link);

            // THE LINK IS PRIVATE
            // Get the user id with the token
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized(new { message = "This link is private. If it is yours, you need to log-in first." });
            
            var userId = User.GetUserId();
            return link.OwnerId != userId ? Forbid() : Ok(link);
        }
        #endregion

        #region POST
        #endregion

        #region PUT
        #endregion

        #region Methods
        private async Task<LinkResponseDTO?> GetLinkDetails(long linkId)
        {
            var link = await _context.Links
                .Where(l => l.Id == linkId)
                .Select(l => new LinkResponseDTO
                {
                    Id = l.Id,
                    Title = l.Title,
                    Description = l.Description,
                    Url = l.Url,
                    IsPrivate = l.IsPrivate,
                    OwnerId = l.OwnerId,
                    CreatedAt = l.CreatedAt,
                    Categories = l.LinkCategories
                        .Select(lc => lc.Category)
                        .Where(c => !c.IsPrivate || c.OwnerId == l.OwnerId)
                        .Select(c => new CategoryResponseDTO
                        {
                            Id = c.Id,
                            Name = c.Name,
                            HexColor = c.HexColor,
                            IsPrivate = c.IsPrivate,
                            OwnerId = c.OwnerId
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            return link;
        }
        #endregion
    }
}
