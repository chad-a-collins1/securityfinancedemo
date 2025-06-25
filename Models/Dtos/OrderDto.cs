namespace HighThroughputApi.Models.Dtos
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string Etag { get; init; } = "";
        public List<OrderItemDto> OrderItems { get; set; } = new();
    }
}
