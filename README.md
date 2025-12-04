# Product Ordering System - Microservices

A production-ready e-commerce microservices application built with .NET 9, demonstrating modern cloud-native patterns including event-driven architecture, CQRS, database-per-service, and distributed transaction management.

## � Quick Start for New Developers

### 1️⃣ Prerequisites
Install these before starting:
- **[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** 
- **[Java 21 JDK](https://adoptium.net/)** (for NotificationService - Spring Boot 3.4)
- **[Apache Maven](https://maven.apache.org/download.cgi)** - Use our automated installer (see step 2.5) OR download manually
- **[Docker Desktop](https://www.docker.com/products/docker-desktop)** ⚠️ **Must be running before starting services**
- **IDE**: Visual Studio 2022, VS Code with C# Dev Kit, or Rider (+ IntelliJ IDEA/VS Code with Java extensions for NotificationService)

### 2️⃣ Clone & Setup
```powershell
git clone <repository-url>
cd microservices
```

### 2.5️⃣ Install Maven (Automated - Windows Only)
```powershell
.\install-maven.ps1
```
This script automatically downloads Maven 3.9.9 with fallback mirrors and configures your PATH. No manual setup needed!

> 💡 **Mac/Linux users**: Install Maven using your package manager (brew, apt, etc.)

### 3️⃣ Trust HTTPS Development Certificates
```powershell
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```
Click **Yes** when prompted. This prevents SSL certificate errors in Aspire Dashboard.

> ⚠️ **Important**: Without this step, you may see SSL/TLS errors when accessing the Aspire Dashboard.

### 4️⃣ Configure API Keys
Run the interactive setup script:
```powershell
.\Setup-UserSecrets.ps1
```
Enter your **Stripe test API keys** when prompted ([Get keys here](https://dashboard.stripe.com/test/apikeys))

> 💡 **New to Stripe?** Use test mode keys (start with `pk_test_` and `sk_test_`). See [API-KEYS-SETUP.md](API-KEYS-SETUP.md) for details.

### 5️⃣ Start Everything (Single Command)

⚠️ **Critical**: Ensure Docker Desktop is running and fully started before proceeding!

```powershell
.\Start-all.ps1
```

This automated script handles everything:
- ✅ Starts Aspire AppHost (orchestrates all containers)
- ✅ Spins up infrastructure: RabbitMQ, MongoDB, PostgreSQL, Seq
- ✅ Waits for containers to be healthy
- ✅ Detects RabbitMQ dynamic port (automatically configured)
- ✅ Starts all .NET microservices (Product, Order, Cart, Customer, Inventory, Payment, Identity)
- ✅ Starts Java NotificationService with correct messaging configuration
- ✅ Starts API Gateway and Blazor WebAssembly UI
- ✅ Starts management UIs (pgAdmin, Mongo Express, RabbitMQ Management)

**What's automated for you:**
- RabbitMQ port detection (no manual configuration needed)
- Container health checks and readiness waiting
- Service startup sequencing
- Environment variable configuration for cross-platform messaging

### 6️⃣ Access the System

**User Interfaces:**
- 🌐 **Web App**: http://localhost:5261 (main application)
- 📊 **Aspire Dashboard**: http://localhost:15888 (service monitoring - check console for actual URL)
- 📋 **Seq Logs**: http://localhost:5341 (centralized logs)

**Management Tools:**
- 🐰 **RabbitMQ**: http://localhost:15672 (guest/guest)
- 🐘 **pgAdmin**: Available via Aspire dashboard
- 🍃 **Mongo Express**: http://localhost:8081 (admin/admin123)

**API Documentation:**
- Each service has Scalar docs at `/scalar/v1` endpoint

### 6.5️⃣ Seed Demo Data (Optional)

Run the data seeder to populate 100 sample products:

```powershell
cd src/Tools/ProductOrderingSystem.DataSeeder
dotnet run
```

See [DataSeeder README](src/Tools/ProductOrderingSystem.DataSeeder/README.md) for configuration options.

### 7️⃣ Test the System

**Pre-seeded Test Users:**
- 👤 **Admin User** - Username: `admin`, Password: `P@ssw0rd` (Full access to admin panel)
- 👤 **Shopper User** - Username: `steve.hopper`, Password: `P@ssw0rd` (Regular customer account)

**Test the Shopping Flow:**

1. **Register/Login** at http://localhost:5261 (or use pre-seeded users above)
2. **Browse Products** - View catalog
3. **Add to Cart** - Add items (CartService + MongoDB)
4. **Place Order** - Watch events flow:
   - OrderService creates order
   - InventoryService reserves stock (PostgreSQL)
   - PaymentService processes via Stripe
   - InventoryService commits reservation
   - OrderService updates status
5. **View Order Status** - See order in "Processing" state
6. **Admin Panel** - Login as `admin` to manage products, inventory, orders

**View Event Flow:**
- Open RabbitMQ Management → See events published/consumed
- Open Seq → Search by OrderId to trace full order journey

## 🏗️ Architecture Overview

### Microservices (8 Total)

| Service | Database | Language | Purpose |
|---------|----------|----------|---------|
| **ProductService** | MongoDB | .NET 9 | Product catalog, pricing, descriptions |
| **OrderService** | MongoDB | .NET 9 | Order processing, status tracking |
| **CartService** | MongoDB | .NET 9 | Shopping cart, session management |
| **CustomerService** | MongoDB | .NET 9 | Customer profiles, addresses |
| **InventoryService** | PostgreSQL | .NET 9 | Stock levels, reservations, fulfillment |
| **PaymentService** | MongoDB | .NET 9 | Payment processing (Stripe integration) |
| **IdentityService** | MongoDB | .NET 9 | Authentication, JWT tokens |
| **NotificationService** | MongoDB | Java 21 + Spring Boot 3.4 | Email notifications via SendGrid, event-driven with RabbitMQ |

### Supporting Components
- **API Gateway** (Yarp) - Single entry point, routing, authentication
- **Blazor WebAssembly** - Modern SPA frontend with MudBlazor UI
- **RabbitMQ + MassTransit** - Event-driven messaging (cross-platform: .NET ↔ Java)
- **.NET Aspire** - Local orchestration, service discovery
- **Seq** - Centralized logging with Serilog

## 📁 Project Structure

```
microservices/
├── src/
│   ├── Services/                    # 8 Independent Microservices
│   │   ├── ProductService/          # Clean Architecture with Domain/Application/Infrastructure/WebAPI
│   │   ├── OrderService/            # CQRS pattern, MongoDB
│   │   ├── CartService/             # MongoDB, session-based carts
│   │   ├── CustomerService/         # Customer management, MongoDB
│   │   ├── InventoryService/        # PostgreSQL with EF Core, stock tracking
│   │   ├── PaymentService/          # Stripe integration, MongoDB, event publishing
│   │   ├── IdentityService/         # JWT authentication, MongoDB
│   │   └── NotificationService/     # Java 21 + Spring Boot 3.4, SendGrid email, RabbitMQ consumers
│   ├── Gateway/
│   │   └── ApiGateway/              # Yarp reverse proxy, JWT validation
│   ├── Shared/
│   │   └── Contracts/               # Shared event contracts (ProductCreatedEvent, etc.)
│   └── Aspire/
│       ├── AppHost/                 # Orchestration, container management
│       └── ServiceDefaults/         # Shared config (Serilog, OpenTelemetry)
├── frontend/                        # Blazor WebAssembly SPA
│   ├── Pages/                       # Customer & Admin pages
│   ├── Services/                    # API clients for each microservice
│   └── Components/                  # Reusable UI components
├── tests/                           # Unit & Integration tests per service
├── docs/                            # Architecture documentation
├── deployment/                      # Docker configs
├── Setup-UserSecrets.ps1            # API keys setup script
└── Start-all.ps1                    # One-command startup
```

## 🚀 Key Features

### Event-Driven Architecture
- **Async Communication** - Services communicate via RabbitMQ events, not HTTP
- **Loose Coupling** - Services don't know about each other, only events
- **Event Contracts** - Standardized naming (`ProductCreatedEvent`, `PaymentProcessedEvent`)

### Order Flow (Distributed Transaction)
```
1. Customer places order → OrderService publishes OrderCreatedEvent
2. InventoryService receives event → Reserves stock → Publishes InventoryReservedEvent
3. PaymentService processes payment → Publishes PaymentProcessedEvent
4. InventoryService commits reservation → Publishes InventoryCommittedEvent
5. OrderService updates status to "Processing"
```

If payment fails:
- `PaymentFailedEvent` published
- InventoryService releases reservation automatically
- OrderService marks order as "Cancelled"

### Database Per Service Pattern
Each service owns its data - no shared databases:
- **PostgreSQL**: InventoryService (EF Core migrations, transactional inventory)
- **MongoDB**: All other services (ProductService, OrderService, CartService, CustomerService, PaymentService, IdentityService)

### Observability
- **Seq Centralized Logging** - Trace events across all services by OrderId
- **Aspire Dashboard** - Service health, resource utilization
- **RabbitMQ Management** - Message flow visualization
- **Serilog** - Structured logging with enrichers

### Security
- **JWT Authentication** - Token-based auth via IdentityService
- **API Gateway** - Single entry point, validates all requests
- **User Secrets** - API keys stored securely, never committed

## 🚀 Technologies & Patterns

### Technology Stack
| Category | Technologies |
|----------|-------------|
| **Runtime** | .NET 9.0 (C# 13), Java 21 (Spring Boot 3.4) |
| **Databases** | PostgreSQL 17, MongoDB 8 |
| **Messaging** | RabbitMQ 4.0, MassTransit 8.3.5 (.NET), Spring AMQP (Java) |
| **Frontend** | Blazor WebAssembly, MudBlazor 7.x |
| **Orchestration** | .NET Aspire 9.5.1 |
| **API Gateway** | Yarp 2.2.0 |
| **Logging** | Seq, Serilog 9.0 (.NET), SLF4J/Logback (Java) |
| **Payments** | Stripe API |
| **Email** | SendGrid API |
| **ORM** | Entity Framework Core 9.0, MongoDB Driver (.NET), Spring Data MongoDB (Java) |

### Architecture Patterns
- ✅ **Microservices** - 8 independent services with separate databases
- ✅ **Polyglot Microservices** - .NET and Java services working together via RabbitMQ
- ✅ **Event-Driven Architecture** - RabbitMQ for async cross-service communication
- ✅ **CQRS** - Command Query Responsibility Segregation with MediatR (.NET)
- ✅ **Clean Architecture** - Domain/Application/Infrastructure separation
- ✅ **Database Per Service** - Polyglot persistence (PostgreSQL, MongoDB)
- ✅ **API Gateway** - Single entry point pattern
- ✅ **Saga Pattern** - Choreography-based distributed transactions
- ✅ **Vertical Slice Architecture** - In some services (InventoryService)
- ✅ **Repository Pattern** - Data access abstraction
- ✅ **Domain Events** - Rich domain models with event sourcing

### Code Quality
- ✅ **Central Package Management** - Directory.Packages.props for version control
- ✅ **User Secrets** - Secure API key management
- ✅ **FluentValidation** - Request validation
- ✅ **ErrorOr** - Railway-oriented programming
- ✅ **Modern .slnx** - XML-based solution format

## 📚 Documentation & Learning Resources

### Getting Started Guides
- **[Quick Start](docs/QUICKSTART.md)** - Fastest way to run everything
- **[API Keys Setup](API-KEYS-SETUP.md)** - Configure Stripe test keys
- **[User Secrets](docs/User-Secrets-Setup.md)** - Secure API key management
- **[Docker Organization](docs/Docker-Container-Organization.md)** - Container naming conventions

### Architecture Documentation
- **[Architecture Diagrams](docs/Architecture-Diagrams.md)** - Comprehensive Mermaid diagrams (system architecture, data flow, event-driven patterns)
- **[Event Naming Conventions](docs/Event-Naming-Conventions.md)** - Standardized event naming
- **[PostgreSQL Migration](docs/PostgreSQL-Migration-Summary.md)** - InventoryService database migration
- **[Polyglot Integration](docs/POLYGLOT_INTEGRATION.md)** - .NET and Java service integration
- **[Messaging Implementation](docs/MESSAGING_IMPLEMENTATION.md)** - RabbitMQ event-driven architecture
- **[Saga Compensation](docs/SAGA_COMPENSATION_IMPLEMENTATION.md)** - Distributed transaction patterns
- **[REST API Principles](docs/REST-API-Principles.md)** - API design standards
- **[Order Queries](docs/ORDER_QUERIES_IMPLEMENTATION.md)** - CQRS query implementation
- **[CartService RabbitMQ](docs/CartService-RabbitMQ-Integration.md)** - Event consumer patterns

### Testing & Development
- **[Testing Guide](docs/TESTING.md)** - Comprehensive testing (unit, integration, E2E, polyglot)
- **[Demo Agenda](docs/Microservices-Demo-Agenda.md)** - 30-45 min presentation guide

### Troubleshooting
- **[Aspire Troubleshooting](docs/ASPIRE_TROUBLESHOOTING.md)** - Common Aspire issues and solutions

### Code Examples
Browse the codebase to see patterns in action:
- **Event Consumers (.NET)**: `src/Services/InventoryService/Features/EventConsumers/`
- **Event Consumers (Java)**: `src/Services/NotificationService/src/main/java/com/productordering/notificationservice/consumers/`
- **CQRS Handlers**: `src/Services/OrderService/Application/`
- **Vertical Slices**: `src/Services/InventoryService/Features/Inventory/`
- **Clean Architecture**: `src/Services/ProductService/`
- **Spring Boot Service**: `src/Services/NotificationService/` (Java microservice example)

## 🎓 Learning Outcomes

By exploring this project, you'll learn:
- ✅ How to build microservices with .NET 9 and Java/Spring Boot
- ✅ **Polyglot microservices** - Integrating .NET and Java services via messaging
- ✅ Event-driven architecture with RabbitMQ and MassTransit
- ✅ **Cross-platform messaging** - .NET (MassTransit) ↔ Java (Spring AMQP)
- ✅ Handling distributed transactions (saga pattern)
- ✅ Database-per-service with PostgreSQL and MongoDB
- ✅ API Gateway patterns with Yarp
- ✅ Blazor WebAssembly frontend development
- ✅ Observability with Seq and Aspire
- ✅ Payment integration with Stripe
- ✅ Email notifications with SendGrid
- ✅ Clean Architecture and CQRS
- ✅ Containerization and orchestration

## 🔧 Common Tasks

### View User Secrets
```powershell
cd src/Services/PaymentService/ProductOrderingSystem.PaymentService.WebAPI
dotnet user-secrets list
```

### View Service Logs
Open Seq at http://localhost:5341 and search by:
- Service name: `@Application = "PaymentService"`
- Order ID: `OrderId = "your-order-guid"`
- Event type: `PaymentProcessedEvent`

### Check Message Queues
1. Open RabbitMQ Management: http://localhost:15672
2. Navigate to Queues tab
3. See messages in queues like `inventory-service-order-created`

### Rebuild Everything
```powershell
dotnet clean
dotnet build
.\Start-all.ps1
```

### Database Access
- **pgAdmin** (PostgreSQL): Via Aspire dashboard → postgres resource
- **Mongo Express** (MongoDB): http://localhost:8081 (admin/admin123)

## 🐛 Troubleshooting

### Docker Desktop Issues

**Problem**: Services fail to start with "Docker daemon not running" or container connection errors

**Solution**: 
1. Ensure Docker Desktop is running
2. Wait for Docker Desktop to fully start (green indicator in system tray)
3. Verify with: `docker ps`
4. If still failing, restart Docker Desktop

### Maven Installation Issues

**Problem**: Getting HTTP 404 errors when trying to install Maven manually

**Solution**: Use the automated installer script:
```powershell
.\install-maven.ps1
```
This script uses Maven 3.9.9 with fallback mirrors (apache.org archives) to handle download failures.

### SSL/HTTPS Certificate Errors in Aspire Dashboard

**Problem**: Seeing `UntrustedRoot` or SSL certificate validation errors when accessing Aspire Dashboard

**Solution**: Regenerate and trust development certificates:
```powershell
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```
Click **Yes** when prompted. Restart the application after this step.

**Why this happens**: Development certificates can become untrusted or corrupted. This is a one-time fix.

### RabbitMQ Connection Refused (Java NotificationService)

**Problem**: Java NotificationService shows `Connection refused` on localhost:5672

**Root Cause**: Aspire assigns RabbitMQ dynamic ports, but Java service expects fixed port 5672

**Solution**: This is already fixed in `start-all.ps1` which automatically:
1. Waits for RabbitMQ container to start
2. Detects the dynamic port using `docker port ProductOrdering-rabbitmq 5672`
3. Sets `RABBITMQ_PORT` environment variable for Java service

**Manual check**: If needed, find the port manually:
```powershell
docker port ProductOrdering-rabbitmq 5672
```

### "Stripe keys not found"

**Problem**: PaymentService fails with missing API key errors

**Solution**: Configure your Stripe test API keys:
```powershell
.\Setup-UserSecrets.ps1
```

Get test keys from [Stripe Dashboard](https://dashboard.stripe.com/test/apikeys) (they start with `pk_test_` and `sk_test_`).

### Services Won't Start

**Common causes and solutions**:

1. **Port conflicts**: Another application is using required ports
   - Check: `netstat -ano | findstr :5261` (or other port)
   - Fix: Stop the conflicting application

2. **Containers not running**: Infrastructure containers failed to start
   - Check: `docker ps` to see running containers
   - Fix: Restart Docker Desktop, then run `.\Start-all.ps1` again

3. **Build errors**: Code compilation issues
   - Fix: Clean and rebuild:
   ```powershell
   dotnet clean
   dotnet restore
   dotnet build
   ```

### Can't Access Aspire Dashboard

**Problem**: http://localhost:15888 doesn't work

**Solution**: Aspire sometimes uses a different port. Check the console output when starting for the actual URL:
```
Now listening on: http://localhost:17234
```

### X-Pagination Header Not Visible in Browser

**Problem**: Custom pagination headers not accessible to JavaScript/browser clients

**Solution**: This is already fixed. All services now expose `X-Pagination` header via CORS configuration.

**How it was fixed**: Added `.WithExposedHeaders("X-Pagination")` to CORS policies in all services.

### Build Errors After Package Updates

**Problem**: Compilation errors after updating NuGet packages

**Solution**: 
1. Check `Directory.Packages.props` for version compatibility
2. Current packages are on latest stable .NET 9 compatible versions
3. Major version updates (MassTransit 9.0, Stripe.net 50.x) may have breaking changes
4. Stick with current versions unless you need specific new features

### Java NotificationService Build Fails

**Problem**: Maven build errors or missing dependencies

**Solution**:
```powershell
cd src/Services/NotificationService
mvn clean install
```

If still failing, ensure:
- Java 21 JDK is installed: `java -version`
- Maven is installed: `mvn -version` 
- Use our installer: `.\install-maven.ps1`

### Database Connection Errors

**Problem**: Services can't connect to MongoDB or PostgreSQL

**Solution**:
1. Verify containers are running: `docker ps | findstr mongo` and `docker ps | findstr postgres`
2. Check Aspire dashboard for container health
3. Restart infrastructure: Stop all services, then run `.\Start-all.ps1`

### Event Messages Not Being Consumed

**Problem**: Events published but not consumed by other services

**Diagnosis**:
1. Open RabbitMQ Management: http://localhost:15672 (guest/guest)
2. Check Queues tab - are messages accumulating?
3. Check Connections tab - are all services connected?

**Common causes**:
- Consumer service not running
- Exchange/queue binding misconfigured
- Event contract version mismatch

**Solution**: Check Seq logs for the specific service to see consumer errors.

### Helpful Commands

**View all running containers:**
```powershell
docker ps
```

**View container logs:**
```powershell
docker logs ProductOrdering-rabbitmq
docker logs ProductOrdering-mongodb
```

**Restart all containers:**
```powershell
docker restart $(docker ps -q)
```

**Clean Docker (nuclear option):**
```powershell
.\Cleanup-Docker.ps1  # Removes all containers, volumes, and networks
```
⚠️ **Warning**: This deletes all data. You'll need to reseed after this.

## 🤝 Contributing

### Adding a New Service
1. Create service in `src/Services/YourService/`
2. Add project reference to `AppHost/Program.cs`
3. Define event contracts in `Shared/Contracts/`
4. Configure MassTransit consumers
5. Update this README

### Adding New Events
1. Add event record to `Shared/Contracts/Events/`
2. Name with "Event" suffix (e.g., `OrderShippedEvent`)
3. Create consumer in relevant service
4. Register consumer in `Program.cs`

## 📝 Recent Updates

- ✅ **PostgreSQL Migration** - InventoryService migrated from MongoDB to PostgreSQL
- ✅ **Event Standardization** - All events follow "Event" suffix naming
- ✅ **Seq Integration** - Centralized logging with Serilog
- ✅ **User Secrets** - API keys moved out of source control
- ✅ **Blazor UI** - Full frontend with customer and admin features
- ✅ **Payment Integration** - Stripe test mode with order flow

## 📄 License

This project is provided as-is for educational and demonstration purposes.
