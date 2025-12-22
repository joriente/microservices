---
tags:
  - planning
  - demo
  - presentation
  - agenda
---
# 30-45 Minute Microservices Architecture Demo Agenda

## Part 0: Git Workflows & Security (10 minutes)

### GitHub Repository Setup
Walk through the repository structure and collaboration workflows:

**Branching Strategy:**
- `main` - Production-ready code
- `develop` - Integration branch
- Feature branches: `feature/analytics-service`, `feature/notification-service`
- Demonstrate pull request workflow

### GitHub CodeQL Security Scanning
Show how to enable and use CodeQL for automated security analysis:

**Enable CodeQL:**
1. Navigate to repository **Settings** → **Security** → **Code security and analysis**
2. Enable **CodeQL analysis**
3. Configure for C#, Java, and JavaScript

**CodeQL Features Demonstration:**
- Show security alerts in **Security** tab
- Review detected vulnerabilities (SQL injection, XSS, insecure dependencies)
- Demonstrate how to fix flagged issues
- Show security trends over time

**Key Benefits:**
- Automated security scanning on every push/PR
- Identifies vulnerabilities before production
- Supports multiple languages (C#, Java, JavaScript in our stack)
- Integration with GitHub Advanced Security

**Demo Tip:** Show a recent CodeQL alert and walk through the remediation process. Point out how it integrates into the PR workflow.

## Part 1: Development Environment Setup (5 minutes)

### VS Code Setup & Extensions
Walk through the recommended development environment:

**Essential Tools:**
- Visual Studio Code (primary IDE)
- .NET 10 SDK
- Java 21 JDK (for NotificationService)
- Apache Maven
- Docker Desktop
- Git

**Key VS Code Extensions** (see [vscode-extension.list](../vscode-extension.list) for full list):

*C# Development:*
- C# Dev Kit (`ms-dotnettools.csdevkit`)
- C# (`ms-dotnettools.csharp`)
- IntelliCode for C# (`ms-dotnettools.vscodeintellicode-csharp`)
- .NET Core Test Explorer (`formulahendry.dotnet-test-explorer`)
- Blazor Snippet Pack (`adrianwilczynski.blazor-snippet-pack`)
- MudBlazor Snippets (`mukul.mudblazor-snippets`)

*Azure & Cloud:*
- Azure Developer CLI (`ms-azuretools.azure-dev`)
- Azure GitHub Copilot (`ms-azuretools.vscode-azure-github-copilot`)
- Docker (`ms-azuretools.vscode-docker`)
- Kubernetes Tools (`ms-kubernetes-tools.vscode-kubernetes-tools`)

*Database Tools:*
- SQL Server (mssql) (`ms-mssql.mssql`)
- SQL Database Projects (`ms-mssql.sql-database-projects-vscode`)
- Azure Cosmos DB (`ms-azuretools.vscode-cosmosdb`)

*Productivity:*
- GitHub Copilot & Chat (`github.copilot`, `github.copilot-chat`)
- GitLens (`eamodio.gitlens`)
- Better Comments (`aaron-bond.better-comments`)
- Thunder Client (`rangav.vscode-thunder-client`) - API testing
- TODO Tree (`gruntfuggly.todo-tree`)

*Architecture & Documentation:*
- OpenAPI (Swagger) Viewer (`42crunch.vscode-openapi`)
- YAML (`redhat.vscode-yaml`)
- Entity Framework Tools (`richardwillis.vscode-entity-framework`)

**Demo Tip:** Show VS Code extensions panel and highlight that all extensions can be installed from the [vscode-extension.list](../vscode-extension.list) file for team consistency.

## Part 2: Architecture Overview (15 minutes)

### Visual Tour of Solution Structure
- **Show the solution in VS Code** - Highlight the clean separation:
  - `src/Services/` - 9 independent microservices (Product, Order, Cart, Customer, Inventory, Payment, Identity, Analytics, Notification)
  - `src/Gateway/` - API Gateway (Yarp reverse proxy)
  - `frontend/` - Blazor WebAssembly UI
  - `src/Aspire/` - Orchestration layer
  - `src/Shared/` - Shared contracts (events only)

### Key Architecture Principles
- **Microservices Independence**: Each service has its own database
  - OrderService → MongoDB
  - InventoryService → PostgreSQL
  - ProductService → MongoDB
  - CartService → MongoDB
  - AnalyticsService → PostgreSQL + Azure Event Hubs + Microsoft Fabric
  - NotificationService → Java/Spring Boot with RabbitMQ
- **Domain-Driven Design**: Clean Architecture with Domain/Application/Infrastructure layers
- **Event-Driven Communication**: RabbitMQ for async messaging between services
- **API Gateway Pattern**: Single entry point for frontend, routes to backend services
- **Polyglot Architecture**: .NET microservices + Java NotificationService demonstrating technology flexibility

## Part 3: Live Service Orchestration (5-8 minutes)

### Start the System
```powershell
.\Start-all.ps1
```

### Aspire Dashboard Walkthrough
- **Show running containers**: PostgreSQL, MongoDB, RabbitMQ
- **Management UIs**: Demonstrate pgAdmin, Mongo Express, RabbitMQ Management
- **Service Health**: Show all 8 microservices + gateway + frontend running
- **Explain Aspire's Role**: Service discovery, configuration, observability, container orchestration

### Point Out Key Features
- "Notice each service runs independently on different ports"
- "If one service crashes, others continue running"
- "Aspire handles connection strings, service references automatically"

## Part 4: End-to-End User Flow (10-12 minutes)

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

## Part 5: Analytics Service & Microsoft Fabric Integration (10 minutes)

### Analytics Architecture Overview
Show the complete analytics pipeline from microservice to cloud:

**Analytics Data Flow:**
```
Microservices → RabbitMQ Events → AnalyticsService
    ↓
PostgreSQL (Local Operational Analytics)
    ↓
Azure Event Hubs (Cloud Analytics)
    ↓
Microsoft Fabric Eventstream
    ↓
Lakehouse (Bronze → Silver → Gold)
    ↓
Power BI Dashboard
```

### Dual-Write Pattern Demonstration
1. **Show AnalyticsService consuming events**:
   - Open Aspire logs → Filter for AnalyticsService
   - Watch events being consumed: `OrderCreatedEvent`, `PaymentProcessedEvent`, `ProductViewedEvent`
   
2. **PostgreSQL Local Storage**:
   - Open pgAdmin → Show analytics database
   - Tables: `analytics_events` with all raw events
   - Quick operational queries for recent activity
   
3. **Azure Event Hubs Publishing**:
   - Show Azure Portal → Event Hubs namespace
   - Navigate to `analytics-events` Event Hub
   - Show incoming messages in real-time metrics

### Microsoft Fabric Medallion Architecture
1. **Bronze Layer** - Raw event ingestion:
   - Open Fabric workspace → ProductOrderingLakehouse
   - Show `OrderEvents` table with all raw events from Event Hub
   - Explain: "This is the source of truth - immutable event log"

2. **Silver Layer** - Curated transformations:
   - Show Fabric notebook: `silver-transformations`
   - Explain: "Cleans and separates events by type"
   - Show curated tables: `orders`, `payments`, `products`, `inventory`
   
3. **Gold Layer** - Business analytics:
   - Show Fabric notebook: `gold-transformations`
   - Explain: "Pre-aggregated metrics for BI dashboards"
   - Show analytics tables:
     - `daily_orders` - Order trends and revenue
     - `product_performance` - Top products, category analysis
     - `customer_metrics` - Lifetime value, purchase patterns
     - `payment_metrics` - Success rates, payment methods

### Power BI Dashboard (Optional)
- If Power BI is set up, show live dashboard:
  - Daily revenue trends
  - Top selling products
  - Customer analytics
  - Payment success rates
- Explain refresh schedule from Fabric pipeline

### Key Points
- **Dual-Write Benefits**: Local queries (fast) + Cloud analytics (scalable)
- **Real-Time Pipeline**: Events flow from services → Event Hubs → Fabric in < 1 second
- **Batch Transformations**: Silver/Gold layers updated every 15 min to 1 hour
- **No Impact on Transactions**: Analytics runs independently, doesn't slow down order processing
8
## Part 6: Event-Driven Architecture Deep Dive (8-10 minutes)

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

4. **Analytics Service Event Consumption**
   - Open Mongo Express → Navigate to Analytics database
   - Show how AnalyticsService consumes the same events:
     - `OrderCreatedEvent` → Captures order metrics (revenue, product popularity)
     - `ProductViewedEvent` → Tracks product view counts
     - `CartAbandonedEvent` → Identifies conversion opportunities
   - Explain: "Multiple services can consume the same event for different purposes"
   - Point out: "AnalyticsService builds real-time dashboards without impacting transactional services"

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

### Aspire Dashboard - Centralized Logging
- Open structured logs in Aspire Dashboard
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
  3. Show in Aspire logs: `InventoryReservationFailedEvent` published
  4. Order creation fails gracefully with clear error message

## Part 6: Database Per Service Pattern (3-5 minutes)

### Show Database Independence
1. **PostgreSQL (InventoryService)** - pgAdmin
   - Show `inventory_items` and `inventory_reservations` tables
   - Point out EF Core migrations folder

2. **MongoDB (Multiple Services)** - Mongo Express
   - **CartService** - Flexible cart documents with embedded items
   - **AnalyticsService** - Aggregated metrics, time-series data for dashboards
   - **ProductService** - Product catalog with flexible schemas
   - **OrderService** - Order history and status tracking
   - Explain: "NoSQL for flexible schemas and analytics workloads"

### Key Points
- "Each service owns its data - no direct database sharing"
- "Services communicate only through APIs and events"
- "Choose the right database for each service's needs"

## Part 9: Scaling & Deployment Discussion (2-3 minutes)

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

### Fabric Data Pipeline Orchestration
- **Show Fabric Data Pipeline** (if configured):
  - Open pipeline: `Analytics-ETL-Pipeline`
  - Show activities: Silver-Transformations → Gold-Transformations
  - Explain schedule: Hourly or every 15 minutes
  - Monitor pipeline runs and execution history
  
- **Incremental Processing**:
  - Explain how notebooks process only new events since last run
  - Show execution time: < 5 minutes for typical loads
  
- **Cost vs. Latency Tradeoffs**:
  - 15-min refresh: Near real-time, higher cost
  - Hourly refresh: Good balance for most BI use cases
  - Daily batch: Lowest cost, overnight processing

### Polyglot Microservices
- **NotificationService (Java + Spring Boot)**:
  - Show how Java service integrates seamlessly via RabbitMQ
  - Cross-platform event consumption (.NET ↔ Java)
  - Email notifications using SendGrid API

### Recent Improvements
- "Integrated Microsoft Fabric with medallion architecture (Bronze/Silver/Gold layers)"
- "Added real-time analytics pipeline: Event Hubs → Fabric → Power BI"
- "Implemented dual-write pattern: PostgreSQL (operational) + Event Hubs (analytical)"
- "Created PySpark notebooks for data transformations"
- "Set up Fabric Data Pipeline for automated ETL orchestration"
- "Implemented Java NotificationService demonstrating polyglot architecture"
- "Enabled GitHub CodeQL for automated security scanning"
- "All event naming standardized with 'Event' suffix"
- "Centralized logging and telemetry through Aspire Dashboard"

---

## Key Talking Points Throughout

1. **Loose Coupling**: Services don't know about each other, only events
2. **Fault Tolerance**: One service failure doesn't crash the system
3. **Technology Diversity**: Different databases, different patterns per service
4. **Observability**: Centralized logging and monitoring across distributed system
5. **Event Sourcing**: Audit trail of all business events
6. **Scalability**: Scale services independently based on load
7. **Team Autonomy**: Different teams can own different services
8. **Modern Tooling**: .NET 10, Aspire Dashboard, RabbitMQ, Docker

## Demo Tips

- **Prepare beforehand**: Run `.\Start-all.ps1` before the demo, verify all services healthy
- **Have data ready**: Create a few products with varying stock levels
- **Use split screen**: Show code + running app + RabbitMQ/Aspire Dashboard simultaneously
- **Tell a story**: Follow a single order through the entire system
- **Show failures**: Demonstrate error handling (insufficient stock, payment failure with bad card)
- **Keep RabbitMQ Management open**: Visual representation of events flowing
- **Use Aspire log filters**: Quickly trace specific order or event types

This agenda gives you flexibility to go deeper or shallower based on audience technical level and time constraints.

