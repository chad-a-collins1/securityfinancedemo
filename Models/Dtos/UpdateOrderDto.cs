using System.Collections.ObjectModel;

namespace HighThroughputApi.Models.Dtos;

public record UpdateOrderDto(Collection<OrderItemDto> OrderItems);