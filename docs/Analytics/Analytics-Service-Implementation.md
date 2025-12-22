---
tags:
  - service
  - analytics
  - mongodb
  - implementation
  - azure-event-hub
  - scalar
  - csharp
---

# Analytics Service Implementation with Microsoft Fabric and Azure Data Lake

This document provides a comprehensive guide for implementing the Analytics Service for the Product Ordering System using Microsoft Fabric and Azure Data Lake Storage.

## Overview

The Analytics Service will provide real-time and batch analytics capabilities for business intelligence, leveraging Microsoft Fabric's unified analytics platform and Azure Data Lake for scalable data storage.

### Key Objectives
- Capture and analyze sales metrics, customer behavior, and product performance
- Provide real-time dashboards and historical trend analysis
- Enable data-driven decision making across the organization
- Support advanced analytics and machine learning scenarios

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     Microservices Layer                          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐       │
│  │  Order   │  │ Product  │  │   Cart   │  │ Customer │       │
│  │ Service  │  │ Service  │  │ Service  │  │ Service  │       │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘       │
│       │             │              │             │              │
│       └─────────────┴──────────────┴─────────────┘              │
│                          │                                       │
│                     RabbitMQ Events                             │
└──────────────────────────┬──────────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────────┐
│              Analytics Service (Event Consumer)                  │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  Event Handlers (MassTransit)                          │    │
│  │  • OrderCreatedEvent                                    │    │
│  │  • ProductViewedEvent                                   │    │
│  │  • CartAbandonedEvent                                   │    │
│  │  • CustomerRegisteredEvent                              │    │
│  └────────────────────┬───────────────────────────────────┘    │
│                       │                                          │
│  ┌────────────────────▼───────────────────────────────────┐    │
│  │  Data Processing Layer                                  │    │
│  │  • Real-time aggregation                                │    │
│  │  • Event transformation                                 │    │
│  │  • Data enrichment                                      │    │
│  └────────────────────┬───────────────────────────────────┘    │
└──────────────────────┬┴──────────────────────────────────────────┘
                       │
          ┌────────────┴────────────┐
          │                         │
┌─────────▼────────────┐  ┌────────▼─────────────┐
│  Azure Event Hubs    │  │  MongoDB (Hot Data)  │
│  (Streaming Ingest)  │  │  • Real-time metrics │
└─────────┬────────────┘  └──────────────────────┘
          │
┌─────────▼──────────────────────────────────────────────────────┐
│              Azure Data Lake Storage Gen2                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│  │  Raw Zone    │→ │ Curated Zone │→ │ Analytics    │         │
│  │  (Bronze)    │  │  (Silver)    │  │ Zone (Gold)  │         │
│  └──────────────┘  └──────────────┘  └──────────────┘         │
└─────────┬──────────────────────────────────────────────────────┘
          │
┌─────────▼──────────────────────────────────────────────────────┐
│                    Microsoft Fabric                             │
│  ┌──────────────────┐  ┌──────────────────┐                   │
│  │  Data Engineering │  │  Data Science    │                   │
│  │  • Synapse        │  │  • ML Models     │                   │
│  │  • Spark Jobs     │  │  • Predictions   │                   │
│  └──────────────────┘  └──────────────────┘                   │
│  ┌──────────────────┐  ┌──────────────────┐                   │
│  │  Data Warehouse  │  │  Power BI        │                   │
│  │  • Lakehouse     │  │  • Dashboards    │                   │
│  │  • SQL Analytics │  │  • Reports       │                   │
│  └──────────────────┘  └──────────────────┘                   │
└────────────────────────────────────────────────────────────────┘
```

---

## Data Flow Architecture

### 1. Event Ingestion

The Analytics Service implements a **dual-write pattern** for resilience and flexibility:

#### Storage Targets

**PostgreSQL (Local Analytics Database)**
- **Purpose**: 
  - Local operational analytics with fast query response
  - Resilience/fallback if Event Hubs is down or misconfigured
  - Development and debugging without Azure portal access
- **Data Retention**: Recent events (typically 30-90 days)
- **Query Performance**: Millisecond latency for operational queries
- **Use Case**: Real-time dashboards, troubleshooting, development

**Azure Event Hubs → Microsoft Fabric (Cloud Analytics)**
- **Purpose**:
  - Long-term historical analytics at scale
  - Advanced BI with Power BI integration
  - Machine learning and predictive analytics
- **Data Retention**: Unlimited (Data Lake storage)
- **Query Performance**: Optimized for complex aggregations and large datasets
- **Use Case**: Executive dashboards, trend analysis, data science

#### Architectural Options

| Approach | Pros | Cons | Best For |
|----------|------|------|----------|
| **Both (Current)** | Resilience, local + cloud analytics, flexibility | Dual writes, storage costs, complexity | Production systems needing both operational and analytical workloads |
| **Event Hubs/Fabric Only** | Simpler code, cloud-native, single source of truth | Cloud dependency, latency for operational queries | Cloud-first organizations, BI-focused analytics |
| **PostgreSQL Only** | Self-contained, no cloud costs, simple | No Fabric/Power BI, limited scale | On-premises deployments, dev/test environments |

**Recommendation**: Start with dual-write for flexibility, then remove PostgreSQL if Fabric covers all analytics needs.

#### Event Flow
- **Source**: Domain events from microservices via RabbitMQ
- **Consumer**: Analytics Service with MassTransit consumers
- **Targets**: 
  1. PostgreSQL (immediate write, local queries)
  2. Azure Event Hubs (streaming to Fabric pipeline)

### 2. Data Lake Zones (Medallion Architecture)

#### Bronze Layer (Raw Data)
- **Purpose**: Store raw events in original format
- **Format**: Parquet files partitioned by date
- **Path**: `/raw/{event-type}/year={yyyy}/month={MM}/day={dd}/`
- **Retention**: 90 days

#### Silver Layer (Curated Data)
- **Purpose**: Cleaned, validated, and enriched data
- **Format**: Delta Lake tables
- **Transformations**:
  - Data validation and cleansing
  - Schema enforcement
  - Deduplication
  - Data type conversions
- **Path**: `/curated/{domain}/`

#### Gold Layer (Analytics-Ready)
- **Purpose**: Aggregated data optimized for reporting
- **Format**: Delta Lake tables with partitioning
- **Aggregations**:
  - Daily/weekly/monthly sales summaries
  - Customer cohort analysis
  - Product performance metrics
  - Funnel conversion rates
- **Path**: `/analytics/{metric-type}/`

---

## Technology Stack

### Core Services
- **.NET 10.0**: Service implementation
- **MassTransit**: Event consumption from RabbitMQ
- **MongoDB**: Hot data storage for real-time queries

### Microsoft Fabric Components
- **Data Factory**: Orchestration of data pipelines
- **Synapse Data Engineering**: Apache Spark for data processing
- **Data Warehouse**: Lakehouse and SQL analytics endpoint
- **Power BI**: Interactive dashboards and reports
- **Real-Time Analytics**: KQL databases for streaming analytics

### Azure Services
- **Azure Event Hubs**: Streaming event ingestion
- **Azure Data Lake Storage Gen2**: Hierarchical namespace storage
- **Azure Key Vault**: Secrets management
- **Azure Monitor**: Service monitoring and diagnostics

---

## Implementation Steps

### Phase 1: Infrastructure Setup

#### 1.1 Azure Data Lake Storage Setup

```bash
# Variables
$resourceGroup = "rg-product-ordering-analytics"
$location = "eastus"
$storageAccount = "stprodorderanalytics"
$containerName = "analytics-datalake"

# Create resource group
az group create --name $resourceGroup --location $location

# Create storage account with hierarchical namespace (Data Lake Gen2)
az storage account create `
  --name $storageAccount `
  --resource-group $resourceGroup `
  --location $location `
  --sku Standard_LRS `
  --kind StorageV2 `
  --hierarchical-namespace true `
  --enable-nfs-v3 false

# Create container
az storage container create `
  --name $containerName `
  --account-name $storageAccount `
  --auth-mode login

# Create folder structure
az storage fs directory create -n "raw" -f $containerName --account-name $storageAccount --auth-mode login
az storage fs directory create -n "curated" -f $containerName --account-name $storageAccount --auth-mode login
az storage fs directory create -n "analytics" -f $containerName --account-name $storageAccount --auth-mode login
```

#### 1.2 Azure Event Hubs Setup

```bash
# Variables
$eventHubNamespace = "evhns-product-ordering"
$eventHubName = "analytics-events"

# Create Event Hubs namespace
az eventhubs namespace create `
  --name $eventHubNamespace `
  --resource-group $resourceGroup `
  --location $location `
  --sku Standard

# Create Event Hub
az eventhubs eventhub create `
  --name $eventHubName `
  --namespace-name $eventHubNamespace `
  --resource-group $resourceGroup `
  --partition-count 4 `
  --message-retention 7

# Get connection string
az eventhubs namespace authorization-rule keys list `
  --resource-group $resourceGroup `
  --namespace-name $eventHubNamespace `
  --name RootManageSharedAccessKey `
  --query primaryConnectionString `
  --output tsv
```

#### 1.3 Microsoft Fabric Workspace Setup

1. **Create Fabric Workspace**:
   - Navigate to [Microsoft Fabric portal](https://app.fabric.microsoft.com/)
   - Click "Workspaces" → "New workspace"
   - Name: "ProductOrderingAnalytics"
   - Select appropriate license mode (Trial/Capacity)

2. **Create Lakehouse**:
   - In workspace, click "New" → "Lakehouse"
   - Name: "ProductOrderingLakehouse"
   - This creates a OneLake location for your data

3. **Connect Azure Data Lake**:
   - Create a Shortcut to ADLS Gen2
   - Navigate to Lakehouse → "..." → "New shortcut"
   - Select "Azure Data Lake Storage Gen2"
   - Enter connection details

---

### Phase 2: Analytics Service Development

#### 2.1 Project Structure

```
src/Services/AnalyticsService/
├── AnalyticsService.Domain/
│   ├── Entities/
│   │   ├── SalesMetric.cs
│   │   ├── CustomerBehaviorMetric.cs
│   │   └── ProductPerformanceMetric.cs
│   ├── Events/         # Events consumed from other services
│   └── ValueObjects/
├── AnalyticsService.Application/
│   ├── EventHandlers/
│   │   ├── OrderCreatedEventHandler.cs
│   │   ├── ProductViewedEventHandler.cs
│   │   └── CartAbandonedEventHandler.cs
│   ├── Services/
│   │   ├── EventHubPublisher.cs
│   │   └── MetricsAggregationService.cs
│   └── DTOs/
├── AnalyticsService.Infrastructure/
│   ├── Data/
│   │   ├── AnalyticsDbContext.cs
│   │   └── Repositories/
│   ├── EventHub/
│   │   └── EventHubProducerService.cs
│   └── DataLake/
│       └── DataLakeWriter.cs
└── AnalyticsService.WebAPI/
    ├── Program.cs
    ├── appsettings.json
    └── Endpoints/
        └── MetricsEndpoints.cs
```

#### 2.2 Domain Models

**SalesMetric.cs**
```csharp
namespace AnalyticsService.Domain.Entities;

public class SalesMetric
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public int ItemCount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ProductPerformanceMetric
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int AddToCartCount { get; set; }
    public int PurchaseCount { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal Revenue { get; set; }
}

public class CustomerBehaviorMetric
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid CustomerId { get; set; }
    public string EventType { get; set; } = string.Empty; // View, AddToCart, Purchase, Abandon
    public Guid? ProductId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}
```

#### 2.3 Event Handlers

**OrderCreatedEventHandler.cs**
```csharp
using MassTransit;
using AnalyticsService.Application.Services;
using ProductOrderingSystem.Shared.Events;

namespace AnalyticsService.Application.EventHandlers;

public class OrderCreatedEventHandler : IConsumer<OrderCreatedEvent>
{
    private readonly IEventHubPublisher _eventHubPublisher;
    private readonly IMetricsRepository _metricsRepository;
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(
        IEventHubPublisher eventHubPublisher,
        IMetricsRepository metricsRepository,
        ILogger<OrderCreatedEventHandler> logger)
    {
        _eventHubPublisher = eventHubPublisher;
        _metricsRepository = metricsRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var orderEvent = context.Message;
        
        _logger.LogInformation(
            "Processing OrderCreatedEvent for Order {OrderId}", 
            orderEvent.OrderId);

        // Create sales metric for hot storage (MongoDB)
        var salesMetric = new SalesMetric
        {
            Id = Guid.NewGuid(),
            Timestamp = orderEvent.CreatedAt,
            OrderId = orderEvent.OrderId,
            CustomerId = orderEvent.CustomerId,
            TotalAmount = orderEvent.TotalAmount,
            TaxAmount = orderEvent.TaxAmount,
            ItemCount = orderEvent.Items?.Count ?? 0,
            PaymentMethod = orderEvent.PaymentMethod ?? "Unknown",
            Metadata = new Dictionary<string, object>
            {
                ["OrderStatus"] = orderEvent.Status,
                ["ItemDetails"] = orderEvent.Items ?? new List<object>()
            }
        };

        // Save to MongoDB for real-time queries
        await _metricsRepository.SaveSalesMetricAsync(salesMetric);

        // Publish to Event Hub for data lake ingestion
        var eventData = new
        {
            EventType = "OrderCreated",
            EventTime = orderEvent.CreatedAt,
            Data = salesMetric,
            PartitionKey = orderEvent.CustomerId.ToString()
        };

        await _eventHubPublisher.PublishAsync(eventData);

        _logger.LogInformation(
            "OrderCreatedEvent processed successfully for Order {OrderId}", 
            orderEvent.OrderId);
    }
}
```

**ProductViewedEventHandler.cs**
```csharp
using MassTransit;
using AnalyticsService.Application.Services;
using ProductOrderingSystem.Shared.Events;

namespace AnalyticsService.Application.EventHandlers;

public class ProductViewedEventHandler : IConsumer<ProductViewedEvent>
{
    private readonly IEventHubPublisher _eventHubPublisher;
    private readonly IMetricsRepository _metricsRepository;
    private readonly ILogger<ProductViewedEventHandler> _logger;

    public ProductViewedEventHandler(
        IEventHubPublisher eventHubPublisher,
        IMetricsRepository metricsRepository,
        ILogger<ProductViewedEventHandler> logger)
    {
        _eventHubPublisher = eventHubPublisher;
        _metricsRepository = metricsRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductViewedEvent> context)
    {
        var viewEvent = context.Message;
        
        var behaviorMetric = new CustomerBehaviorMetric
        {
            Id = Guid.NewGuid(),
            Timestamp = viewEvent.ViewedAt,
            CustomerId = viewEvent.CustomerId,
            EventType = "ProductView",
            ProductId = viewEvent.ProductId,
            SessionId = viewEvent.SessionId ?? Guid.NewGuid().ToString(),
            Properties = new Dictionary<string, object>
            {
                ["ProductName"] = viewEvent.ProductName ?? "Unknown",
                ["Category"] = viewEvent.Category ?? "Uncategorized",
                ["Source"] = viewEvent.Source ?? "Direct"
            }
        };

        await _metricsRepository.SaveBehaviorMetricAsync(behaviorMetric);
        
        await _eventHubPublisher.PublishAsync(new
        {
            EventType = "ProductViewed",
            EventTime = viewEvent.ViewedAt,
            Data = behaviorMetric,
            PartitionKey = viewEvent.ProductId.ToString()
        });
    }
}
```

#### 2.4 Event Hub Publisher Service

**IEventHubPublisher.cs**
```csharp
namespace AnalyticsService.Application.Services;

public interface IEventHubPublisher
{
    Task PublishAsync<T>(T eventData, CancellationToken cancellationToken = default);
    Task PublishBatchAsync<T>(IEnumerable<T> eventDataBatch, CancellationToken cancellationToken = default);
}
```

**EventHubPublisherService.cs**
```csharp
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System.Text.Json;

namespace AnalyticsService.Infrastructure.EventHub;

public class EventHubPublisherService : IEventHubPublisher, IAsyncDisposable
{
    private readonly EventHubProducerClient _producerClient;
    private readonly ILogger<EventHubPublisherService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public EventHubPublisherService(
        IConfiguration configuration,
        ILogger<EventHubPublisherService> logger)
    {
        var connectionString = configuration["EventHub:ConnectionString"]
            ?? throw new InvalidOperationException("EventHub connection string not configured");
        
        var eventHubName = configuration["EventHub:Name"]
            ?? throw new InvalidOperationException("EventHub name not configured");

        _producerClient = new EventHubProducerClient(connectionString, eventHubName);
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task PublishAsync<T>(T eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(eventData, _jsonOptions);
            var eventDataBatch = await _producerClient.CreateBatchAsync(cancellationToken);
            
            var eventToSend = new EventData(json);
            
            if (!eventDataBatch.TryAdd(eventToSend))
            {
                throw new InvalidOperationException("Event is too large for the batch");
            }

            await _producerClient.SendAsync(eventDataBatch, cancellationToken);
            
            _logger.LogDebug("Published event to Event Hub: {EventType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event to Event Hub");
            throw;
        }
    }

    public async Task PublishBatchAsync<T>(
        IEnumerable<T> eventDataBatch, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var batch = await _producerClient.CreateBatchAsync(cancellationToken);
            
            foreach (var eventData in eventDataBatch)
            {
                var json = JsonSerializer.Serialize(eventData, _jsonOptions);
                var eventToSend = new EventData(json);
                
                if (!batch.TryAdd(eventToSend))
                {
                    // Send current batch and create new one
                    await _producerClient.SendAsync(batch, cancellationToken);
                    batch.Clear();
                    
                    if (!batch.TryAdd(eventToSend))
                    {
                        _logger.LogWarning("Event too large, skipping");
                        continue;
                    }
                }
            }

            if (batch.Count > 0)
            {
                await _producerClient.SendAsync(batch, cancellationToken);
            }
            
            _logger.LogInformation("Published batch of {Count} events to Event Hub", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing batch to Event Hub");
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _producerClient.DisposeAsync();
    }
}
```

#### 2.5 Program.cs Configuration

```csharp
using AnalyticsService.Application.EventHandlers;
using AnalyticsService.Application.Services;
using AnalyticsService.Infrastructure.Data;
using AnalyticsService.Infrastructure.EventHub;
using MassTransit;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// MongoDB Configuration
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = builder.Configuration["MongoDB:ConnectionString"]
        ?? "mongodb://localhost:27017";
    return new MongoClient(connectionString);
});

builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var databaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "AnalyticsDb";
    return client.GetDatabase(databaseName);
});

// Register repositories
builder.Services.AddScoped<IMetricsRepository, MetricsRepository>();

// Event Hub Publisher
builder.Services.AddSingleton<IEventHubPublisher, EventHubPublisherService>();

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Register consumers
    x.AddConsumer<OrderCreatedEventHandler>();
    x.AddConsumer<ProductViewedEventHandler>();
    x.AddConsumer<CartAbandonedEventHandler>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        // Configure endpoints for each consumer
        cfg.ReceiveEndpoint("analytics-order-events", e =>
        {
            e.ConfigureConsumer<OrderCreatedEventHandler>(context);
        });

        cfg.ReceiveEndpoint("analytics-product-events", e =>
        {
            e.ConfigureConsumer<ProductViewedEventHandler>(context);
        });

        cfg.ReceiveEndpoint("analytics-cart-events", e =>
        {
            e.ConfigureConsumer<CartAbandonedEventHandler>(context);
        });
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddMongoDb(builder.Configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017")
    .AddRabbitMQ(rabbitConnectionString: builder.Configuration["RabbitMQ:Host"] ?? "localhost");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

// Metrics API endpoints
app.MapGet("/api/metrics/sales/daily", async (IMetricsRepository repo, DateTime? date) =>
{
    var targetDate = date ?? DateTime.UtcNow.Date;
    var metrics = await repo.GetDailySalesMetricsAsync(targetDate);
    return Results.Ok(metrics);
})
.WithName("GetDailySalesMetrics")
.WithOpenApi();

app.MapGet("/api/metrics/products/top", async (IMetricsRepository repo, int count = 10) =>
{
    var topProducts = await repo.GetTopPerformingProductsAsync(count);
    return Results.Ok(topProducts);
})
.WithName("GetTopProducts")
.WithOpenApi();

app.Run();
```

#### 2.6 Configuration (appsettings.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "AnalyticsDb"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "EventHub": {
    "ConnectionString": "Endpoint=sb://evhns-product-ordering.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=<key>",
    "Name": "analytics-events"
  },
  "DataLake": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=stprodorderanalytics;AccountKey=<key>;EndpointSuffix=core.windows.net",
    "FileSystemName": "analytics-datalake"
  }
}
```

---

### Phase 3: Microsoft Fabric Data Pipelines

#### 3.1 Event Hub to Data Lake Pipeline

**Create Data Pipeline in Fabric**:

1. Navigate to your Fabric workspace
2. Click "New" → "Data Pipeline"
3. Name: "EventHub-to-DataLake-Pipeline"

**Pipeline Activities**:

```python
# Notebook: event_hub_to_bronze.py
# Description: Reads from Event Hub and writes to Bronze layer in Parquet format

from pyspark.sql import SparkSession
from pyspark.sql.functions import *
from pyspark.sql.types import *
from datetime import datetime

# Initialize Spark session
spark = SparkSession.builder.appName("EventHubToBronze").getOrCreate()

# Event Hub configuration
event_hub_namespace = "evhns-product-ordering"
event_hub_name = "analytics-events"
connection_string = "Endpoint=sb://..."  # From Key Vault

# Define schema for incoming events
event_schema = StructType([
    StructField("EventType", StringType(), True),
    StructField("EventTime", TimestampType(), True),
    StructField("Data", StringType(), True),
    StructField("PartitionKey", StringType(), True)
])

# Read from Event Hub
df = (spark
    .readStream
    .format("eventhubs")
    .option("eventhubs.connectionString", connection_string)
    .option("eventhubs.consumerGroup", "$Default")
    .option("maxEventsPerTrigger", 1000)
    .load()
)

# Parse Event Hub body
parsed_df = (df
    .select(
        from_json(col("body").cast("string"), event_schema).alias("event"),
        col("enqueuedTime"),
        col("offset"),
        col("sequenceNumber")
    )
    .select("event.*", "enqueuedTime", "offset", "sequenceNumber")
)

# Add partitioning columns
partitioned_df = (parsed_df
    .withColumn("year", year(col("EventTime")))
    .withColumn("month", month(col("EventTime")))
    .withColumn("day", dayofmonth(col("EventTime")))
)

# Write to Bronze layer (ADLS Gen2)
bronze_path = "abfss://analytics-datalake@stprodorderanalytics.dfs.core.windows.net/raw"

query = (partitioned_df
    .writeStream
    .format("parquet")
    .option("checkpointLocation", f"{bronze_path}/_checkpoints/eventhub")
    .partitionBy("EventType", "year", "month", "day")
    .trigger(processingTime="10 seconds")
    .start(bronze_path)
)

query.awaitTermination()
```

#### 3.2 Bronze to Silver Transformation

```python
# Notebook: bronze_to_silver.py
# Description: Cleanse and validate data from Bronze to Silver layer

from pyspark.sql import SparkSession
from pyspark.sql.functions import *
from delta.tables import *

spark = SparkSession.builder \
    .appName("BronzeToSilver") \
    .config("spark.sql.extensions", "io.delta.sql.DeltaSparkSessionExtension") \
    .config("spark.sql.catalog.spark_catalog", "org.apache.spark.sql.delta.catalog.DeltaCatalog") \
    .getOrCreate()

bronze_path = "abfss://analytics-datalake@stprodorderanalytics.dfs.core.windows.net/raw"
silver_path = "abfss://analytics-datalake@stprodorderanalytics.dfs.core.windows.net/curated"

# Read Bronze data
bronze_df = spark.read.parquet(f"{bronze_path}/OrderCreated")

# Parse JSON data column
from pyspark.sql.types import *

sales_schema = StructType([
    StructField("Id", StringType(), False),
    StructField("Timestamp", TimestampType(), False),
    StructField("OrderId", StringType(), False),
    StructField("CustomerId", StringType(), False),
    StructField("TotalAmount", DecimalType(18, 2), False),
    StructField("TaxAmount", DecimalType(18, 2), False),
    StructField("ItemCount", IntegerType(), False),
    StructField("PaymentMethod", StringType(), True),
    StructField("Region", StringType(), True)
])

# Parse and cleanse
cleansed_df = (bronze_df
    .select(
        from_json(col("Data"), sales_schema).alias("sales"),
        col("EventTime"),
        col("year"),
        col("month"),
        col("day")
    )
    .select("sales.*", "EventTime")
    .filter(col("TotalAmount") > 0)  # Data quality check
    .filter(col("CustomerId").isNotNull())
    .dropDuplicates(["OrderId"])  # Remove duplicates
    .withColumn("ProcessedAt", current_timestamp())
)

# Write to Silver layer as Delta table
silver_table_path = f"{silver_path}/sales_metrics"

(cleansed_df
    .write
    .format("delta")
    .mode("append")
    .option("mergeSchema", "true")
    .save(silver_table_path)
)

print(f"Processed {cleansed_df.count()} records to Silver layer")
```

#### 3.3 Silver to Gold Aggregation

```python
# Notebook: silver_to_gold_aggregations.py
# Description: Create aggregated analytics tables in Gold layer

from pyspark.sql import SparkSession
from pyspark.sql.functions import *
from pyspark.sql.window import Window

spark = SparkSession.builder \
    .appName("SilverToGold") \
    .config("spark.sql.extensions", "io.delta.sql.DeltaSparkSessionExtension") \
    .config("spark.sql.catalog.spark_catalog", "org.apache.spark.sql.delta.catalog.DeltaCatalog") \
    .getOrCreate()

silver_path = "abfss://analytics-datalake@stprodorderanalytics.dfs.core.windows.net/curated"
gold_path = "abfss://analytics-datalake@stprodorderanalytics.dfs.core.windows.net/analytics"

# Read Silver sales data
sales_df = spark.read.format("delta").load(f"{silver_path}/sales_metrics")

# Daily Sales Aggregation
daily_sales = (sales_df
    .withColumn("Date", to_date(col("Timestamp")))
    .groupBy("Date", "Region", "PaymentMethod")
    .agg(
        count("OrderId").alias("OrderCount"),
        sum("TotalAmount").alias("TotalRevenue"),
        sum("TaxAmount").alias("TotalTax"),
        avg("TotalAmount").alias("AverageOrderValue"),
        sum("ItemCount").alias("TotalItemsSold")
    )
    .orderBy("Date", "Region")
)

# Write to Gold layer
(daily_sales
    .write
    .format("delta")
    .mode("overwrite")
    .option("overwriteSchema", "true")
    .save(f"{gold_path}/daily_sales_summary")
)

# Customer Cohort Analysis
customer_metrics = (sales_df
    .groupBy("CustomerId")
    .agg(
        count("OrderId").alias("TotalOrders"),
        sum("TotalAmount").alias("LifetimeValue"),
        min("Timestamp").alias("FirstOrderDate"),
        max("Timestamp").alias("LastOrderDate"),
        avg("TotalAmount").alias("AverageOrderValue")
    )
    .withColumn("DaysSinceFirstOrder", 
                datediff(current_date(), col("FirstOrderDate")))
    .withColumn("DaysSinceLastOrder", 
                datediff(current_date(), col("LastOrderDate")))
    .withColumn("CustomerSegment",
                when(col("TotalOrders") >= 10, "VIP")
                .when(col("TotalOrders") >= 5, "Loyal")
                .when(col("TotalOrders") >= 2, "Repeat")
                .otherwise("New"))
)

(customer_metrics
    .write
    .format("delta")
    .mode("overwrite")
    .save(f"{gold_path}/customer_metrics")
)

print("Gold layer aggregations completed")
```

---

### Phase 4: Power BI Dashboards

#### 4.1 Connect Power BI to Lakehouse

1. Open Power BI Desktop
2. Get Data → More → OneLake data hub
3. Select your Lakehouse: "ProductOrderingLakehouse"
4. Select Gold layer tables:
   - `daily_sales_summary`
   - `customer_metrics`
   - `product_performance`

#### 4.2 Sample Dashboard Components

**Sales Overview Dashboard**:
- KPI Cards:
  - Total Revenue (MTD/YTD)
  - Average Order Value
  - Total Orders
  - Growth % vs Prior Period
  
- Charts:
  - Revenue Trend (Line chart by date)
  - Sales by Region (Map visual)
  - Sales by Payment Method (Donut chart)
  - Top 10 Products (Bar chart)

**Customer Analytics Dashboard**:
- Customer Segmentation (Pie chart)
- Customer Lifetime Value Distribution (Histogram)
- Cohort Retention Analysis (Matrix)
- New vs Returning Customers (Area chart)

**Product Performance Dashboard**:
- Conversion Funnel (Funnel chart: Views → Adds to Cart → Purchases)
- Product Performance Matrix (Table with conditional formatting)
- Category Performance (Treemap)
- Inventory Turnover (Gauge charts)

#### 4.3 DAX Measures

```dax
// Total Revenue
Total Revenue = SUM(daily_sales_summary[TotalRevenue])

// Revenue MTD
Revenue MTD = TOTALMTD([Total Revenue], daily_sales_summary[Date])

// Revenue Growth %
Revenue Growth % = 
VAR CurrentRevenue = [Total Revenue]
VAR PriorRevenue = CALCULATE([Total Revenue], DATEADD(daily_sales_summary[Date], -1, MONTH))
RETURN
DIVIDE(CurrentRevenue - PriorRevenue, PriorRevenue, 0)

// Average Order Value
Average Order Value = DIVIDE([Total Revenue], SUM(daily_sales_summary[OrderCount]))

// Customer Lifetime Value Average
Avg Customer LTV = AVERAGE(customer_metrics[LifetimeValue])

// Conversion Rate
Conversion Rate = 
DIVIDE(
    SUM(product_performance[PurchaseCount]),
    SUM(product_performance[ViewCount]),
    0
) * 100
```

---

### Phase 5: Real-Time Analytics with KQL

#### 5.1 Create Real-Time Analytics in Fabric

1. In Fabric workspace, click "New" → "KQL Database"
2. Name: "ProductOrderingRealTimeAnalytics"
3. Create data connection to Event Hub

#### 5.2 KQL Queries for Real-Time Monitoring

```kql
// Real-time sales monitoring (last 15 minutes)
SalesEvents
| where EventTime > ago(15m)
| summarize 
    Revenue = sum(TotalAmount),
    OrderCount = count(),
    AvgOrderValue = avg(TotalAmount)
    by bin(EventTime, 1m)
| render timechart

// Top products in last hour
ProductViewEvents
| where EventTime > ago(1h)
| summarize ViewCount = count() by ProductId, ProductName
| top 10 by ViewCount desc

// Real-time conversion funnel
let funnel_data = 
    union
        (ProductViewEvents | extend Stage = "View"),
        (CartEvents | where EventType == "AddToCart" | extend Stage = "AddToCart"),
        (OrderEvents | extend Stage = "Purchase")
    | where EventTime > ago(1h);
funnel_data
| summarize Count = count() by Stage
| order by 
    case(Stage == "View", 1, Stage == "AddToCart", 2, Stage == "Purchase", 3, 4)

// Abandoned cart detection (real-time alert)
CartEvents
| where EventType == "AddToCart" and EventTime > ago(30m)
| join kind=leftanti (
    OrderEvents
    | where EventTime > ago(30m)
) on CustomerId
| distinct CustomerId, ProductId, EventTime
| project CustomerId, ProductId, MinutesSinceAbandonment = datetime_diff('minute', now(), EventTime)
```

---

## Event Schema Definitions

### Shared Events (add to ProductOrderingSystem.Shared)

```csharp
namespace ProductOrderingSystem.Shared.Events;

public record ProductViewedEvent(
    Guid ProductId,
    string ProductName,
    string Category,
    Guid CustomerId,
    DateTime ViewedAt,
    string? SessionId = null,
    string? Source = null
);

public record CartAbandonedEvent(
    Guid CartId,
    Guid CustomerId,
    List<CartItemData> Items,
    decimal TotalValue,
    DateTime AbandonedAt,
    string? SessionId = null
);

public record CartItemData(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal Price
);
```

---

## Monitoring and Observability

### Application Insights Integration

```csharp
// In Program.cs
builder.Services.AddApplicationInsightsTelemetry();

// Custom telemetry
builder.Services.AddSingleton<TelemetryClient>();

// In event handlers
private readonly TelemetryClient _telemetry;

public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
{
    var startTime = DateTime.UtcNow;
    var success = false;
    
    try
    {
        // Processing logic
        success = true;
    }
    catch (Exception ex)
    {
        _telemetry.TrackException(ex);
        throw;
    }
    finally
    {
        var duration = DateTime.UtcNow - startTime;
        _telemetry.TrackDependency(
            "EventHub", 
            "PublishEvent", 
            startTime, 
            duration, 
            success);
    }
}
```

### Health Checks Dashboard

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        });
        await context.Response.WriteAsync(result);
    }
});
```

---

## Cost Optimization

### Microsoft Fabric Capacity Planning
- Start with F2 or F4 capacity for development/testing
- Monitor Capacity Metrics in Fabric portal
- Use pause/resume for non-production workspaces

### Data Lake Storage Optimization
- Use lifecycle management policies:
  - Move Bronze data to Cool tier after 30 days
  - Archive data older than 90 days
- Compress Parquet files with Snappy or ZSTD
- Partition data appropriately to reduce scan costs

### Event Hub Optimization
- Use Standard tier with auto-inflate disabled initially
- Monitor throughput units usage
- Consider Event Hub Dedicated for high volume

---

## Testing Strategy

### Unit Tests

```csharp
// OrderCreatedEventHandlerTests.cs
public class OrderCreatedEventHandlerTests
{
    [Fact]
    public async Task Consume_ValidOrderEvent_SavesMetricAndPublishesToEventHub()
    {
        // Arrange
        var mockRepo = new Mock<IMetricsRepository>();
        var mockEventHub = new Mock<IEventHubPublisher>();
        var mockLogger = new Mock<ILogger<OrderCreatedEventHandler>>();
        
        var handler = new OrderCreatedEventHandler(
            mockEventHub.Object,
            mockRepo.Object,
            mockLogger.Object);

        var orderEvent = new OrderCreatedEvent(
            OrderId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            TotalAmount: 150.00m,
            TaxAmount: 15.00m,
            Items: new List<OrderItemData>(),
            Status: "Pending",
            CreatedAt: DateTime.UtcNow,
            PaymentMethod: "CreditCard"
        );

        var context = Mock.Of<ConsumeContext<OrderCreatedEvent>>(
            c => c.Message == orderEvent);

        // Act
        await handler.Consume(context);

        // Assert
        mockRepo.Verify(r => r.SaveSalesMetricAsync(
            It.Is<SalesMetric>(m => m.OrderId == orderEvent.OrderId)), 
            Times.Once);
        
        mockEventHub.Verify(e => e.PublishAsync(
            It.IsAny<object>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
```

### Integration Tests

```csharp
// AnalyticsServiceIntegrationTests.cs
public class AnalyticsServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AnalyticsServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDailySalesMetrics_ReturnsData()
    {
        // Arrange
        var client = _factory.CreateClient();
        var date = DateTime.UtcNow.Date;

        // Act
        var response = await client.GetAsync($"/api/metrics/sales/daily?date={date:yyyy-MM-dd}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }
}
```

---

## Deployment

### Docker Configuration

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/AnalyticsService/AnalyticsService.WebAPI/AnalyticsService.WebAPI.csproj", "AnalyticsService.WebAPI/"]
COPY ["src/Services/AnalyticsService/AnalyticsService.Application/AnalyticsService.Application.csproj", "AnalyticsService.Application/"]
COPY ["src/Services/AnalyticsService/AnalyticsService.Domain/AnalyticsService.Domain.csproj", "AnalyticsService.Domain/"]
COPY ["src/Services/AnalyticsService/AnalyticsService.Infrastructure/AnalyticsService.Infrastructure.csproj", "AnalyticsService.Infrastructure/"]
RUN dotnet restore "AnalyticsService.WebAPI/AnalyticsService.WebAPI.csproj"

COPY src/Services/AnalyticsService/ .
WORKDIR "/src/AnalyticsService.WebAPI"
RUN dotnet build "AnalyticsService.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AnalyticsService.WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AnalyticsService.WebAPI.dll"]
```

### Docker Compose Integration

```yaml
# Add to docker-compose.yml
services:
  analytics-service:
    image: analytics-service:latest
    build:
      context: .
      dockerfile: src/Services/AnalyticsService/AnalyticsService.WebAPI/Dockerfile
    ports:
      - "5010:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - MongoDB__ConnectionString=mongodb://mongodb:27017
      - MongoDB__DatabaseName=AnalyticsDb
      - RabbitMQ__Host=rabbitmq
      - RabbitMQ__Username=guest
      - RabbitMQ__Password=guest
      - EventHub__ConnectionString=${EVENTHUB_CONNECTION_STRING}
      - EventHub__Name=analytics-events
    depends_on:
      - rabbitmq
      - mongodb
    networks:
      - product-ordering-network
```

---

## Security Considerations

### Azure Key Vault Integration

```csharp
// Program.cs
if (builder.Environment.IsProduction())
{
    var keyVaultUri = new Uri(builder.Configuration["KeyVault:Uri"]!);
    builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
}

// Store secrets in Key Vault:
// - EventHub--ConnectionString
// - DataLake--ConnectionString
// - MongoDB--ConnectionString
```

### Managed Identity for Azure Resources

```csharp
// Use Managed Identity for Event Hub
var credential = new DefaultAzureCredential();
var eventHubNamespace = "evhns-product-ordering.servicebus.windows.net";
var eventHubName = "analytics-events";

_producerClient = new EventHubProducerClient(
    eventHubNamespace,
    eventHubName,
    credential);
```

---

## Performance Tuning

### Event Hub Throughput
- Enable Kafka protocol for higher throughput
- Use batch publishing (100-1000 events per batch)
- Configure appropriate partition count (4-32 based on volume)

### Spark Job Optimization
- Use Delta Lake for ACID transactions
- Enable auto-optimize and auto-compaction
- Partition large tables appropriately
- Use Z-ordering for commonly filtered columns

### MongoDB Optimization
```csharp
// Create indexes for hot queries
await collection.Indexes.CreateOneAsync(
    new CreateIndexModel<SalesMetric>(
        Builders<SalesMetric>.IndexKeys
            .Ascending(m => m.Timestamp)
            .Ascending(m => m.CustomerId),
        new CreateIndexOptions { Background = true }
    ));
```

---

## Next Steps

1. **Phase 1**: Set up Azure infrastructure (Data Lake, Event Hub)
2. **Phase 2**: Develop and test Analytics Service locally
3. **Phase 3**: Create Microsoft Fabric workspace and data pipelines
4. **Phase 4**: Build Power BI dashboards
5. **Phase 5**: Deploy to production with monitoring

## Additional Resources

- [Microsoft Fabric Documentation](https://learn.microsoft.com/en-us/fabric/)
- [Azure Data Lake Storage Gen2](https://learn.microsoft.com/en-us/azure/storage/blobs/data-lake-storage-introduction)
- [Azure Event Hubs](https://learn.microsoft.com/en-us/azure/event-hubs/)
- [Delta Lake on Azure](https://learn.microsoft.com/en-us/azure/synapse-analytics/spark/apache-spark-delta-lake-overview)
- [Power BI with Fabric](https://learn.microsoft.com/en-us/power-bi/connect-data/service-dataset-modes-understand)

---

**Document Version**: 1.0  
**Last Updated**: December 9, 2025  
**Author**: Product Ordering System Team
