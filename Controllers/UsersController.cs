using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreYourStuffAPI.Data;
using StoreYourStuffAPI.Models;

namespace StoreYourStuffAPI.Controllers
{
    [Route("api/[controller]")] // Defines the API route
    [ApiController]
    public class UsersController : ControllerBase
    {
        #region Attributes
        private readonly AppDbContext _context;
        #endregion

        #region Constructors
        // Dependences injection: program will give automaticalle the DDBB translator to this controller
        public UsersController(AppDbContext context) { _context = context; }
        #endregion

        #region Methods
        // Petition to get every user
        // GET api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var usuarios = await _context.Users.ToListAsync();
            return Ok(usuarios);
        }
        #endregion
    }
}
