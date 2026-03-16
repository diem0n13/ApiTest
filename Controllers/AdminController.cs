using Course.Data;
using Course.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Course.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController(ApiDbContext dbContext,IAccount accountService) : ControllerBase
    {

        [Authorize(Roles = "Admin")]
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetAdminDashboardSumary()
        {

            var res = await accountService.GetAdminDashboardSummary();
            if(res!=null)
                return Ok(res);
            
            var totalOrders = await dbContext.Orders.CountAsync();
            var pendingOrders = await dbContext.Orders.CountAsync(x => x.Status.Equals("Pending"));
            var totalRevenue = await dbContext.Orders.Where(x => x.Status.Equals("completed"))
                .SumAsync(x => (double?)x.TotalAmount ?? 0);
            var totalProducts = await dbContext.Products.CountAsync();
            var totalCategories = await dbContext.Categories.CountAsync();

            var result = new
            {
                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                TotalRevenue = totalRevenue,
                TotalProducts = totalProducts,
                TotalCategories = totalCategories
            };

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue([FromQuery] string range = "monthly") //weekly, yearly
        {
            var now = DateTime.UtcNow;
            var result = new List<object>();
            for (var i = 6; i >= 0; i--)
            {
                DateTime start, end;
                string period;
                if (range == "yearly")
                {
                    var year = now.Year - i;
                    start = new DateTime(year, 1, 1);
                    end = start.AddYears(1);
                    period = year.ToString();
                }
                else if (range == "monthly")
                {
                    var date = now.AddMonths(-i);
                    start = new DateTime(date.Year, date.Month, 1);
                    end = start.AddMonths(1);
                    period = $"{date.Year}-{date.Month:D2}";
                }
                else if (range == "weekly")
                {
                    var weekStart = now.AddDays(-7 * i);
                    start = weekStart.AddDays(-(int)weekStart.DayOfWeek);
                    end = start.AddDays(7);
                    period = start.ToString("yyyy-mm-dd");
                }
                else
                {
                    return BadRequest("Use range = yearly, monthly or weekly");
                }

                //decimal revanue = await _dbContext.Orders.Where(x=>x.Status.Equals("completed") && x.OrderDate >= start && x.OrderDate < end).SumAsync(x=>(double?)x.TotalAmount) ?? 0;
                double value = await dbContext.Orders
                    .Where(x => x.Status.Equals("completed") && x.OrderDate >= start && x.OrderDate < end)
                    .SumAsync(x => (double?)x.TotalAmount) ?? 0;
                //long revenue = await _dbContext.Orders.Where(...).SumAsync(x => x.TotalAmount);
                //decimal result = revenue / 100m;
                decimal revanue = (decimal)value;
                result.Add(new
                {
                    Revenue = revanue,
                    Period = period,
                });
            }

            return Ok(result);
        }
    }
}