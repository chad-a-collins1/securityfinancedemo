using HighThroughputApi.Dtos;
using HighThroughputApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HighThroughputApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomerController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("Email")]
        public async Task<ActionResult<CreateCustomerDto>> GetCustomer([FromBody] CustomerDto dto)
        {
            var customer = await _context.Customers.Where(c => c.Email == dto.Email).FirstOrDefaultAsync();
            if (customer == null)
                return NotFound();

            return new CreateCustomerDto
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email
            };
        }

        [HttpPost]
        public async Task<ActionResult<CreateCustomerDto>> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            var customer = new Customer
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, new CreateCustomerDto
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email
            });
        }

        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<CreateCustomerDto>>> GetAllCustomers()
        //{
        //    var customers = await _context.Customers.ToListAsync();

        //    return customers
        //        .Select(c => new CreateCustomerDto
        //        {
        //            Id = c.Id,
        //            FirstName = c.FirstName,
        //            LastName = c.LastName,
        //            Email = c.Email
        //        })
        //        .ToList();
        //}

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound();

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
