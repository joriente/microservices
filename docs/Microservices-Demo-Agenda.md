# 30-45 Minute Microservices Architecture Demo Agenda

## Part 1: Architecture Overview (5-7 minutes)

### Visual Tour of Solution Structure
- **Show the solution in VS Code** - Highlight the clean separation:
  - `src/Services/` - 6 independent microservices (Product, Order, Cart, Customer, Inventory, Payment)
  - `src/Gateway/` - API Gateway (Yarp reverse proxy)
  - `frontend/` - Blazor WebAssembly UI
  - `src/Aspire/` - Orchestration layer
  - `src/Shared/` - Shared contracts (events only)

### Key Architecture Principles
- **Microservices Independence**: Each service has its own database (OrderService→SQL Server, InventoryService→PostgreSQL, ProductService→SQL Server, CartService→MongoDB)
- **Domain-Driven Design**: Clean Architecture with Domain/Application/Infrastructure layers where applicable
- **Event-Driven Communication**: RabbitMQ for async messaging between services
- **API Gateway Pattern**: Single entry point for frontend, routes to backend services

## Part 2: Live Service Orchestration (5-8 minutes)

### Start the System
```powershell
.\Start-all.ps1
```

### Aspire Dashboard Walkthrough
- **Show running containers**: PostgreSQL, MongoDB, SQL Server, RabbitMQ, Seq
- **Management UIs**: Demonstrate pgAdmin, Mongo Express, RabbitMQ Management
- **Service Health**: Show all 6 microservices + gateway + frontend running
- **Explain Aspire's Role**: Service discovery, configuration, observability, container orchestration

### Point Out Key Features
- "Notice each service runs independently on different ports"
- "If one service crashes, others continue running"
- "Aspire handles connection strings, service references automatically"

## Part 3: End-to-End User Flow (10-12 minutes)

### Customer Journey
1. **Browse Products** (ProductService)
   - Navigate to product catalog
   - Show products loaded from ProductService database
   - Point out: "This calls API Gateway → ProductService → SQL Server"

2. **Add to Cart** (CartService)
   - Add 2-3 products to cart
   - Show cart persisting in MongoDB
   - Point out: "CartService is stateful, stores in MongoDB for session management"

3. **Place Order** (Demonstrates Event Chain)
   - Go through checkout process
   - **Stop here and explain what happens behind the scenes**:
   
   ```
   OrderService receives order → Publishes OrderCreatedEvent
   ↓
   InventoryService reserves stock (PostgreSQL)
   ↓
   PaymentService processes payment (Stripe API)
   ↓
   Publishes PaymentProcessedEvent
   ↓
   InventoryService commits reservation
   ↓
   OrderService updates status to "Processing"
   ```

4. **View Order Status**
   - Show order in "Processing" state
   - Refresh to see real-time updates

## Part 4: Event-Driven Architecture Deep Dive (8-10 minutes)

### RabbitMQ Management Console
- Open RabbitMQ Management (from Aspire dashboard)
- **Show Exchanges**: Point out event types (ProductCreatedEvent, OrderCreatedEvent, PaymentProcessedEvent, etc.)
- **Show Queues**: Demonstrate service-specific queues:
  - `inventory-service-order-created`
  - `inventory-service-payment-processed`
  - `product-service-order-created`

### Live Event Flow Demonstration
1. **Create a New Product** (Admin page)
   - Open admin product management
   - Create product with initial stock (e.g., "Demo Widget", 50 units)
   
2. **Watch the Event Chain** in RabbitMQ:
   - ProductService publishes `ProductCreatedEvent`
   - InventoryService consumes it → Initializes inventory record
   - Switch to pgAdmin → Show new inventory row created

3. **Place an Order** for the new product
   - Watch events in RabbitMQ:
     - `OrderCreatedEvent` → InventoryService reserves stock
     - `PaymentProcessedEvent` → InventoryService commits reservation
   - Show in pgAdmin: Reserved quantity changes, then committed

### Code Walkthrough (Optional - use selected code)
- **Show PaymentProcessedEventConsumer.cs**:
  ```csharp
  // Find all reservations for this order
  var reservations = await _context.InventoryReservations
      .Where(x => x.OrderId == message.OrderId && x.Status == ReservationStatus.Reserved)
      .ToListAsync();
  ```
- Explain: "When payment succeeds, this consumer commits the inventory by fulfilling reservations"
- Point out: "This is async, decoupled - PaymentService doesn't know about InventoryService"

## Part 5: Observability & Monitoring (5-7 minutes)

### Seq Centralized Logging
- Open Seq dashboard from Aspire
- **Filter by service**: Show logs from specific microservices
- **Search for events**: Search "PaymentProcessedEvent" to see event processing
- **Trace an order**: Search by OrderId to see full journey across services:
  ```
  OrderService: Order created
  → InventoryService: Inventory reserved
  → PaymentService: Payment processed
  → InventoryService: Reservation fulfilled
  → OrderService: Order status updated
  ```

### Demonstrate Failure Handling
- **Insufficient Inventory Scenario**:
  1. Check current stock in inventory management (e.g., 2 units left)
  2. Try to order 10 units
  3. Show in Seq: `InventoryReservationFailedEvent` published
  4. Order creation fails gracefully with clear error message

## Part 6: Database Per Service Pattern (3-5 minutes)

### Show Database Independence
1. **PostgreSQL (InventoryService)** - pgAdmin
   - Show `inventory_items` and `inventory_reservations` tables
   - Point out EF Core migrations folder

2. **MongoDB (CartService)** - Mongo Express
   - Show cart documents with embedded items
   - Explain: "NoSQL for flexible cart schemas"

3. **SQL Server (OrderService, ProductService)** - SQL Server Management Studio or Azure Data Studio
   - Show relational order tables with foreign keys
   - Explain: "Strong consistency for transactional data"

### Key Points
- "Each service owns its data - no direct database sharing"
- "Services communicate only through APIs and events"
- "Choose the right database for each service's needs"

## Part 7: Scaling & Deployment Discussion (2-3 minutes)

### Explain Scalability
- "Each service can scale independently"
- "High order volume? Scale OrderService without touching ProductService"
- "RabbitMQ queues buffer during traffic spikes"

### Deployment Story
- "Aspire simplifies local development"
- "For production: Deploy to Azure Container Apps or Kubernetes"
- "Each service has its own CI/CD pipeline"
- "Zero-downtime deployments per service"

## Bonus Topics (If Time Permits)

### API Gateway Features
- Show Yarp configuration in `appsettings.json`
- Demonstrate route transformation
- JWT authentication at gateway level

### Admin Features
- Inventory adjustment with audit trail
- Product management with stock sync
- Order management and status updates

### Recent Improvements
- "We just migrated InventoryService from MongoDB to PostgreSQL"
- "All event naming standardized with 'Event' suffix"
- "Integrated Seq for centralized logging with Serilog"

---

## Key Talking Points Throughout

1. **Loose Coupling**: Services don't know about each other, only events
2. **Fault Tolerance**: One service failure doesn't crash the system
3. **Technology Diversity**: Different databases, different patterns per service
4. **Observability**: Centralized logging and monitoring across distributed system
5. **Event Sourcing**: Audit trail of all business events
6. **Scalability**: Scale services independently based on load
7. **Team Autonomy**: Different teams can own different services
8. **Modern Tooling**: .NET 9, Aspire, RabbitMQ, Seq, Docker

## Demo Tips

- **Prepare beforehand**: Run `.\Start-all.ps1` before the demo, verify all services healthy
- **Have data ready**: Create a few products with varying stock levels
- **Use split screen**: Show code + running app + RabbitMQ/Seq simultaneously
- **Tell a story**: Follow a single order through the entire system
- **Show failures**: Demonstrate error handling (insufficient stock, payment failure with bad card)
- **Keep RabbitMQ Management open**: Visual representation of events flowing
- **Use Seq filters**: Quickly trace specific order or event types

This agenda gives you flexibility to go deeper or shallower based on audience technical level and time constraints.
