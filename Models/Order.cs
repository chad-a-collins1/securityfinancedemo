namespace HighThroughputApi.Models
{
    public class Order
    {
        public int Id { get; set; }  
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public int CustomerId { get; set; } 
        public Customer Customer { get; set; } = null!;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

}
