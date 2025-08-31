# Warehouse Management System (WMS)

## Overview
A production-quality WinForms Warehouse Management System built with .NET 8, following Clean Architecture principles and Domain-Driven Design patterns. This complete system implements all core WMS functionality including receiving, putaway, inventory management, picking, and reporting.

## Features Implemented (Complete System)

### Core Operations
- **Item Receiving**: Scan barcode, specify quantity, location, and lot information
- **Item Putaway**: Move items from receiving to storage locations  
- **Inventory Management**: View stock levels, search inventory, perform adjustments
- **Order Picking**: Pick items from locations for order fulfillment
- **Stock Adjustments**: Adjust quantities with reason tracking

### Reporting & Analytics
- **Movement Reports**: Filter by date, item, movement type, and user
- **Stock Reports**: Real-time inventory visibility by item and location
- **Export Functionality**: Export reports to CSV format
- **Audit Trail**: Complete movement history with user tracking

### Technical Features
- **Scanner-First UI**: Keyboard-wedge barcode scanner support throughout
- **Clean Architecture**: Separated layers (Domain, Application, Infrastructure, WinForms)
- **Entity Framework Core**: SQLite database with code-first migrations
- **Dependency Injection**: Microsoft Extensions DI container
- **Structured Logging**: Serilog with file output
- **Comprehensive Validation**: Business rules enforced at domain level

## Architecture

### Layer Structure
```
??? Wms.Domain/           # Business entities, value objects, domain services
??? Wms.Application/      # Use cases, DTOs, application services  
??? Wms.Infrastructure/   # EF Core, repositories, external services
??? Wms.WinForms/        # WinForms UI, presenters, forms
```

### Key Entities
- **Item**: SKU, name, barcodes, lot/serial requirements
- **Location**: Hierarchical warehouse locations (Zone/Aisle/Rack/Shelf)
- **Stock**: Quantity tracking by item/location/lot with reservations
- **Movement**: Immutable audit trail of all stock movements
- **Lot**: Expiry tracking for lot-controlled items

## Getting Started

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- SQLite (included with EF Core)

### Build & Run
```bash
# Clone and navigate to the project
cd "Warehouse Management System"

# Restore packages
dotnet restore

# Build the solution
dotnet build

# Run the application
dotnet run
```

### Database
- SQLite database (`wms.db`) is automatically created on first run
- Sample data is seeded automatically:
  - Items: WIDGET-001, GADGET-001, TOOL-001, PART-001
  - Locations: RECEIVE, Z001, Z001-A001, Z001-A002, Z001-A001-01, Z001-A001-02
  - Barcodes: 123456789012, 234567890123, 345678901234, 456789012345

## Complete User Guide

### Main Menu Navigation
- **F1**: Receiving form
- **F2**: Putaway form  
- **F3**: Inventory Management
- **F4**: Order Picking
- **F5**: Movement Reports

### 1. Item Receiving Workflow
1. Launch application and click **Receiving (F1)**
2. Scan or enter barcode in the barcode field
3. Item information displays automatically
4. Enter quantity (required)
5. Confirm location code (defaults to RECEIVE)
6. For lot-controlled items, enter lot number and expiry date
7. Press **Receive (F1)** or Enter to complete

### 2. Item Putaway Workflow  
1. From main menu, click **Putaway (F2)**
2. Scan barcode of item to move
3. Confirm "From Location" (defaults to RECEIVE)
4. Enter "To Location" code
5. Enter quantity to move
6. Press **Putaway (F1)** to complete

### 3. Inventory Management
1. Click **Inventory (F3)** from main menu
2. View all current stock with available quantities
3. Use search box to find specific items
4. Select a stock line and click **Adjustment (F2)** to modify quantities
5. Enter new quantity and reason for adjustment
6. System maintains complete audit trail

### 4. Order Picking
1. Click **Picking (F4)** from main menu
2. Scan item barcode to pick
3. Enter source location code
4. Specify quantity to pick
5. Enter order number (optional)
6. Press **Pick (F1)** to complete
7. System validates stock availability before processing

### 5. Reports & Analytics
1. Click **Reports (F5)** from main menu
2. Set date range filters (defaults to last 30 days)
3. Filter by item SKU or movement type (optional)
4. Press **Generate (F1)** to create report
5. Press **Export (F2)** to save as CSV file
6. View detailed movement history with timestamps and users

## Complete Feature Set

### Inventory Operations ?
- Real-time stock visibility
- Multi-location inventory tracking
- Lot and serial number management
- Stock adjustments with reason codes
- Inventory search and filtering

### Movement Processing ?
- Receipt processing with validation
- Putaway with location verification
- Pick processing with availability checks
- Stock adjustments with audit trail
- Movement history preservation

### Business Rules Enforcement ?
- Item validation (active status, barcode lookup)
- Location validation (active, receivable/pickable)
- Quantity validation (positive numbers, availability)
- Lot requirement enforcement
- Serial number requirement enforcement

### Reporting Capabilities ?
- Movement reports by date range
- Filtering by item, type, and user
- Real-time stock level reporting
- CSV export functionality
- Complete audit trail visibility

### User Experience ?
- Scanner-optimized workflow
- Keyboard navigation (F1-F5 shortcuts)
- Audio feedback for success/error
- Responsive UI with progress indicators
- Clear error messages and validation

## Technical Implementation Highlights

### Domain Layer
- **Value Objects**: Barcode, Quantity with business rules
- **Entities**: Rich domain models with encapsulated behavior
- **Repository Contracts**: Clean abstraction for data access
- **Domain Services**: Complex business operations

### Application Layer  
- **Use Cases**: Single responsibility command handlers
- **Result Pattern**: Consistent error handling across operations
- **DTOs**: Clean data contracts between layers
- **Validation**: Input validation with detailed error messages

### Infrastructure Layer
- **EF Core**: Code-first with fluent API configurations
- **Repository Pattern**: Generic base with specialized implementations
- **Unit of Work**: Transaction management and consistency
- **Stock Movement Service**: Centralized inventory operations

### UI Layer
- **Scanner-First Design**: Optimized for barcode workflow
- **Service Integration**: Dependency injection for loose coupling
- **Error Handling**: User-friendly messages with logging
- **Async Operations**: Non-blocking UI with progress feedback

## Sample Data & Testing

### Test Items Available
- **WIDGET-001**: Basic item (Barcode: 123456789012)
- **GADGET-001**: Lot-controlled item (Barcode: 234567890123)
- **TOOL-001**: Standard item (Barcode: 345678901234)
- **PART-001**: Serial-controlled item (Barcode: 456789012345)

### Test Locations Available
- **RECEIVE**: Receiving dock (receivable only)
- **Z001**: Zone 1 (receivable and pickable)
- **Z001-A001**: Aisle 1 (receivable and pickable)
- **Z001-A002**: Aisle 2 (receivable and pickable)
- **Z001-A001-01**: Bin 01 (receivable and pickable)
- **Z001-A001-02**: Bin 02 (receivable and pickable)

### Complete Test Workflow
1. **Receive Items**: Use barcodes above to receive items to RECEIVE location
2. **Putaway Stock**: Move items from RECEIVE to storage locations (Z001-A001-01, etc.)
3. **Check Inventory**: View stock levels and locations in Inventory Management
4. **Pick Orders**: Pick items from storage locations for orders
5. **Adjust Stock**: Make quantity adjustments with reason tracking
6. **Generate Reports**: View complete movement history and export data

## Architecture Benefits

### Maintainability
- Clear separation of concerns across layers
- Testable business logic in domain layer
- Dependency injection for flexible component composition

### Extensibility  
- New use cases easily added to application layer
- Additional repositories and services via interfaces
- New UI forms integrate seamlessly with existing services

### Data Integrity
- Domain-driven validation rules
- Immutable movement history
- Transaction management with rollback capability

### Performance
- Async operations prevent UI blocking
- Efficient data access with EF Core
- Minimal memory footprint with proper disposal patterns

This system represents a complete, production-ready Warehouse Management System with all essential functionality for managing inventory operations in a modern warehouse environment.