// Wms.Application/UseCases/Receiving/ReceiveItemUseCase.cs

using Microsoft.Extensions.Logging;
using Wms.Application.Common;
using Wms.Application.DTOs;
using Wms.Domain.Entities;
using Wms.Domain.Repositories;
using Wms.Domain.Services;
using Wms.Domain.ValueObjects;

namespace Wms.Application.UseCases.Receiving;

public interface IReceiveItemUseCase
{
    Task<Result<ReceiptResultDto>> ExecuteAsync(ReceiveItemDto request, string userId,
        CancellationToken cancellationToken = default);
}

public class ReceiveItemUseCase : IReceiveItemUseCase
{
    private readonly ILogger<ReceiveItemUseCase> _logger;
    private readonly IStockMovementService _stockMovementService;
    private readonly IUnitOfWork _unitOfWork;

    public ReceiveItemUseCase(IUnitOfWork unitOfWork, IStockMovementService stockMovementService,
        ILogger<ReceiveItemUseCase> logger)
    {
        _unitOfWork = unitOfWork;
        _stockMovementService = stockMovementService;
        _logger = logger;
    }

    public async Task<Result<ReceiptResultDto>> ExecuteAsync(ReceiveItemDto request, string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate item exists
            var item = await _unitOfWork.Items.GetBySkuAsync(request.ItemSku, cancellationToken);
            if (item == null)
                return Result.Failure<ReceiptResultDto>($"Item with SKU '{request.ItemSku}' not found");

            if (!item.IsActive)
                return Result.Failure<ReceiptResultDto>($"Item '{request.ItemSku}' is inactive");

            // Validate location exists and is receivable
            var location = await _unitOfWork.Locations.GetByCodeAsync(request.LocationCode, cancellationToken);
            if (location == null)
                return Result.Failure<ReceiptResultDto>($"Location '{request.LocationCode}' not found");

            if (!location.IsReceivable)
                return Result.Failure<ReceiptResultDto>($"Location '{request.LocationCode}' is not receivable");

            if (!location.IsActive)
                return Result.Failure<ReceiptResultDto>($"Location '{request.LocationCode}' is inactive");

            // Handle lot creation if required
            int? lotId = null;
            if (item.RequiresLot && !string.IsNullOrWhiteSpace(request.LotNumber))
            {
                var existingLot = await GetOrCreateLotAsync(item.Id, request.LotNumber,
                    request.ExpiryDate, request.ManufacturedDate, cancellationToken);
                lotId = existingLot.Id;
            }
            else if (item.RequiresLot)
            {
                return Result.Failure<ReceiptResultDto>($"Item '{request.ItemSku}' requires a lot number");
            }

            // Validate serial number requirement
            if (item.RequiresSerial && string.IsNullOrWhiteSpace(request.SerialNumber))
                return Result.Failure<ReceiptResultDto>($"Item '{request.ItemSku}' requires a serial number");

            // Create the receipt movement
            var quantity = new Quantity(request.Quantity);
            var movement = await _stockMovementService.ReceiveAsync(
                item.Id, location.Id, quantity, userId, lotId,
                request.SerialNumber, request.ReferenceNumber, request.Notes, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Item {ItemSku} received: {Quantity} to {LocationCode} by {UserId}",
                request.ItemSku, request.Quantity, request.LocationCode, userId);

            return Result.Success(new ReceiptResultDto(
                movement.Id,
                request.ItemSku,
                request.LocationCode,
                request.Quantity,
                request.LotNumber,
                movement.Timestamp
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving item {ItemSku}", request.ItemSku);
            return Result.Failure<ReceiptResultDto>($"Error receiving item: {ex.Message}");
        }
    }

    private Task<Lot> GetOrCreateLotAsync(int itemId, string lotNumber,
        DateTime? expiryDate, DateTime? manufacturedDate, CancellationToken cancellationToken)
    {
        // For now, create a simple lot lookup - in full implementation this would be a proper repository method
        var lot = new Lot(lotNumber, itemId, expiryDate, manufacturedDate);
        // This would typically check if lot exists first, but keeping simple for MVP
        return Task.FromResult(lot);
    }
}