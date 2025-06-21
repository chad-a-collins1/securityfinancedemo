using Azure;
using HighThroughputApi.Dtos;
using HighThroughputApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HighThroughputApi.Helpers;

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

        [HttpPost("get-customer")]
        public async Task<ActionResult<CreateCustomerDto>> GetCustomer([FromBody] CustomerDto dto)
        {
            var customer = await _context.Customers.Where(c => c.Email == dto.Email).FirstOrDefaultAsync();
            if (customer == null)
                return NotFound();


            return new CreateCustomerDto
            {
                Id = customer.Id,
                Email = customer.Email
            };
        }

        [HttpPost]
        public async Task<ActionResult<CreateCustomerDto>> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            var customer = new Customer
            {
                Email = dto.Email
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, new CreateCustomerDto
            {
                Id = customer.Id,
                Email = customer.Email
            });
        }

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
