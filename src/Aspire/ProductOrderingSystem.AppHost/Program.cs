var builder = DistributedApplication.CreateBuilder(args);

// Configure container names with ProductOrdering prefix to group in Docker Desktop
builder.Configuration["AppHost:ContainerRegistry"] = "ProductOrdering";

// Add Seq for centralized logging
var seq = builder.AddSeq("seq")
    .WithContainerName("ProductOrdering-seq")
    .WithLifetime(ContainerLifetime.Persistent);

// Add RabbitMQ for messaging (ARM64 compatible for Raspberry Pi)
var messaging = builder.AddRabbitMQ("messaging")
    .WithContainerName("ProductOrdering-rabbitmq")
    .WithManagementPlugin()  // Adds RabbitMQ management UI at http://localhost:15672
    .WithLifetime(ContainerLifetime.Persistent);

// Add MongoDB container with Aspire
var mongodb = builder.AddMongoDB("mongodb")
    .WithContainerName("ProductOrdering-mongodb")
    .WithMongoExpress()  // Adds MongoDB Express web UI
    .WithLifetime(ContainerLifetime.Persistent);  // Keep container running between sessions

// Create separate databases for each microservice
var productDb = mongodb.AddDatabase("productdb");
var orderDb = mongodb.AddDatabase("orderdb");
var identityDb = mongodb.AddDatabase("identitydb");
var cartDb = mongodb.AddDatabase("cartdb");
var paymentDb = mongodb.AddDatabase("paymentdb");
var customerDb = mongodb.AddDatabase("customerdb");

// Add PostgreSQL for Inventory Service
var postgres = builder.AddPostgres("postgres")
    .WithContainerName("ProductOrdering-postgres")
    .WithPgAdmin()  // Adds pgAdmin web UI
    .WithLifetime(ContainerLifetime.Persistent);  // Keep container running between sessions

var inventoryDb = postgres.AddDatabase("inventorydb");

// Add Data Seeder - runs once to seed initial data
var dataSeeder = builder.AddProject<Projects.ProductOrderingSystem_DataSeeder>("data-seeder")
    .WithReference(mongodb)
    .WithReference(messaging)
    .WaitFor(mongodb)
    .WaitFor(messaging);

// Add Identity Service with MongoDB and RabbitMQ
// Use the "http" launch profile to get correct port configuration
var identityService = builder.AddProject<Projects.ProductOrderingSystem_IdentityService_WebAPI>("identity-service", launchProfileName: "http")
    .WithReference(identityDb)
    .WithReference(messaging)
    .WithReference(seq)
    .WaitFor(mongodb)
    .WaitFor(messaging)
    .WaitFor(dataSeeder);  // Wait for seeder to complete

// Add Product Service with its own MongoDB database and RabbitMQ
var productService = builder.AddProject<Projects.ProductOrderingSystem_ProductService_WebAPI>("product-service", launchProfileName: "http")
    .WithReference(productDb)
    .WithReference(messaging)
    .WithReference(seq)
    .WaitFor(mongodb)
    .WaitFor(messaging)
    .WaitFor(dataSeeder);  // Wait for seeder to complete

// Add Order Service with its own MongoDB database and RabbitMQ
var orderService = builder.AddProject<Projects.ProductOrderingSystem_OrderService_WebAPI>("order-service", launchProfileName: "http")
    .WithReference(orderDb)
    .WithReference(messaging)
    .WithReference(seq)
    .WaitFor(mongodb)
    .WaitFor(messaging);

// Add Cart Service with its own MongoDB database and RabbitMQ
var cartService = builder.AddProject<Projects.ProductOrderingSystem_CartService_WebAPI>("cart-service", launchProfileName: "http")
    .WithReference(cartDb)
    .WithReference(messaging)
    .WithReference(seq)
    .WaitFor(mongodb)
    .WaitFor(messaging);

// Add Payment Service with its own MongoDB database and RabbitMQ
var paymentService = builder.AddProject<Projects.ProductOrderingSystem_PaymentService_WebAPI>("payment-service", launchProfileName: "http")
    .WithReference(paymentDb)
    .WithReference(messaging)
    .WithReference(seq)
    .WaitFor(mongodb)
    .WaitFor(messaging);

// Add Customer Service with its own MongoDB database and RabbitMQ
var customerService = builder.AddProject<Projects.ProductOrderingSystem_CustomerService_WebAPI>("customer-service", launchProfileName: "http")
    .WithReference(customerDb)
    .WithReference(messaging)
    .WithReference(seq)
    .WaitFor(mongodb)
    .WaitFor(messaging);

// Add Inventory Service with its own PostgreSQL database and RabbitMQ
var inventoryService = builder.AddProject<Projects.ProductOrderingSystem_InventoryService>("inventory-service", launchProfileName: "http")
    .WithReference(inventoryDb)
    .WithReference(messaging)
    .WithReference(seq)
    .WaitFor(postgres)
    .WaitFor(messaging);

// Add API Gateway with references to all services
// Use a fixed HTTP endpoint so the frontend can reliably connect
var apiGateway = builder.AddProject<Projects.ProductOrderingSystem_ApiGateway>("api-gateway", launchProfileName: "http")
    .WithReference(identityService)
    .WithReference(productService)
    .WithReference(orderService)
    .WithReference(cartService)
    .WithReference(paymentService)
    .WithReference(customerService)
    .WithReference(inventoryService)
    .WithExternalHttpEndpoints();

// Add Blazor Frontend (WebAssembly)
// The frontend will connect to the API Gateway for all backend services
var frontend = builder.AddProject<Projects.ProductOrderingSystem_Web>("frontend")
    .WithExternalHttpEndpoints();

builder.Build().Run();
