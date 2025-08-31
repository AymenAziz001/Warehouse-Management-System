// Wms.Infrastructure.Tests/Services/StockMovementServiceTests.cs

using Microsoft.Extensions.Logging;
using Wms.Domain.Entities;
using Wms.Domain.Enums;
using Wms.Domain.ValueObjects;
using Wms.Infrastructure.Data;
using Wms.Infrastructure.Repositories;
using Wms.Infrastructure.Services;

namespace Wms.Infrastructure.Tests.Services;

public class StockMovementServiceTests : IDisposable
{
    private readonly WmsDbContext _context;
    private readonly Item _item;
    private readonly Location _location;
    private readonly StockMovementService _service;
    private readonly Warehouse _warehouse;

    public StockMovementServiceTests()
    {
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new WmsDbContext(options);
        _context.Database.EnsureCreated();

        var mockLogger = new Mock<ILogger<StockMovementService>>();
        var unitOfWork = new UnitOfWork(_context);
        _service = new StockMovementService(unitOfWork, mockLogger.Object);

        // Setup test data
        _warehouse = new Warehouse("TEST", "Test Warehouse");
        _item = new Item("WIDGET-001", "Widget A", "EA");
        _location = new Location("Z001", "Zone 1", _warehouse.Id);

        _context.Warehouses.Add(_warehouse);
        _context.Items.Add(_item);
        _context.Locations.Add(_location);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task ReceiveAsync_WithNewStock_CreatesStockAndMovement()
    {
        // Arrange
        var quantity = new Quantity(10.0m);
        var userId = "USER1";

        // Act
        var movement = await _service.ReceiveAsync(_item.Id, _location.Id, quantity, userId);
        await _context.SaveChangesAsync(); // Save changes to persist data

        // Assert
        movement.Should().NotBeNull();
        movement.Type.Should().Be(MovementType.Receipt);
        movement.ItemId.Should().Be(_item.Id);
        movement.ToLocationId.Should().Be(_location.Id);
        movement.Quantity.Should().Be(quantity);
        movement.UserId.Should().Be(userId);

        // Check stock was created
        var stock = await _context.Stock.FirstOrDefaultAsync(s => s.ItemId == _item.Id && s.LocationId == _location.Id);
        stock.Should().NotBeNull();
        stock!.QuantityAvailable.Should().Be(quantity);
    }

    [Fact]
    public async Task ReceiveAsync_WithExistingStock_UpdatesStockQuantity()
    {
        // Arrange
        var initialQuantity = new Quantity(5.0m);
        var additionalQuantity = new Quantity(3.0m);
        var expectedTotal = new Quantity(8.0m);

        var existingStock = new Stock(_item.Id, _location.Id, initialQuantity);
        _context.Stock.Add(existingStock);
        await _context.SaveChangesAsync();

        // Act
        var movement = await _service.ReceiveAsync(_item.Id, _location.Id, additionalQuantity, "USER1");

        // Assert
        var stock = await _context.Stock.FirstOrDefaultAsync(s => s.ItemId == _item.Id && s.LocationId == _location.Id);
        stock!.QuantityAvailable.Should().Be(expectedTotal);
    }

    [Fact]
    public async Task PutawayAsync_WithSufficientStock_MovesStockBetweenLocations()
    {
        // Arrange
        var fromLocation = _location;
        var toLocation = new Location("Z002", "Zone 2", _warehouse.Id);
        _context.Locations.Add(toLocation);
        await _context.SaveChangesAsync();

        var initialStock = new Stock(_item.Id, fromLocation.Id, new Quantity(10.0m));
        _context.Stock.Add(initialStock);
        await _context.SaveChangesAsync();

        var moveQuantity = new Quantity(4.0m);

        // Act
        var movement = await _service.PutawayAsync(_item.Id, fromLocation.Id, toLocation.Id, moveQuantity, "USER1");
        await _context.SaveChangesAsync(); // Save changes to persist data

        // Assert
        movement.Type.Should().Be(MovementType.Putaway);
        movement.FromLocationId.Should().Be(fromLocation.Id);
        movement.ToLocationId.Should().Be(toLocation.Id);

        var fromStock =
            await _context.Stock.FirstOrDefaultAsync(s => s.ItemId == _item.Id && s.LocationId == fromLocation.Id);
        var toStock =
            await _context.Stock.FirstOrDefaultAsync(s => s.ItemId == _item.Id && s.LocationId == toLocation.Id);

        fromStock!.QuantityAvailable.Value.Should().Be(6.0m); // 10 - 4
        toStock!.QuantityAvailable.Value.Should().Be(4.0m);
    }

    [Fact]
    public async Task PutawayAsync_WithInsufficientStock_ThrowsInvalidOperationException()
    {
        // Arrange
        var fromLocation = _location;
        var toLocation = new Location("Z002", "Zone 2", _warehouse.Id);
        _context.Locations.Add(toLocation);
        await _context.SaveChangesAsync();

        var initialStock = new Stock(_item.Id, fromLocation.Id, new Quantity(3.0m));
        _context.Stock.Add(initialStock);
        await _context.SaveChangesAsync();

        var moveQuantity = new Quantity(5.0m); // More than available

        // Act & Assert
        var act = async () =>
            await _service.PutawayAsync(_item.Id, fromLocation.Id, toLocation.Id, moveQuantity, "USER1");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*insufficient*");
    }

    [Fact]
    public async Task PickAsync_WithSufficientStock_ReducesStockQuantity()
    {
        // Arrange
        var initialStock = new Stock(_item.Id, _location.Id, new Quantity(10.0m));
        _context.Stock.Add(initialStock);
        await _context.SaveChangesAsync();

        var pickQuantity = new Quantity(3.0m);

        // Act
        var movement = await _service.PickAsync(_item.Id, _location.Id, pickQuantity, "USER1");

        // Assert
        movement.Type.Should().Be(MovementType.Pick);
        movement.FromLocationId.Should().Be(_location.Id);
        movement.ToLocationId.Should().BeNull();

        var stock = await _context.Stock.FirstOrDefaultAsync(s => s.ItemId == _item.Id && s.LocationId == _location.Id);
        stock!.QuantityAvailable.Value.Should().Be(7.0m); // 10 - 3
    }

    [Fact]
    public async Task AdjustAsync_WithNewQuantity_AdjustsStockToSpecifiedAmount()
    {
        // Arrange
        var initialStock = new Stock(_item.Id, _location.Id, new Quantity(8.0m));
        _context.Stock.Add(initialStock);
        await _context.SaveChangesAsync();

        var newQuantity = new Quantity(12.0m);
        var reason = "Physical count adjustment";

        // Act
        var movement = await _service.AdjustAsync(_item.Id, _location.Id, newQuantity, "USER1", reason);
        await _context.SaveChangesAsync(); // Save changes to persist data

        // Assert
        movement.Type.Should().Be(MovementType.Adjustment);
        movement.ItemId.Should().Be(_item.Id);
        movement.ToLocationId.Should().Be(_location.Id);
        movement.Quantity.Should().Be(newQuantity); // Adjustment movement shows the new quantity
        movement.Notes.Should().Be(reason);

        var stock = await _context.Stock.FirstOrDefaultAsync(s => s.ItemId == _item.Id && s.LocationId == _location.Id);
        stock!.QuantityAvailable.Should().Be(newQuantity);
    }

    [Fact]
    public async Task AdjustAsync_WithLowerQuantity_CreatesNegativeAdjustmentMovement()
    {
        // Arrange
        var initialStock = new Stock(_item.Id, _location.Id, new Quantity(10.0m));
        _context.Stock.Add(initialStock);
        await _context.SaveChangesAsync();

        var newQuantity = new Quantity(6.0m);
        var reason = "Damage write-off";

        // Act
        var movement = await _service.AdjustAsync(_item.Id, _location.Id, newQuantity, "USER1", reason);
        await _context.SaveChangesAsync(); // Save changes to persist data

        // Assert
        movement.Quantity.Should().Be(newQuantity); // Adjustment movement shows the new quantity, not the difference

        var stock = await _context.Stock.FirstOrDefaultAsync(s => s.ItemId == _item.Id && s.LocationId == _location.Id);
        stock!.QuantityAvailable.Should().Be(newQuantity);
    }

    [Fact]
    public async Task ReceiveAsync_WithLotNumber_CreatesLotAndAssociatesWithStock()
    {
        // Arrange
        var quantity = new Quantity(5.0m);
        var lotId = 1; // Assuming lot exists

        // Act
        var movement = await _service.ReceiveAsync(_item.Id, _location.Id, quantity, "USER1", lotId);
        await _context.SaveChangesAsync(); // Save changes to persist data

        // Assert
        movement.LotId.Should().Be(lotId);

        var stock = await _context.Stock.FirstOrDefaultAsync(s =>
            s.ItemId == _item.Id && s.LocationId == _location.Id && s.LotId == lotId);
        stock.Should().NotBeNull();
        stock!.LotId.Should().Be(lotId);
    }

    [Fact]
    public async Task ReceiveAsync_WithSerialNumber_AssociatesSerialWithStock()
    {
        // Arrange
        var quantity = new Quantity(1.0m);
        var serialNumber = "SN-12345";

        // Act
        var movement =
            await _service.ReceiveAsync(_item.Id, _location.Id, quantity, "USER1", serialNumber: serialNumber);
        await _context.SaveChangesAsync(); // Save changes to persist data

        // Assert
        movement.SerialNumber.Should().Be(serialNumber);

        var stock = await _context.Stock.FirstOrDefaultAsync(s =>
            s.ItemId == _item.Id && s.LocationId == _location.Id && s.SerialNumber == serialNumber);
        stock.Should().NotBeNull();
        stock!.SerialNumber.Should().Be(serialNumber);
    }

    [Fact]
    public async Task AllMovementOperations_ShouldPersistMovementHistory()
    {
        // Arrange
        var quantity = new Quantity(10.0m);

        // Act - Perform various operations
        await _service.ReceiveAsync(_item.Id, _location.Id, quantity, "USER1", referenceNumber: "PO-001");
        await _service.AdjustAsync(_item.Id, _location.Id, new Quantity(15.0m), "USER1", "Count adjustment");
        await _context.SaveChangesAsync(); // Save changes to persist data

        // Assert
        var movements = await _context.Movements.Where(m => m.ItemId == _item.Id).ToListAsync();
        movements.Should().HaveCount(2);

        var receiptMovement = movements.First(m => m.Type == MovementType.Receipt);
        receiptMovement.ReferenceNumber.Should().Be("PO-001");

        var adjustmentMovement = movements.First(m => m.Type == MovementType.Adjustment);
        adjustmentMovement.Notes.Should().Be("Count adjustment");
    }
}