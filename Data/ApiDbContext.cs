using Course.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Course.Data
{
    public class ApiDbContext(DbContextOptions<ApiDbContext> options) : IdentityDbContext<User>(options)
    {


        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Product>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Name).HasMaxLength(255);
                e.Property(p => p.Description).HasMaxLength(255);
                e.Property(p => p.ImageUrl).HasMaxLength(255);

            });



        }
    }
}




/*
 Refresh Token (как в production)
Access Token — живёт 5–15 минут
Refresh Token — живёт 7–30 дней
когда access истёк → клиент отправляет refresh → получает новый access
Шаг 1. Модель RefreshToken
public class RefreshToken
{
public int Id { get; set; }
public string Token { get; set; }
public DateTime Expires { get; set; }
public int UserId { get; set; }
}Добавить в DbContext.

Шаг 2. Генерация Refresh Token
private string GenerateRefreshToken()
{
var randomBytes = new byte[64];

using var rng = RandomNumberGenerator.Create();
rng.GetBytes(randomBytes);

return Convert.ToBase64String(randomBytes);
}

Шаг 3. Возвращать 2 токена при Login
var refreshToken = GenerateRefreshToken()
_dbContext.RefreshTokens.Add(new RefreshToken
{
Token = refreshToken,
UserId = currentUser.Id,
Expires = DateTime.Now.AddDays(30)
});
await _dbContext.SaveChangesAsync();
return Ok(new
{
access_token = jwt,
refresh_token = refreshToken
});

Шаг 4. Endpoint обновления токена
[HttpPost("refresh")]
public async Task<IActionResult> Refresh(string refreshToken)
{
var token = await _dbContext.RefreshTokens
    .FirstOrDefaultAsync(x => x.Token == refreshToken);

if (token == null || token.Expires < DateTime.Now)
    return Unauthorized();

var user = await _dbContext.Users.FindAsync(token.UserId);

var newJwt = GenerateJwt(user);

return Ok(new
{
    access_token = newJwt
});
}

*Получать UserId автоматически
Самый удобный способ — extension method.
Создай файл:
Extensions/UserExtensions.cs
using System.Security.Claims;

public static class UserExtensions
{
public static int GetUserId(this ClaimsPrincipal user)
{
    return int.Parse(user.FindFirst(ClaimTypes.NameIdentifier).Value);
}
}

Теперь в любом контроллере:
var userId = User.GetUserId();

Глобальная авторизация на весь API Чтобы не писать [Authorize] на каждом контроллере.
В Program.cs:
builder.Services.AddControllers(options =>
{
var policy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

options.Filters.Add(new AuthorizeFilter(policy));
});
Теперь весь API защищён.
Если endpoint должен быть публичным Используй:
[AllowAnonymous]

Например:
[AllowAnonymous]
[HttpPost("login")]
public IActionResult Login()

*Правильная архитектура JWT В production обычно:
Controllers
Services
Repositories
Models
DTO
Extensions

JWT логика лежит в:
Services/JwtService.cs

*Лучшие настройки токена
Access token: 10–15 минут
Refresh token: 7–30 дней
Можно получить email из токена:
User.FindFirst(ClaimTypes.Email)?.Value
или
User.Identity.Name

показать очень мощную production схему JWT, которую используют почти все проекты:
Архитектура
AuthController
JwtService
RefreshTokenService
UserService
и:
rotation refresh token
revoke token
logout
blacklist токенов*/

