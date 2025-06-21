namespace HighThroughputApi.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Stock { get; set; }
        public decimal Price { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}