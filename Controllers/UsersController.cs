using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreYourStuffAPI.Data;
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
        #endregion

        #region Constructors
        // Dependences injection: program will give automaticalle the DDBB translator to this controller
        public UsersController(AppDbContext context, IPasswordHasher passwordHasher) {
            _context = context;
            _passwordHasher = passwordHasher;
        }
        #endregion

        #region GET
        // Petition to get every user
        // GET api/users
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
        #endregion

        #region POST
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
            return CreatedAtAction(nameof(GetUsers), new { id = userEntity.Id }, responseDTO);
        }
        #endregion
    }
}
