namespace HighThroughputApi.Models.Dtos;
public record OrderItemDto(int OrderItemId, int ItemId, int Quantity, string Name);