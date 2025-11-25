# Product Ordering System - Microservices

A production-ready e-commerce microservices application built with .NET 9, demonstrating modern cloud-native patterns including event-driven architecture, CQRS, database-per-service, and distributed transaction management.

## � Quick Start for New Developers

### 1️⃣ Prerequisites
Install these before starting:
- **[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** 
- **[Java 21 JDK](https://adoptium.net/)** (for NotificationService - Spring Boot 3.4)
- **[Maven 3.9+](https://maven.apache.org/download.cgi)** (for building Java service)
- **[Docker Desktop](https://www.docker.com/products/docker-desktop)** (for databases and message broker)
- **IDE**: Visual Studio 2022, VS Code with C# Dev Kit, or Rider (+ IntelliJ IDEA/VS Code with Java extensions for NotificationService)

### 2️⃣ Clone & Setup
```bash
git clone <repository-url>
cd microservices
```

### 3️⃣ Configure API Keys
Run the interactive setup script:
```bash
.\Setup-UserSecrets.ps1
```
Enter your **Stripe test API keys** when prompted ([Get keys here](https://dashboard.stripe.com/test/apikeys))

> 💡 **New to Stripe?** Use test mode keys (start with `pk_test_` and `sk_test_`). See [API-KEYS-SETUP.md](API-KEYS-SETUP.md) for details.

### 4️⃣ Start Everything
```bash
.\Start-all.ps1
```

This single command starts:
- ✅ 7 .NET Microservices (Product, Order, Cart, Customer, Inventory, Payment, Identity)
- ✅ 1 Java Microservice (Notification - email notifications)
- ✅ API Gateway (single entry point)
- ✅ Blazor WebAssembly UI
- ✅ PostgreSQL & MongoDB databases
- ✅ RabbitMQ message broker
- ✅ Seq centralized logging
- ✅ Management UIs (pgAdmin, Mongo Express, RabbitMQ Management)

### 5️⃣ Access the System

**User Interfaces:**
- 🌐 **Web App**: http://localhost:5261 (main application)
- 📊 **Aspire Dashboard**: http://localhost:15888 (service monitoring)
- 📋 **Seq Logs**: http://localhost:5341 (centralized logs)

**Management Tools:**
- 🐰 **RabbitMQ**: http://localhost:15672 (guest/guest)
- 🐘 **pgAdmin**: Available via Aspire dashboard
- 🍃 **Mongo Express**: http://localhost:8081 (admin/admin123)

**API Documentation:**
- Each service has Scalar docs at `/scalar/v1` endpoint

### 6️⃣ Test the System

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
```bash
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
```bash
dotnet clean
dotnet build
.\Start-all.ps1
```

### Database Access
- **pgAdmin** (PostgreSQL): Via Aspire dashboard → postgres resource
- **Mongo Express** (MongoDB): http://localhost:8081 (admin/admin123)

## 🐛 Troubleshooting

### "Stripe keys not found"
Run: `.\Setup-UserSecrets.ps1` to configure your API keys

### Services won't start
1. Ensure Docker Desktop is running
2. Check port availability (5341, 15672, 27017, etc.)
3. Run `docker ps` to verify containers are running

### Build errors
```bash
dotnet clean
dotnet restore
dotnet build
```

### Can't access Aspire Dashboard
Check console output for the actual URL (usually http://localhost:15888)

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
