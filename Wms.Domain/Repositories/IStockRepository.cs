// Wms.Domain/Repositories/IStockRepository.cs

using Wms.Domain.Entities;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Repositories;

public interface IStockRepository : IRepository<Stock>
{
    Task<IEnumerable<Stock>> GetByItemIdAsync(int itemId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Stock>> GetByLocationIdAsync(int locationId, CancellationToken cancellationToken = default);

    Task<Stock?> GetByItemAndLocationAsync(int itemId, int locationId, int? lotId = null,
        string? serialNumber = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<Stock>> GetAvailableStockAsync(int itemId, CancellationToken cancellationToken = default);
    Task<Quantity> GetTotalQuantityAsync(int itemId, CancellationToken cancellationToken = default);
}