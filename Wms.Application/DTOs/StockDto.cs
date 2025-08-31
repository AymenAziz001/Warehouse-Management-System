// Wms.Application/DTOs/StockDto.cs

namespace Wms.Application.DTOs;

public record StockDto(
    int Id,
    int ItemId,
    string ItemSku,
    string ItemName,
    int LocationId,
    string LocationCode,
    string LocationName,
    int? LotId,
    string? LotNumber,
    string? SerialNumber,
    decimal QuantityAvailable,
    decimal QuantityReserved,
    decimal AvailableQuantity,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record StockSummaryDto(
    string ItemSku,
    string ItemName,
    decimal TotalQuantity,
    decimal TotalReserved,
    decimal TotalAvailable,
    int LocationCount
);