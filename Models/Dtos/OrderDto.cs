namespace HighThroughputApi.Models.Dtos
{
    public class OrderDto
    {
        public int Id { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    }    
}
