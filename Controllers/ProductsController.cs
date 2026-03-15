using Course.Models;
using Microsoft.AspNetCore.Mvc;
using Course.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Course.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {

        private ApiDbContext _dbContext;
        public ProductsController(ApiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: api/<ProductsController>?search=string
        // GET: api/<ProductsController>?pageNumber=1&pageSize=5
        // GET: api/<ProductsController>?categoryId=1&material=qwer&gender=qwer
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string search, [FromQuery] int? categoryId, [FromQuery] string material, [FromQuery] string gender, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            var query = _dbContext.Products.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.Name.ToLower().Contains(search.ToLower())  || x.Description.ToLower().Contains(search.ToLower()));
            }
            if (!string.IsNullOrEmpty(material))
            {
                query = query.Where(x => x.Material.ToLower() == material.ToLower());
            }
            if (!string.IsNullOrEmpty(gender))
            {
                query = query.Where(x => x.Gender.ToLower() == gender.ToLower());
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(x => x.Price >= maxPrice);
            }
            if (minPrice.HasValue)
            {
                query = query.Where(x => x.Price <= minPrice);
            }
            if (categoryId.HasValue)
            {
                query = query.Where(x => x.CategoryId <= categoryId);
            }
            var result = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return Ok(result);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (product == null)
            {
                NotFound();
            }
            return Ok(product);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public async Task<IActionResult> Post([FromForm] Product product)
        {
            var guid = Guid.NewGuid();
            var filePath = Path.Combine("wwwroot", guid + ".jpeg");
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await product.Image.CopyToAsync(fileStream);
            }
            product.ImageUrl = filePath.Substring(8);
            if (product == null)
            {
                BadRequest("Product is null");
            }
            await _dbContext.Products.AddAsync(product);
            await _dbContext.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromForm] Product product)
        {
            var existPRODUCT = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (existPRODUCT != null)
            {
                existPRODUCT.Name = product.Name;
                existPRODUCT.Description = product.Description;
                existPRODUCT.Price = product.Price;
                existPRODUCT.CategoryId = product.CategoryId;
                if (existPRODUCT.Image == null)
                {
                    if (!string.IsNullOrEmpty(existPRODUCT.ImageUrl))
                    {
                        var oldImagePath = Path.Combine("wwwroot", existPRODUCT.ImageUrl);
                        if (System.IO.File.Exists(oldImagePath))
                            System.IO.File.Delete(oldImagePath);
                    }
                    var guid = Guid.NewGuid();
                    var filePath = Path.Combine("wwwroot", guid + ".jpeg");
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await product.Image.CopyToAsync(fileStream);
                    }
                    existPRODUCT.ImageUrl = filePath.Substring(8);
                }
                await _dbContext.SaveChangesAsync();
                return Ok("Record updated ...");
            }
            return NotFound();
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existPRODUCT = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (existPRODUCT != null)
            {
                if (!string.IsNullOrEmpty(existPRODUCT.ImageUrl))
                {
                    var oldImagePath = Path.Combine("wwwroot", existPRODUCT.ImageUrl);
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);
                }
                _dbContext.Products.Remove(existPRODUCT);
                await _dbContext.SaveChangesAsync();
                return Ok("Record deleted");
            }
            return NotFound();
        }
    }
}
