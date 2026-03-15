using Course.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Course.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Course.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private ApiDbContext _dbContext;
        public OrdersController(ApiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: api/orders/admin?pageNumber=1&pageSize=5&status=Pending&startDate&endDate&user

        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<IActionResult> GetAllOrdersForAdmins(
            int pageNumber = 1,
            int pageSize = 5,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? user = null)
        {
            var query = _dbContext.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(x => x.Status == status);

            if (startDate.HasValue)
                query = query.Where(x => x.OrderDate >= startDate);

            if (endDate.HasValue)
                query = query.Where(x => x.OrderDate <= endDate);

            if (!string.IsNullOrEmpty(user))
                query = query.Where(x => x.User.Name.Contains(user) || x.User.Email.Contains(user));

            var orders = await query.OrderByDescending(x => x.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                Id = x.Id,
                UserName = x.User.Name,
                OrderDate = x.OrderDate,
                TotalAmount = x.TotalAmount,
                Status = x.Status,
                Address = x.Address
            })
            .ToListAsync();
            return Ok(orders);
        }

        // GET: api/orders/admin/pending
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/pending")]
        public async Task<IActionResult> GetPenndingOrdersForAdmin(int pageNumber = 1,
            int pageSize = 5)
        {
            var pendingOrders = await _dbContext.Orders.Where(x => x.Status == "Pending")
                .OrderByDescending(x => x.OrderDate)
                .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                Id = x.Id,
                UserName = x.User.Name,
                OrderDate = x.OrderDate,
                TotalAmount = x.TotalAmount,
                Status = x.Status,
                Address = x.Address
            })
            .ToListAsync();
            return Ok(pendingOrders);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{orderId:int}/admindetails")]
        public async Task<IActionResult> GetOrderDetailsForAdmin(int orderId)
        {
            var orderDetails = await _dbContext.OrderDetails
                .Where(x => x.OrderId == orderId)
                .Include(x => x.Product)
                .Select(x => new
                {
                    Id = x.Id,
                    Qty = x.Qty,
                    TotalAmount = x.TotalAmount,
                    ProductName = x.Product.Name,
                    ProductImageUrl = x.Product.ImageUrl,
                    ProductPrice = x.Product.Price
                })
                .ToListAsync();
            return Ok(orderDetails);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{orderId:int}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromQuery] string orderStatus)
        {
            if (!orderStatus.Equals("completed") && !orderStatus.Equals("cancelled"))
            {
                return BadRequest("Ivalid status");
            }

            var order = await _dbContext.Orders.FindAsync(orderId);
            if(order == null)
            {
                return NotFound("Order Not Found");
            }
            order.Status = orderStatus;
            await _dbContext.SaveChangesAsync();

            return Ok("Order status updated");
        }

        [HttpGet("my")]
        public async Task<IActionResult> Get()
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                return Unauthorized();
            }
            var userOrders = await _dbContext.Orders
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.OrderDate)
                .Select(x =>
                new
                {
                    x.Id,
                    x.TotalAmount,
                    x.OrderDate
                }).ToListAsync();
            return Ok(userOrders);
        }

        ////api/orders/{orderId}/details
        [HttpGet("{orderId:int}/details")]
        public async Task<IActionResult> GetOrderDetailfForUser(int orderId)
        {
            var orderDetails = await _dbContext.OrderDetails
                .Where(x => x.OrderId == orderId)
                .Include(x => x.Product)
                .Select(x => new
                {
                    Id = x.Id,
                    Qty = x.Qty,
                    TotalAmount = x.TotalAmount,
                    ProductName = x.Product.Name,
                    ProductImageUrl = x.Product.ImageUrl,
                    ProductPrice = x.Product.Price
                }).ToListAsync();
            return Ok(orderDetails);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            await _dbContext.Orders.AddAsync(order);
            await _dbContext.SaveChangesAsync();

            var carts = await _dbContext.ShoppingCartItems.Where(x => x.UserId == order.UserId).ToListAsync();

            order.OrderDate = DateTime.UtcNow;
            order.Status = "Pending";

            order.TotalAmount = carts.Sum(x => x.TotalAmount);

            foreach (var item in carts)
            {
                var orderDetails = new OrderDetail
                {
                    UnitPrice = item.UnitPrice,
                    TotalAmount = item.TotalAmount,
                    Qty = item.Qty,
                    ProductId = item.ProductId,
                    OrderId = order.Id
                };
                await _dbContext.OrderDetails.AddAsync(orderDetails);
            }
            await _dbContext.SaveChangesAsync();
            _dbContext.ShoppingCartItems.RemoveRange(carts);
            await _dbContext.SaveChangesAsync();
            return Ok("Your order has been placed. Your order Id is " + order.Id);
        }
    }
}
