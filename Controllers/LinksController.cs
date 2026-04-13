using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreYourStuffAPI.Data;
using StoreYourStuffAPI.DTOs.Category;
using StoreYourStuffAPI.DTOs.Link;
using StoreYourStuffAPI.Extensions;
using StoreYourStuffAPI.Models;

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

            if (link == null) return NotFound(new { message = $"Link not found." });

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
        // Creates a new link (POST /api/links)
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<LinkResponseDTO>> CreateLink(LinkCreateDTO newLink)
        {
            // Get the user id with the token
            var userId = User.GetUserId();

            var linkEntity = new Link
            {
                Title = newLink.Title,
                Description = newLink.Description,
                Url = newLink.Url,
                IsPrivate = newLink.IsPrivate,
                OwnerId = userId
            };

            // Process the link' categories
            if (newLink.CategoriesIds != null && newLink.CategoriesIds.Count != 0)
            {
                // SECURITY: search those cats ids on ddbb if they exists and are from the system or user
                var validCategoriesIds = await _context.Categories
                    .Where(c =>
                        newLink.CategoriesIds.Contains(c.Id) &&
                        (c.OwnerId == null || c.OwnerId == userId)
                    )
                    .Select(c => c.Id)
                    .ToListAsync();

                // Create the relation link-category for each valid category
                validCategoriesIds.ForEach(cid =>
                    linkEntity.LinkCategories.Add(
                        new LinkCategory {
                            CategoryId = cid,
                        })
                    );
            }

            // Save to ddbb
            _context.Links.Add(linkEntity);
            await _context.SaveChangesAsync();

            // Return complete DTO
            var createdLink = await GetLinkDetails(linkEntity.Id);

            // Return a 201 code (created) and the link data
            return CreatedAtAction(nameof(GetLinkDetail), new { linkId = linkEntity.Id }, createdLink);
        }
        #endregion

        #region PUT
        // Updates a link (PUT /api/links/{linkId})
        [Authorize]
        [HttpPut("{linkId}")]
        public async Task<IActionResult> UpdateLink([FromBody] LinkUpdateDTO updateData, long linkId)
        {
            var userId = User.GetUserId();

            var link = await _context.Links
                .Include(l => l.LinkCategories) // Ensure to also bring all the categories
                .FirstOrDefaultAsync(l => l.Id == linkId);
            if (link == null)
                return NotFound(new { message = "Link not found." });

            if (link.OwnerId != userId)
                return Forbid();

            // Flag
            bool hasChanges = false;

            hasChanges |= TryUpdateTitle(link, updateData.Title);
            hasChanges |= TryUpdateDescription(link, updateData.Description);
            hasChanges |= TryUpdateUrl(link, updateData.Url);
            hasChanges |= TryUpdatePrivacy(link, updateData.IsPrivate);
            hasChanges |= await TryUpdateCategoriesAsync(link, updateData.CategoriesIds, userId);

            if (hasChanges) await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion

        #region DELETE
        // Deletes a link (DELETE /api/links/{linkId})
        [Authorize]
        [HttpDelete("{linkId}")]
        public async Task<IActionResult> DeleteLink(long linkId)
        {
            var userId = User.GetUserId();

            // Get the link
            var link = await _context.Links.FindAsync(linkId);

            // If not exists, return 404
            if (link == null)
                return NotFound(new { message = "Link not found." });

            // Check the link is of the user trying to delete it
            if (link.OwnerId != userId)
                return Forbid();

            // Execute the deletetion
            _context.Links.Remove(link);
            await _context.SaveChangesAsync();

            return NoContent();
        }
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

        #region Try Update Methods
        private bool TryUpdateTitle(Link link, string? title)
        {
            if (!string.IsNullOrWhiteSpace(title) && !string.Equals(link.Title, title))
            {
                link.Title = title;
                return true;
            }
            return false;
        }
        private bool TryUpdateDescription(Link link, string? description)
        {
            if (description != null)
            {
                if (!string.IsNullOrWhiteSpace(description) && !string.Equals(link.Description, description))
                {
                    link.Description = description;
                    return true;
                }
                else if (string.IsNullOrWhiteSpace(description) && link.Description != null)
                {
                    link.Description = null;
                    return true;
                }
            }
            return false;
        }
        private bool TryUpdateUrl(Link link, string? url)
        {
            if (!string.IsNullOrWhiteSpace(url) && !string.Equals(link.Url, url))
            {
                link.Url = url;
                return true;
            }
            return false;
        }
        private bool TryUpdatePrivacy(Link link, bool? privacyState)
        {
            if (privacyState.HasValue && link.IsPrivate != privacyState)
            {
                link.IsPrivate = privacyState.Value;
                return true;
            }
            return false;
        }
        private async Task<bool> TryUpdateCategoriesAsync(Link link, List<int>? newCategories, int currentUserId)
        {
            // Check the categories send by the front
            if (newCategories == null) return false;

            // Get current link' categories ids
            // This controls the nullable data of the list, if it is null, '??' turns it to a blank list
            var currentIds = (link.LinkCategories ?? []).Select(lc => lc.CategoryId).ToList();

            // Is in newCategories but NOT in the current ones (add)
            var toAdd = newCategories.Except(currentIds).ToList();
            // Is in the current ones but NOT in the newCategories (delete)
            var toRemove = currentIds.Except(newCategories).ToList();

            // If there is nothing to add, neither to delete, no changes
            if (!toAdd.Any() && !toRemove.Any()) return false;

            bool hasChanges = false;

            // Process Deleted ones
            if (toRemove.Any())
            {
                // Get the LinkCategory objects that match with the id to delete
                var categoriesToRemove = link.LinkCategories!
                    .Where(lc => toRemove.Contains(lc.CategoryId))
                    .ToList();

                // Delete all of them
                foreach (var cat in categoriesToRemove)
                {
                    link.LinkCategories!.Remove(cat);
                }
                hasChanges |= true;
            }

            // Process Added ones
            if (toAdd.Any())
            {
                // Go to DB to check which ids are valid and public or of the user
                var validCategoriesToAdd = await _context.Categories
                    .Where(c => toAdd.Contains(c.Id) && (c.OwnerId == null || c.OwnerId == currentUserId))
                    .Select(c => c.Id)
                    .ToListAsync();

                // Add all of them
                foreach (var validCatId in validCategoriesToAdd)
                {
                    link.LinkCategories!.Add(new LinkCategory
                    {
                        LinkId = link.Id,
                        CategoryId = validCatId
                    });
                    hasChanges |= true;
                }
            }

            return hasChanges;
        }
        #endregion
    }
}
