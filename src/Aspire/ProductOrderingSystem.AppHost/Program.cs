var builder = DistributedApplication.CreateBuilder(args);

// Configure container names with ProductOrdering prefix to group in Docker Desktop
builder.Configuration["AppHost:ContainerRegistry"] = "ProductOrdering";

// Add Seq for centralized logging
var seq = builder.AddSeq("seq")
    .WithContainerName("ProductOrdering-seq")
    .WithLifetime(ContainerLifetime.Persistent);

// Add RabbitMQ message broker with management UI  
var rabbitMq = builder.AddRabbitMQ("messaging")
    .WithContainerName("ProductOrdering-rabbitmq")
    .WithManagementPlugin()
    .PublishAsConnectionString();

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

// Add Identity Service with MongoDB and RabbitMQ
// Use the "http" launch profile to get correct port configuration
var identityService = builder.AddProject<Projects.ProductOrderingSystem_IdentityService_WebAPI>("identity-service", launchProfileName: "http")
    .WithReference(identityDb)
    .WithReference(rabbitMq)
    .WithReference(seq)
    .WaitFor(mongodb)
    .WaitFor(rabbitMq);

// Add Product Service with its own MongoDB database and RabbitMQ
var productService = builder.AddProject<Projects.ProductOrderingSystem_ProductService_WebAPI>("product-service", launchProfileName: "http")
    .WithReference(productDb)
    .WithReference(rabbitMq)
    .WithReference(seq)
    .WaitFor(mongodb)
    .WaitFor(rabbitMq);

// Add Order Service with its own MongoDB database and RabbitMQ
var orderService = builder.AddProject<Projects.ProductOrderingSystem_OrderService_WebAPI>("order-service", launchProfileName: "http")
    .WithReference(orderDb)
    .WithReference(rabbitMq)
    .WithReference(seq)
    .WaitFor(mongodb)
    .WaitFor(rabbitMq);

// Add Cart Service with its own MongoDB database and RabbitMQ
var cartService = builder.AddProject<Projects.ProductOrderingSystem_CartService_WebAPI>("cart-service", launchProfileName: "http")
    .WithReference(cartDb)
    .WithReference(rabbitMq)
    .WithReference(seq)
    .WaitFor(mongodb)
    .WaitFor(rabbitMq);

// Add Payment Service with its own MongoDB database and RabbitMQ
var paymentService = builder.AddProject<Projects.ProductOrderingSystem_PaymentService_WebAPI>("payment-service", launchProfileName: "http")
    .WithReference(paymentDb)
    .WithReference(rabbitMq)
    .WithReference(seq)
    .WaitFor(mongodb)
    .WaitFor(rabbitMq);

// Add Customer Service with its own MongoDB database and RabbitMQ
var customerService = builder.AddProject<Projects.ProductOrderingSystem_CustomerService_WebAPI>("customer-service", launchProfileName: "http")
    .WithReference(customerDb)
    .WithReference(rabbitMq)
    .WithReference(seq)
    .WaitFor(mongodb)
    .WaitFor(rabbitMq);

// Add Inventory Service with its own PostgreSQL database and RabbitMQ
var inventoryService = builder.AddProject<Projects.ProductOrderingSystem_InventoryService>("inventory-service", launchProfileName: "http")
    .WithReference(inventoryDb)
    .WithReference(rabbitMq)
    .WithReference(seq)
    .WaitFor(postgres)
    .WaitFor(rabbitMq);

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
