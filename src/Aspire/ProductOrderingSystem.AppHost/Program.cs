using System.Net;

var builder = DistributedApplication.CreateBuilder(args);

// Fix for Aspire 9.x SSL certificate validation issue in development
// Allow insecure HTTP/2 for development dashboard communication
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

// Disable HTTPS certificate validation for development
Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_DANGEROUSACCEPTANYSERVERCERTIFICATE", "true");

// Configure container names with ProductOrdering prefix to group in Docker Desktop
builder.Configuration["AppHost:ContainerRegistry"] = "ProductOrdering";

// Add RabbitMQ for messaging (ARM64 compatible for Raspberry Pi)
var messaging = builder.AddRabbitMQ("messaging")
    .WithContainerName("ProductOrdering-rabbitmq")
    .WithEndpoint("tcp", endpoint => endpoint.Port = 5672)  // Fixed AMQP port
    .WithManagementPlugin()  // Adds RabbitMQ management UI at http://localhost:15672
    .PublishAsConnectionString();

// Add MongoDB container with Aspire
var mongodb = builder.AddMongoDB("mongodb")
    .WithContainerName("ProductOrdering-mongodb")
    .WithEndpoint("tcp", endpoint => endpoint.Port = 27017)  // Fixed port for MongoDB
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
    .WithEndpoint("tcp", endpoint => endpoint.Port = 5432)  // Fixed port for PostgreSQL
    .WithPgAdmin()  // Adds pgAdmin web UI
    .WithLifetime(ContainerLifetime.Persistent);  // Keep container running between sessions

var inventoryDb = postgres.AddDatabase("inventorydb");

// Add Identity Service with MongoDB and RabbitMQ
var identityService = builder.AddProject<Projects.ProductOrderingSystem_IdentityService_WebAPI>("identity-service")
    .WithReference(identityDb)
    .WithReference(messaging)
    .WaitFor(mongodb)
    .WaitFor(messaging);

// Add Product Service with its own MongoDB database and RabbitMQ
// Product Service must start BEFORE DataSeeder so it can publish events when products are created
var productService = builder.AddProject<Projects.ProductOrderingSystem_ProductService_WebAPI>("product-service")
    .WithReference(productDb)
    .WithReference(messaging)
    .WaitFor(mongodb)
    .WaitFor(messaging);

// Add Order Service with its own MongoDB database and RabbitMQ
var orderService = builder.AddProject<Projects.ProductOrderingSystem_OrderService_WebAPI>("order-service")
    .WithReference(orderDb)
    .WithReference(messaging)
    .WaitFor(mongodb)
    .WaitFor(messaging)
    .WaitFor(productService);  // Wait for ProductService to ensure ProductCreatedEvents are available

// Add Cart Service with its own MongoDB database and RabbitMQ
var cartService = builder.AddProject<Projects.ProductOrderingSystem_CartService_WebAPI>("cart-service")
    .WithReference(cartDb)
    .WithReference(messaging)
    .WaitFor(mongodb)
    .WaitFor(messaging)
    .WaitFor(productService);  // Wait for ProductService to ensure ProductCreatedEvents are available

// Add Inventory Service with its own PostgreSQL database and RabbitMQ
var inventoryService = builder.AddProject<Projects.ProductOrderingSystem_InventoryService>("inventory-service")
    .WithReference(inventoryDb)
    .WithReference(messaging)
    .WaitFor(postgres)
    .WaitFor(messaging)
    .WaitFor(productService);  // Wait for ProductService to ensure ProductCreatedEvents are available

// Add Data Seeder - runs AFTER ProductService/OrderService/CartService/InventoryService are ready
// This ensures consumers are listening when events are published
var dataSeeder = builder.AddProject<Projects.ProductOrderingSystem_DataSeeder>("data-seeder")
    .WithReference(mongodb)
    .WithReference(messaging)
    .WithEnvironment("ConnectionStrings__messaging-management", "http://localhost:15672")  // RabbitMQ Management API
    .WaitFor(mongodb)
    .WaitFor(messaging)
    .WaitFor(productService)   // Wait for ProductService to be ready to receive commands
    .WaitFor(orderService)     // Wait for OrderService consumer to be ready
    .WaitFor(cartService)      // Wait for CartService consumer to be ready
    .WaitFor(inventoryService);  // Wait for InventoryService consumer to be ready

// Add Payment Service with its own MongoDB database and RabbitMQ
var paymentService = builder.AddProject<Projects.ProductOrderingSystem_PaymentService_WebAPI>("payment-service")
    .WithReference(paymentDb)
    .WithReference(messaging)
    .WaitFor(mongodb)
    .WaitFor(messaging)
    .WaitFor(orderService);  // Wait for OrderService to ensure OrderCreatedEvents are available

// Add Customer Service with its own MongoDB database and RabbitMQ
var customerService = builder.AddProject<Projects.ProductOrderingSystem_CustomerService_WebAPI>("customer-service")
    .WithReference(customerDb)
    .WithReference(messaging)
    .WaitFor(mongodb)
    .WaitFor(messaging);

// Add API Gateway with references to all services
// Use a fixed HTTP endpoint so the frontend can reliably connect
var apiGateway = builder.AddProject<Projects.ProductOrderingSystem_ApiGateway>("api-gateway")
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
