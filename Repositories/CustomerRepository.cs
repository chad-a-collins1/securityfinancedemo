using HighThroughputApi.Interfaces;
using HighThroughputApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HighThroughputApi.Repositories
{
    public class CustomerRepository : BaseRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(AppDbContext context) : base(context) { }

        public async Task<Customer?> GetByEmailAsync(string email) =>
            await _dbSet.FirstOrDefaultAsync(c => c.Email == email);
    }
}
