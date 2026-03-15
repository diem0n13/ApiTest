using System.Security.Claims;
using Course.Data;
using Course.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Course.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingCartController : ControllerBase
    {
        private ApiDbContext _dbContext;
        public ShoppingCartController(ApiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        //api/shoppingcart
        [HttpGet]
        public async Task<IActionResult> Get() 
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                return Unauthorized();
            }

            var cartItems = await _dbContext.ShoppingCartItems
                .Where(s=>s.UserId==user.Id)
                .Include(s=>s.Product)
                .Select(s => new 
                {
                Id=s.Id,
                Qty=s.Qty,
                UnitPrice = s.UnitPrice,
                TotalAmount = s.TotalAmount,
                ProductId = s.ProductId,
                ProductName = s.Product.Name,
                ImageUrl = s.Product.ImageUrl
                }).ToListAsync();
            return Ok(cartItems);
        }

        //api/shoppingcart/add
        [HttpPost("add")]
        public async Task<IActionResult> Post([FromBody] ShoppingCartItem shoppingCartItem)
        {
            var existCart = await _dbContext.ShoppingCartItems.FirstOrDefaultAsync(x => x.ProductId == shoppingCartItem.ProductId && x.UserId == shoppingCartItem.UserId);
            if (existCart != null)
            {
                existCart.Qty += shoppingCartItem.Qty;
                existCart.TotalAmount = shoppingCartItem.TotalAmount * shoppingCartItem.Qty;
            }
            else
            {
                var productCart = await _dbContext.Products.FindAsync(shoppingCartItem.ProductId);
                var newCart = new ShoppingCartItem
                {
                    UserId = shoppingCartItem.UserId,
                    ProductId = shoppingCartItem.ProductId,
                    Qty = shoppingCartItem.Qty,
                    UnitPrice = productCart.Price,
                    TotalAmount = productCart.Price * shoppingCartItem.Qty
                };
                await _dbContext.ShoppingCartItems.AddAsync(newCart);    
            }
            
            await _dbContext.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created);
        }

        //api/shoppingcart?productId=1&action=increase
        [HttpPut]
        public async Task<IActionResult> Put([FromQuery]int productId, [FromQuery]string action)
        {
            var userEmail = User.Claims.FirstOrDefault(c=>c.Type==ClaimTypes.Email)?.Value;
            var user = await _dbContext.Users.FirstOrDefaultAsync(u=>u.Email==userEmail);
            if(user == null) 
            {
                return Unauthorized();
            }
            var cart = await _dbContext.ShoppingCartItems.FirstOrDefaultAsync(s=>s.ProductId==productId&&s.UserId==user.Id);
            if(cart == null)
            {
                return NotFound("Product not found");
            }
            switch (action.ToLower())
            {
                case"increase":
                    cart.Qty += 1;
                    break;
                case "decrease":
                    if(cart.Qty > 1)
                    cart.Qty -= 1;
                    else
                    _dbContext.ShoppingCartItems.Remove(cart);
                    break;
                default:
                    return BadRequest("Invalide action");
            }
            cart.TotalAmount = cart.UnitPrice * cart.Qty;
            await _dbContext.SaveChangesAsync();
            return Ok("Shopping cart was updated");
        }

        //api/shoppingcart/remove/1
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> Delete(int productId) 
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                return Unauthorized();
            }
            var cart = await _dbContext.ShoppingCartItems.FirstOrDefaultAsync(s => s.ProductId == productId && s.UserId == user.Id);
            if (cart == null)
            {
                return NotFound("Product not found");
            }
            _dbContext.ShoppingCartItems.Remove(cart);
            await _dbContext.SaveChangesAsync();
            return Ok("Shopping cart was removed");
        }
    }
}
