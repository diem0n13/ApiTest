using Course.Data;
using Course.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Course.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private ApiDbContext _dbContext;
        public CategoriesController(ApiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: api/<CategoriesController>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var categories = await _dbContext.Categories.ToListAsync();
            return Ok(categories);
        }

        // POST api/<CategoriesController>
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Category category)
        {
            if(category == null)
            {
                return BadRequest("Category is null");
            }
            await _dbContext.Categories.AddAsync(category);
            await _dbContext.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created);
        }

        // PUT api/<CategoriesController>/5
        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Category category)
        {
            var existPRODUCT = await _dbContext.Categories.FindAsync(id);
            if (existPRODUCT == null)
                return NotFound();

            existPRODUCT.Name = category.Name;
            await _dbContext.SaveChangesAsync();
            return Ok("Record updated ...");
        }

        // DELETE api/<CategoriesController>/5
        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existPRODUCT = await _dbContext.Categories.FindAsync(id);
            if (existPRODUCT == null)
                return NotFound();
            _dbContext.Categories.Remove(existPRODUCT);
            await _dbContext.SaveChangesAsync();
            return Ok("Record deleted");
        }
    }
}
