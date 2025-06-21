namespace HighThroughputApi.Models
{
    public class OrderItem
    {
        public int Id { get; set; } 

        public int OrderId { get; set; }  
        public Order Order { get; set; } = null!;

        public int ItemId { get; set; }  
        public Item Item { get; set; } = null!;

        public int Quantity { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    }

}
