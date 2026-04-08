using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreYourStuffAPI.Data;
using StoreYourStuffAPI.DTOs.Category;

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

        #region GET LINKS
        // GET all link's categories (GET /api/links/{linkId}/categories)
        [HttpGet("{linkId}/categories")]
        public async Task<ActionResult<IEnumerable<CategoryResponseDTO>>> GetAllLinkCategories(int linkId)
        {
            // Ensure the link exists
            var link = await _context.Links.FindAsync(linkId);
            if (link == null)
                return NotFound(new { message = "Link not found." });

            // Get all link' categories (using intermediate table LinkCategories)
            var linkCategories = _context.LinkCategories
                // Where the linkIds are the same than the link to search
                .Where(lc => lc.LinkId == linkId)
                // Get only those categories
                .Select(lc => lc.Category)

                .Where(c =>
                    // Category' owner is the same than the link or the system
                    (c.OwnerId == link.OwnerId || c.OwnerId == null)
                    // And is public
                    && !c.IsPrivate
                )
                // Map to DTO
                .Select(c => new CategoryResponseDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    HexColor = c.HexColor,
                    IsPrivate = c.IsPrivate,
                    OwnerId = c.OwnerId
                })
                .ToListAsync();

            return Ok(linkCategories);
        }
        #endregion

        #region POST LINKS
        #endregion
    }
}
