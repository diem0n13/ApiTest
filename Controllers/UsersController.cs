using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Course.Data;
using Course.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Course.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private ApiDbContext _dbContext;
        private IConfiguration _configuration;
        public UsersController(ApiDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (existingUser != null)
            {
                return BadRequest("User with same email exist..");
            }

            var passHasher = new PasswordHasher<User>();
            user.PasswordHash = passHasher.HashPassword(user, user.PasswordHash);

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var currentUser = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
            if (currentUser == null)
            {
                return NotFound("User not found.");
            }
            var passHasher = new PasswordHasher<User>();
            var result = passHasher.VerifyHashedPassword(currentUser, currentUser.PasswordHash, request.Password);
            if (result != PasswordVerificationResult.Success)
            {
                return NotFound("Invalid password.");
            }
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, request.Email),
                //new Claim(ClaimTypes.NameIdentifier, currentUser.Id.ToString())
                new Claim(ClaimTypes.Role, currentUser.Role)
            };
            var token = new JwtSecurityToken(issuer: _configuration["Jwt:Issuer"], audience: _configuration["Jwt:Audience"], claims: claims,
                expires: DateTime.Now.AddDays(60), signingCredentials: credentials);
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return new ObjectResult(new
            {
                access_token = jwt,
                token_type = "bearer",
                user_id = currentUser.Id,
                user_name = currentUser.Name
            });
        }

        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
    }
}