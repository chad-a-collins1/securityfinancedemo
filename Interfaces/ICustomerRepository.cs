using HighThroughputApi.Models;

namespace HighThroughputApi.Interfaces
{
    public interface ICustomerRepository : IBaseRepository<Customer>
    {
        Task<Customer?> GetByEmailAsync(string email);
    }
}
