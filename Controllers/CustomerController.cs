using Azure;
using HighThroughputApi.Dtos;
using HighThroughputApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HighThroughputApi.Helpers;
using HighThroughputApi.Interfaces;

namespace HighThroughputApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICustomerRepository _customerRepository;

        public CustomerController(AppDbContext context,ICustomerRepository customerRepository)
        {
            _context = context;
            _customerRepository = customerRepository;   
        }

        [HttpPost("get-customer")]
        public async Task<ActionResult<CreateCustomerDto>> GetCustomer([FromBody] CustomerDto dto)
        {
            //var customer = await _context.Customers.Where(c => c.Email == dto.Email).FirstOrDefaultAsync();
            var customer = await _customerRepository.GetByEmailAsync(dto.Email);
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
                FirstName = dto.FirstName,
                LastName = dto.LastName,
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
