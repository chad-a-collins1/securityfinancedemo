using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HighThroughputApi.Models;
using HighThroughputApi.Dtos;
using Microsoft.EntityFrameworkCore;
using HighThroughputApi.Interfaces;

namespace HighThroughputApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ICustomerRepository _customerRepository;

        public AuthController(AppDbContext context, IConfiguration configuration, ICustomerRepository customerRepository)
        {
            _context = context;
            _config = configuration;
            _customerRepository = customerRepository;   
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] CustomerDto dto)
        {

            var customer = await _customerRepository.GetByEmailAsync(dto.Email);

            if (customer == null)
                return NotFound("Customer not found.");

            var token = GenerateJwtToken(customer.Email, customer.Id);

            return Ok(new
            {
                token,
                customer = new CreateCustomerDto
                {
                    Id = customer.Id,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Email = customer.Email
                }
            });
        }

        private string GenerateJwtToken(string email, int customerId)
        {
            var claims = new[]
            {
                        new Claim(ClaimTypes.NameIdentifier, customerId.ToString()),
                        new Claim(ClaimTypes.Email, email)
                    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
