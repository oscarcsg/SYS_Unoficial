using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.JsonWebTokens;
using StoreYourStuffAPI.Data;
using StoreYourStuffAPI.DTOs.Category;
using StoreYourStuffAPI.DTOs.Link;
using StoreYourStuffAPI.DTOs.User;
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

        #region GET USERS
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
        public async Task<ActionResult<IEnumerable<UserSearchDTO>>> SearchUser([FromQuery] string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return BadRequest(new { message = "Search string can not be empty." });

            if (search.Trim().Length < 3)
                return BadRequest(new { message = "At least 3 characters are required." });

            var users = await _context.Users
                .Where(u => u.Alias.Contains(search) || u.Email.Contains(search))
                .Select(u => new UserSearchDTO
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
        public async Task<ActionResult<IEnumerable<LinkResponseDTO>>> GetAllPublicUserLinks(int userId)
        {
            // Ensure the user exists
            if (!await _context.Users.AnyAsync(u => u.Id == userId))
                return NotFound(new { message = "User does not exists." });

            // Get all user' links
            var publicLinks = await _context.Links
                .Where(l => l.OwnerId == userId && !l.IsPrivate)
                .Select(l => new LinkResponseDTO
                {
                    Id = l.Id,
                    Title = l.Title,
                    Description = l.Description,
                    Url = l.Url,
                    IsPrivate = l.IsPrivate,
                    OwnerId = l.OwnerId,
                    CreatedAt = l.CreatedAt,

                    // SubSelect
                    Categories = l.LinkCategories
                        .Select(lc => lc.Category) // Go to the Category table
                        .Where(c =>
                            // Only the categories which owner is the system (null) or the same owner
                            // than the link and is public
                            (c.OwnerId == l.OwnerId || c.OwnerId == null) && !c.IsPrivate
                        )
                        .Select(c => new CategoryResponseDTO
                        {
                            Id = c.Id,
                            Name = c.Name,
                            HexColor = c.HexColor,
                            IsPrivate = c.IsPrivate,
                            OwnerId = c.OwnerId
                        })
                        .ToList() // Create a list for the Links DTO
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

        #region POST USERS
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

            // 3. Transformamos el nuevo usuario a un DTO seguro para devolverlo
            var responseDTO = new UserResponseDTO
            {
                Id = userEntity.Id,
                Alias = userEntity.Alias,
                Email = userEntity.Email,
                CreatedAt = userEntity.CreatedAt,
                LastSignIn = userEntity.LastSignIn,
            };

            // Devuelve un código 201 (Creado) y los datos seguros del usuario
            return CreatedAtAction(nameof(GetUserById), new { userId = userEntity.Id }, responseDTO);
        }
        #endregion

        #region PUT USERS
        #endregion

        #region Login
        // POST for login verification (POST /api/users/login)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginAttempt)
        {
            if (string.IsNullOrWhiteSpace(loginAttempt.Alias) && string.IsNullOrWhiteSpace(loginAttempt.Email))
                return BadRequest(new { message = "An alias or email is required for login." });

            // Search the data of the user depending on the data given by the form
            var user = await _context.Users.FirstOrDefaultAsync(u => 
                (!string.IsNullOrWhiteSpace(loginAttempt.Alias) && u.Alias == loginAttempt.Alias) ||
                (!string.IsNullOrWhiteSpace(loginAttempt.Email) && u.Email == loginAttempt.Email)
            );

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
                    links = user.Links
                }
            });
        }
        #endregion
    }
}
