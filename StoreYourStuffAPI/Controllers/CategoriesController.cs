using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreYourStuffAPI.Data;
using StoreYourStuffAPI.DTOs.Category;
using StoreYourStuffAPI.Extensions;
using StoreYourStuffAPI.Models;

namespace StoreYourStuffAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        #region Attributes
        private readonly AppDbContext _context;
        #endregion

        #region Constructors
        // Dependences injection: program will give automatically the DDBB translator to this controller
        public CategoriesController(AppDbContext context) { _context = context; }
        #endregion

        #region GET
        // Get all the categories of the logged user (GET /api/categories)
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryResponseDTO>>> GetAllUserCategories()
        {
            var userId = User.GetUserId();

            var userCategories = await _context.Categories
                .Where(c => c.OwnerId == userId)
                .Select(c => new CategoryResponseDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    HexColor = c.HexColor,
                    IsPrivate = c.IsPrivate,
                    OwnerId = c.OwnerId,
                })
                .ToListAsync();

            return Ok(userCategories);
        }
        #endregion

        #region POST
        // Creates a new category using the user token (POST /api/categories)
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CategoryResponseDTO>> CreateCategory(CategoryCreateDTO newCategory)
        {
            var userId = User.GetUserId();

            var categoryEntity = new Category
            {
                Name = newCategory.Name,
                HexColor = newCategory.HexColor,
                IsPrivate = newCategory.IsPrivate,
                OwnerId = userId,
            };

            // Save
            _context.Categories.Add(categoryEntity);
            await _context.SaveChangesAsync();

            var category = new CategoryResponseDTO
            {
                Id = categoryEntity.Id,
                Name = categoryEntity.Name,
                HexColor = categoryEntity.HexColor,
                IsPrivate = categoryEntity.IsPrivate,
                OwnerId = categoryEntity.OwnerId
            };

            // Return a 201 code (created) and the link data
            return CreatedAtAction(nameof(GetCategoryDetail), new { categoryId = categoryEntity.Id }, category);
        }
        #endregion

        #region PUT
        // Updates an existing category with the token (PUT /api/categories/{categoryId})
        [Authorize]
        [HttpPut("{categoryId}")]
        public async Task<IActionResult> UpdateCategory([FromBody] CategoryUpdateDTO updateData, int categoryId)
        {
            var userId = User.GetUserId();

            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.OwnerId != userId)
                return Forbid();

            // Flag
            bool hasChanges = false;

            hasChanges |= TryUpdateName(category, updateData.Name);
            hasChanges |= TryUpdateHexColor(category, updateData.HexColor);
            hasChanges |= TryUpdatePrivacy(category, updateData.IsPrivate);

            if (hasChanges) await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion

        #region DELETE
        // Deletes an existing category with the token (DELETE /api/categories/{categoryId})
        [Authorize]
        [HttpDelete("{categoryId}")]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            var userId = User.GetUserId();

            var category = await _context.Categories.FindAsync(categoryId);

            // If not exists, return 404
            if (category == null)
                return NotFound(new { message = "Category not found." });

            // Check the category is of the user trying to delete it
            if (category.OwnerId != userId)
                return Forbid();

            // Execute the deletetion
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        #endregion

        #region Methods
        // GET a single category by id (GET /api/categories/{categoryId})
        [Authorize]
        [HttpGet("{categoryId}")]
        public async Task<ActionResult<CategoryResponseDTO>> GetCategoryDetail(int categoryId)
        {
            var userId = User.GetUserId();

            var category = await _context.Categories
                .Where(c => c.Id == categoryId && c.OwnerId == userId)
                .Select(c => new CategoryResponseDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    HexColor = c.HexColor,
                    IsPrivate = c.IsPrivate,
                    OwnerId = c.OwnerId
                })
                .FirstOrDefaultAsync();

            if (category == null) return NotFound(new { message = "Category not found." });

            return Ok(category);
        }
        #endregion

        #region Try Update Methods
        private bool TryUpdateName(Category cat, string? name)
        {
            if (!string.IsNullOrWhiteSpace(name) && !string.Equals(cat.Name, name))
            {
                cat.Name = name;
                return true;
            }
            return false;
        }
        private bool TryUpdateHexColor(Category cat, string? hexColor)
        {
            if (!string.IsNullOrWhiteSpace(hexColor) && !string.Equals(cat.HexColor, hexColor))
            {
                cat.HexColor = hexColor;
                return true;
            }
            return false;
        }
        private bool TryUpdatePrivacy(Category cat, bool? privacy)
        {
            if (privacy.HasValue && cat.IsPrivate != privacy)
            {
                cat.IsPrivate = privacy.Value;
                return true;
            }
            return false;
        }

        #endregion
    }
}
