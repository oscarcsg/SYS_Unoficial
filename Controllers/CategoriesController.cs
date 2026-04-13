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
        #endregion

        #region DELETE
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
    }
}
