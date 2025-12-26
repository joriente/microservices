# Product Ordering System - Data Seeder

A standalone console application for seeding development and demo data into the microservices system with Azure Service Bus event publishing.

## Features

- ✅ **Configurable**: Control what gets seeded via `appsettings.json`
- ✅ **Fast**: Direct database insertion (no HTTP overhead)
- ✅ **Event Publishing**: Publishes events to Azure Service Bus for downstream services
- ✅ **Safe**: Checks existing data before seeding
- ✅ **Flexible**: Can clear existing data or preserve it
- ✅ **Standalone**: Run independently of the main application
- ✅ **Generalized**: Seeds multiple services from one tool
- ✅ **Aspire Integration**: Runs automatically when starting via Aspire

## Usage

### Run via Aspire (Recommended for Development)

When you run the solution via Aspire (`dotnet run --project src/Aspire/ProductOrderingSystem.AppHost`), the DataSeeder will automatically run and seed data before the API services start. The seeder is configured in the AppHost to:
1. Wait for MongoDB and Azure Service Bus to be ready
2. Seed data and publish events
3. Exit (API services then start with pre-seeded data)

### Run from Command Line

```powershell
# From the DataSeeder project directory
cd src/Tools/ProductOrderingSystem.DataSeeder
dotnet run

# Or from the solution root
dotnet run --project src/Tools/ProductOrderingSystem.DataSeeder/ProductOrderingSystem.DataSeeder.csproj
```

### Configuration

Edit `appsettings.json` to control seeding behavior:

```json
{
  "Seeding": {
    "Enabled": true,
    "ClearExistingData": false,
    "PublishEvents": true,
    "Services": {
      "ProductService": {
        "Enabled": true,
        "ProductCount": 100,
        "PublishEvents": true
      },
      "IdentityService": {
        "Enabled": true,
        "CreateAdminUser": true,
        "CreateShopperUser": true,
        "PublishEvents": true
      }
    }
  },
  "ConnectionStrings": {
    "messaging": "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true",
    "MongoDB": "mongodb://localhost:27017"
  },
  "MongoDB": {
    "ProductDatabaseName": "productdb",
    "IdentityDatabaseName": "identitydb"
  }
}
```

### Options

#### Global Seeding Options
- **`Seeding:Enabled`**: Master switch to enable/disable all seeding
- **`Seeding:ClearExistingData`**: If `true`, deletes existing data before seeding
- **`Seeding:PublishEvents`**: Global event publishing toggle (can be overridden per service)

#### Product Service Options
- **`ProductService:Enabled`**: Enable/disable product seeding
- **`ProductService:ProductCount`**: Number of products to generate (default: 100)
- **`ProductService:PublishEvents`**: Publish `ProductCreatedEvent` for each product

#### Identity Service Options
- **`IdentityService:Enabled`**: Enable/disable identity seeding
- **`IdentityService:CreateAdminUser`**: Create admin user with role "Admin"
- **`IdentityService:CreateShopperUser`**: Create test shopper user
- **`IdentityService:PublishEvents`**: Publish `UserCreatedEvent` for each user

### Event Publishing

When `PublishEvents` is enabled, the seeder publishes events to Azure Service Bus:

- **ProductCreatedEvent**: Published for each product inserted
- **UserCreatedEvent**: Published for each user inserted

These events are received by downstream services (e.g., CartService, InventoryService) just like runtime application events.

**Requirements:**
- Azure Service Bus emulator must be running
- Connection string must be configured in `appsettings.json`
- MassTransit handles event serialization and publishing

### When to Use

Run the data seeder:
- ✅ After fresh deployment
- ✅ When setting up a new development environment
- ✅ Before demos or presentations
- ✅ After clearing/resetting databases
- ✅ When you need specific test data
- ✅ To trigger event flows across services

### ProductService Background Seeder

The ProductService has a built-in background seeder that is **disabled by default**. 

To enable it, set in `appsettings.Development.json`:
```json
{
  "Seeding": {
    "Enabled": true
  }
}
```

**Recommendation**: Use this standalone DataSeeder project instead for better control, performance, and event publishing.

## What Gets Seeded

### Product Service
- 100 realistic products across 8 categories:
  - Electronics (laptops, headphones, smart devices)
  - Clothing (shirts, jeans, shoes, accessories)
  - Books (fiction, non-fiction, technical)
  - Home & Garden (furniture, decor, plants)
  - Sports (equipment, apparel, accessories)
  - Toys (games, puzzles, educational)
  - Food (coffee, tea, snacks, gourmet)
  - Health & Beauty (skincare, supplements, cosmetics)
- Each product has:
  - Realistic names and descriptions
  - Category-appropriate pricing ($4.99 - $999.99)
  - Random stock quantities (10-200 units)
  - Unsplash placeholder images

**Events Published**: `ProductCreatedEvent` per product (if enabled)

### Identity Service
- **Admin User**:
  - Username: `admin`
  - Password: `P@ssw0rd`
  - Email: `admin@productordering.com`
  - Role: `Admin`
- **Shopper User**:
  - Username: `shopper`
  - Password: `P@ssw0rd`
  - Email: `steve.hopper@email.com`
  - Role: (none - regular user)

**Events Published**: `UserCreatedEvent` per user (if enabled)

## Technical Details

- **Direct Database Access**: 
  - MongoDB for ProductService and IdentityService
  - Bypasses HTTP/Wolverine for faster bulk insertion
- **Optimized Seeding**:
  - Bulk insert all products first (`InsertManyAsync`)
  - Then publish events separately with error handling
  - Ensures all data is seeded even if event publishing fails
- **Event Publishing**:
  - MassTransit with Azure Service Bus
  - Same event contracts as runtime application
  - Events flow to consuming services (CartService, InventoryService, CustomerService, etc.)
  - Resilient: continues even if individual event publishing fails
- **Bogus Library**: Generates realistic fake data
- **Idempotent**: Safe to run multiple times (checks existing data)
- **Logging**: Detailed progress, event publishing, and status information

## Architecture

```
DataSeeder
├── Infrastructure/
│   ├── IEventPublisher - Event publishing interface
│   ├── EventPublisher - Azure Service Bus publisher via MassTransit
│   └── NoOpEventPublisher - Disabled publishing fallback
├── Seeders/
│   ├── DataSeederRunner - Orchestrates all seeders
│   ├── ProductSeeder - Seeds products + publishes ProductCreatedEvent
│   └── IdentitySeeder - Seeds users + publishes UserCreatedEvent
└── Program.cs - DI setup and execution
```

## Troubleshooting

**"MongoDB connection failed"**
- Ensure MongoDB is running (check Aspire dashboard or Docker)
- Verify the connection string in `appsettings.json`
- Check database names in `MongoDB` configuration section

**"Azure Service Bus connection failed"**
- Ensure Azure Service Bus emulator is running
- Verify connection string matches emulator configuration
- Check that `messaging` connection string uses `UseDevelopmentEmulator=true`

**"Products already exist"**
- Set `"ClearExistingData": true` to replace existing data
- Or manually clear MongoDB: `docker exec -it <mongo-container> mongosh productdb --eval "db.products.drop()"`

**"No products visible in app"**
- Check that the application is connecting to the same MongoDB instance
- Verify database name matches between seeder and application
- Check MongoDB Express (port 8081) to inspect data

**"Events not being received"**
- Verify `PublishEvents` is enabled in configuration
- Check Azure Service Bus emulator is running and accessible
- Review service logs to confirm event consumption
- Use Service Bus Explorer to inspect queues/topics

## Adding New Seeders

To add a seeder for a new service:

1. Create a new seeder class in `Seeders/` folder
2. Implement seeding logic with direct database access
3. Publish relevant events using `IEventPublisher`
4. Register seeder in `Program.cs` DI container
5. Add configuration options to `appsettings.json`
6. Update `DataSeederRunner` to call your seeder
7. Update this README with details
