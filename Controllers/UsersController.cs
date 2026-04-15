using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreYourStuffAPI.Data;
using StoreYourStuffAPI.DTOs.Category;
using StoreYourStuffAPI.DTOs.Link;
using StoreYourStuffAPI.DTOs.User;
using StoreYourStuffAPI.Extensions;
using StoreYourStuffAPI.Models;
using StoreYourStuffAPI.Security;

namespace StoreYourStuffAPI.Controllers
{
    [Route("api/[controller]")] // Defines the API route
    [ApiController]
    public class UsersController : ControllerBase
    {
        #region Attributes
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        #endregion

        #region Constructors
        // Dependences injection: program will give automatically the DDBB translator to this controller
        public UsersController(AppDbContext context, IPasswordHasher passwordHasher, ITokenService tokenService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }
        #endregion

        #region GET
        // GET all users (GET /api/users)
        // TODO: MODIFICAR ESTE MÉTODO PARA HACERLO MODO EXPLORACIÓN, es decir, que traiga solo 20 usuarios por ejemplo
            // y sólo con id, alias, total_links_publicos y total_categorias_publicas para implementarlo
            // en una ventana de exploración general de usuarios con posibilidad de filtrado y ordenado.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDTO>>> GetUsers()
        {
            var usuarios = await _context.Users
                .Select(u => new UserResponseDTO
                {
                    Id = u.Id,
                    Alias = u.Alias,
                    Email = u.Email,
                    CreatedAt = u.CreatedAt,
                    LastSignIn = u.LastSignIn,
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        // GET user searched by alias or email (GET /api/users/search?search=)
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<UserPreviewDTO>>> SearchUser([FromQuery] string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return BadRequest(new { message = "Search string can not be empty." });

            if (search.Trim().Length < 3)
                return BadRequest(new { message = "At least 3 characters are required." });

            var users = await _context.Users
                .Where(u => u.Alias.Contains(search) || u.Email.Contains(search))
                .Select(u => new UserPreviewDTO
                {
                    Id = u.Id,
                    Alias = u.Alias,
                    Email = u.Email
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET user by id (GET /api/users/{userId})
        [HttpGet("{userId}")]
        public async Task<ActionResult<UserResponseDTO>> GetUserById(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return new UserResponseDTO
            {
                Id = user.Id,
                Alias = user.Alias,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                LastSignIn = user.LastSignIn,
            };
        }

        // GET all the public links of an user with id (GET /api/users/{userId}/links)
        [HttpGet("{userId}/links")]
        public async Task<ActionResult<IEnumerable<LinkPreviewDTO>>> GetAllPublicUserLinks(int userId)
        {
            // Ensure the user exists
            if (!await _context.Users.AnyAsync(u => u.Id == userId))
                return NotFound(new { message = "User does not exists." });

            int? currentVisitorId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;

            // Get all user' links
            var publicLinks = await _context.Links
                .Where(l => l.OwnerId == userId && !l.IsPrivate)
                .Select(l => new LinkPreviewDTO
                {
                    Id = l.Id,
                    Title = l.Title,
                    Description = l.Description,
                    IsPrivate = l.IsPrivate,
                    OwnerId = l.OwnerId,
                    Categories = l.LinkCategories
                        .Select(lc => lc.Category)
                        // Privacy, get only the public categories
                        .Where(c => !c.IsPrivate || (currentVisitorId != null && c.OwnerId == currentVisitorId))
                        .Select(c => new CategoryResponseDTO
                        {
                            Id = c.Id,
                            Name = c.Name,
                            HexColor = c.HexColor,
                            IsPrivate = c.IsPrivate,
                            OwnerId = c.OwnerId
                        }).ToList()
                })
                .ToListAsync();

            return Ok(publicLinks);
        }

        // GET all the public categories of an user with id (GET /api/users/{userId}/categories)
        [HttpGet("{userId}/categories")]
        public async Task<ActionResult<IEnumerable<CategoryResponseDTO>>> GetAllPublicUserCategories(int userId)
        {
            // Check if the user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found." });

            // Get all user' categories
            var userCategories = await _context.Categories
                .Where(c => c.OwnerId == userId && !c.IsPrivate)
                .Select(c => new CategoryResponseDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    HexColor = c.HexColor,
                    IsPrivate = c.IsPrivate,
                    OwnerId = c.OwnerId
                })
                .ToListAsync();

            return Ok(userCategories);
        }
        #endregion

        #region POST
        // POST to create a new user (POST /api/users)
        [HttpPost]
        public async Task<ActionResult<UserResponseDTO>> CreateUser(UserCreateDTO newUser)
        {
            var hashedPassword = _passwordHasher.HashPassword(newUser.Password);
            // Transform the DTO to a real User Model of the DDBB
            var userEntity = new User
            {
                Alias = newUser.Alias,
                Email = newUser.Email,
                Password = hashedPassword
            };

            // Send to DDBB
            _context.Users.Add(userEntity);
            await _context.SaveChangesAsync();

            // Transform the new user to a secure DTO to return it
            var responseDTO = new UserResponseDTO
            {
                Id = userEntity.Id,
                Alias = userEntity.Alias,
                Email = userEntity.Email,
                CreatedAt = userEntity.CreatedAt,
                LastSignIn = userEntity.LastSignIn,
            };

            // Return a 201 code (created) and user's secure data
            return CreatedAtAction(nameof(GetUserById), new { userId = userEntity.Id }, responseDTO);
        }
        #endregion

        #region PUT
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserUpdateDTO updateData)
        {
            var userId = User.GetUserId();
            // Get the user from the DDBB
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            // If this flag is true, then there are changes
            bool hasChanges = false;

            if (!string.IsNullOrWhiteSpace(updateData.Alias) && !string.Equals(user.Alias, updateData.Alias))
            {
                bool exists = await _context.Users.AnyAsync(u => u.Alias == updateData.Alias);
                if (exists) return BadRequest(new { message = "Alias already exists." });
                user.Alias = updateData.Alias;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(updateData.Email) && !string.Equals(user.Email, updateData.Email))
            {
                bool exists = await _context.Users.AnyAsync(u => u.Email == updateData.Email);
                if (exists) return BadRequest(new { message = "Email already exists." });
                user.Email = updateData.Email;
                hasChanges = true;
            }

            // If any change, update the database
            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }

            // Return Ok with the updated data
            return Ok(new
            {
                message = "Profile updated successfully.",
                user = new
                {
                    id = userId,
                    alias = user.Alias,
                    email = user.Email
                }
            });
        }
        #endregion

        #region Login
        // POST for login verification (POST /api/users/login)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginAttempt)
        {
            if (string.IsNullOrWhiteSpace(loginAttempt.Alias) && string.IsNullOrWhiteSpace(loginAttempt.Email))
                return BadRequest(new { message = "An alias or email is required for login." });

            // Build the query manually so it is more optimized and surely uses the DB indexes
            IQueryable<User> query = _context.Users;

            if (!string.IsNullOrWhiteSpace(loginAttempt.Email))
                query = query.Where(u => u.Email == loginAttempt.Email);
            else if (!string.IsNullOrWhiteSpace(loginAttempt.Alias))
                query = query.Where(u => u.Alias == loginAttempt.Alias);

            var user = await query.FirstOrDefaultAsync();

            // Make sure if the user exists
            if (user == null) return Unauthorized(new { message = "Invalid credentials." });

            // If the user exists, verify the password with the hashed one
            if (!_passwordHasher.VerifyPassword(loginAttempt.Password, user.Password)) return Unauthorized(new { message = "Invalid credentials." });

            // If the password is correct, update the last login date
            user.LastSignIn = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate the token
            var jwtToken = _tokenService.CreateToken(user);

            return Ok(new
            {
                message = "Correct Login.",
                token = jwtToken,
                userData = new
                {
                    id = user.Id,
                    alias = user.Alias,
                    email = user.Email,
                    lastLogin = user.LastSignIn,
                }
            });
        }
        #endregion
    }
}
