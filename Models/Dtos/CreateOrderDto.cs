namespace HighThroughputApi.Models.Dtos;

public record CreateOrderDto(int CustomerId, List<OrderItemDto> OrderItems);