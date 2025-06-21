using StackExchange.Redis;

namespace HighThroughputApi.Models
{
    public class Customer
    {
        public int Id { get; set; } 
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; } = string.Empty;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    }
}
