# Analytics Service - Architecture Diagrams

## Overview
This document provides detailed architectural diagrams for the Analytics Service and its integration with Microsoft Fabric. These diagrams illustrate the data flow, component interactions, and system architecture from multiple perspectives.

---

## 1. High-Level System Architecture

This diagram shows the complete analytics pipeline from local development through Azure cloud services to Microsoft Fabric and Power BI.

```mermaid
flowchart TB
    subgraph Local["Local Development"]
        Services["Order/Payment/Product<br/>Services"]
        RabbitMQ["RabbitMQ<br/>(Message Bus)"]
        Analytics["AnalyticsService<br/>(MassTransit Consumers)"]
        PostgreSQL[("PostgreSQL<br/>(Local Analytics DB)")]
    end
    
    subgraph Azure["Azure Cloud"]
        EventHub["Azure Event Hubs<br/>analytics-events"]
    end
    
    subgraph Fabric["Microsoft Fabric"]
        Eventstream["Eventstream<br/>(Stream Processing)"]
        Lakehouse[("ProductOrderingLakehouse<br/>(OneLake Delta Tables)")]
        SQL["SQL Analytics Endpoint"]
        PowerBI["Power BI<br/>(Dashboards & Reports)"]
    end
    
    Services -->|Publish Events| RabbitMQ
    RabbitMQ -->|Consume Events| Analytics
    Analytics -->|Save Locally| PostgreSQL
    Analytics -->|Stream to Cloud| EventHub
    EventHub -->|Real-time Ingestion| Eventstream
    Eventstream -->|Write Delta Tables| Lakehouse
    Lakehouse -->|Query Data| SQL
    Lakehouse -->|Visualize| PowerBI
    
    style Local fill:#e1f5ff
    style Azure fill:#fff4e1
    style Fabric fill:#f0e1ff
    style Analytics fill:#4CAF50,color:#fff
    style EventHub fill:#FF9800,color:#fff
    style Eventstream fill:#9C27B0,color:#fff
    style Lakehouse fill:#2196F3,color:#fff
```

### Key Components

**Local Development Environment:**
- **Microservices**: OrderService, PaymentService, ProductService, InventoryService generate business events
- **RabbitMQ**: Message broker that enables asynchronous, decoupled communication between services
- **AnalyticsService**: Consumes events from RabbitMQ and implements dual-write pattern
- **PostgreSQL**: Local relational database for development and testing analytics queries

**Azure Cloud:**
- **Event Hubs**: Highly scalable event streaming platform that acts as the bridge between on-premises/local services and cloud analytics
- **analytics-events Hub**: Specific Event Hub configured to receive all analytics events

**Microsoft Fabric:**
- **Eventstream**: Real-time stream processing service that ingests events from Event Hubs
- **ProductOrderingLakehouse**: OneLake-based data lakehouse using Delta Lake format for ACID transactions
- **SQL Analytics Endpoint**: T-SQL interface for querying lakehouse data
- **Power BI**: Business intelligence platform for creating interactive dashboards and reports

### Data Flow Pattern

The architecture implements a **dual-write pattern** where the AnalyticsService simultaneously:
1. Writes to PostgreSQL for local development and testing
2. Streams to Event Hubs for cloud-based analytics and long-term storage

This approach provides:
- **Local Development**: Fast feedback loop without cloud dependencies
- **Cloud Analytics**: Centralized data warehouse for production insights
- **Resilience**: Graceful degradation if Event Hubs is unavailable
- **Flexibility**: Can evolve local and cloud storage independently

---

## 2. Event Processing Sequence Flow

This sequence diagram illustrates the complete lifecycle of an analytics event from user action to dashboard visualization.

```mermaid
sequenceDiagram
    participant User
    participant OrderService
    participant RabbitMQ
    participant Analytics as AnalyticsService
    participant PG as PostgreSQL
    participant EH as Event Hubs
    participant ES as Eventstream
    participant LH as Lakehouse
    participant PBI as Power BI
    
    User->>OrderService: Place Order
    OrderService->>RabbitMQ: Publish OrderCreatedEvent
    RabbitMQ->>Analytics: Consume Event
    
    par Dual Write
        Analytics->>PG: Save OrderEvent
        PG-->>Analytics: Saved ✓
    and
        Analytics->>EH: Publish to Event Hub
        EH-->>Analytics: Accepted ✓
    end
    
    EH->>ES: Stream Event
    ES->>ES: Filter by EventType
    ES->>LH: Write to Delta Table
    LH-->>ES: Written ✓
    
    PBI->>LH: Query Latest Data
    LH-->>PBI: Return Results
    PBI->>User: Display Dashboard
    
    Note over Analytics,EH: Graceful degradation:<br/>EH failures don't break PG saves
```

### Step-by-Step Breakdown

1. **User Interaction**: User places an order through the web UI or API
2. **Event Publication**: OrderService publishes an `OrderCreatedEvent` to RabbitMQ
3. **Event Consumption**: AnalyticsService consumes the event via MassTransit
4. **Dual Write** (Parallel):
   - **PostgreSQL Path**: Event data saved to local PostgreSQL for immediate querying
   - **Event Hubs Path**: Event serialized to JSON and sent to Azure Event Hubs
5. **Cloud Streaming**: Event Hubs streams the event to Fabric Eventstream
6. **Event Filtering**: Eventstream applies filter based on `EventType` field
7. **Schema Mapping**: Event JSON mapped to Delta table schema
8. **Delta Write**: Data written to appropriate Delta table in Lakehouse
9. **Power BI Query**: Dashboard queries lakehouse for latest data
10. **Visualization**: Updated metrics displayed to user

### Parallel Processing Benefits

The parallel dual-write pattern ensures:
- **No Blocking**: PostgreSQL write completes independently of Event Hubs
- **Fast Local Queries**: Development teams can query PostgreSQL immediately
- **Cloud Reliability**: Event Hubs handles temporary failures with retry logic
- **Data Consistency**: Both stores eventually contain the same data

### Error Handling

If Event Hubs fails:
- PostgreSQL write still succeeds
- Error logged as warning (not exception)
- Application continues normally
- Event Hubs automatically retries on next event

---

## 3. Event Routing and Table Mapping

This diagram shows how different event types are filtered and routed to their respective Delta tables in the Lakehouse.

```mermaid
flowchart LR
    subgraph EventHub["Event Hub: analytics-events"]
        Events["All Events<br/>(JSON Stream)"]
    end
    
    subgraph Eventstream["Eventstream Processing"]
        Filter1["Filter:<br/>EventType = OrderEvent"]
        Filter2["Filter:<br/>EventType = PaymentEvent"]
        Filter3["Filter:<br/>EventType = ProductEvent"]
        Filter4["Filter:<br/>EventType = InventoryEvent"]
    end
    
    subgraph Lakehouse["ProductOrderingLakehouse"]
        T1[(OrderEvents)]
        T2[(PaymentEvents)]
        T3[(ProductEvents)]
        T4[(InventoryEvents)]
    end
    
    Events --> Filter1
    Events --> Filter2
    Events --> Filter3
    Events --> Filter4
    
    Filter1 -->|OrderId, CustomerId,<br/>TotalAmount, Status,<br/>ItemCount| T1
    Filter2 -->|PaymentId, OrderId,<br/>Amount, Status,<br/>PaymentMethod| T2
    Filter3 -->|ProductId, Name,<br/>Category, Price| T3
    Filter4 -->|ProductId, OrderId,<br/>QuantityChange| T4
    
    style Filter1 fill:#4CAF50,color:#fff
    style Filter2 fill:#2196F3,color:#fff
    style Filter3 fill:#FF9800,color:#fff
    style Filter4 fill:#9C27B0,color:#fff
```

### Event Type Filtering

The Eventstream processes each incoming event through multiple filter conditions simultaneously:

**OrderEvent Filter:**
- **Condition**: `EventType = "OrderEvent"`
- **Fields Extracted**: OrderId, CustomerId, TotalAmount, Status, ItemCount, EventTimestamp
- **Destination**: `OrderEvents` Delta table
- **Use Cases**: Order volume analysis, order status tracking, revenue calculations

**PaymentEvent Filter:**
- **Condition**: `EventType = "PaymentEvent"`
- **Fields Extracted**: PaymentId, OrderId, Amount, Status, PaymentMethod, EventTimestamp
- **Destination**: `PaymentEvents` Delta table
- **Use Cases**: Payment success rates, revenue reconciliation, payment method preferences

**ProductEvent Filter:**
- **Condition**: `EventType = "ProductEvent"`
- **Fields Extracted**: ProductId, Name, Category, Price, EventType (Created/Updated), EventTimestamp
- **Destination**: `ProductEvents` Delta table
- **Use Cases**: Product catalog changes, pricing history, product lifecycle tracking

**InventoryEvent Filter:**
- **Condition**: `EventType = "InventoryEvent"`
- **Fields Extracted**: ProductId, OrderId, QuantityChange, QuantityAfter, EventType (Reserved/Released), EventTimestamp
- **Destination**: `InventoryEvents` Delta table
- **Use Cases**: Inventory turnover analysis, stock level trends, reservation patterns

### Benefits of Separate Tables

**Query Performance:**
- Each table optimized for specific query patterns
- Smaller tables mean faster scans
- Targeted indexes on relevant columns

**Schema Evolution:**
- Each event type can evolve independently
- No need for sparse columns or complex unions
- Type-safe queries with predictable schemas

**Access Control:**
- Fine-grained permissions per table
- Different teams can own different event types
- Easier compliance and auditing

**Cost Optimization:**
- Only query tables needed for specific analytics
- Reduce compute costs by scanning less data
- Better partition pruning per event type

---

## 4. AnalyticsService Clean Architecture

This diagram illustrates the internal structure of the AnalyticsService following Clean Architecture principles.

```mermaid
flowchart TB
    subgraph WebAPI["WebAPI Layer"]
        Endpoints["Minimal API Endpoints<br/>/api/analytics/*"]
        Program["Program.cs<br/>(Service Registration)"]
    end
    
    subgraph Application["Application Layer"]
        Queries["MediatR Queries<br/>GetAnalyticsSummary<br/>GetDailyOrders<br/>GetDailyRevenue<br/>GetPopularProducts"]
        IEventHub["IEventHubPublisher<br/>(Interface)"]
    end
    
    subgraph Infrastructure["Infrastructure Layer"]
        Consumers["MassTransit Consumers<br/>OrderCreated<br/>PaymentProcessed<br/>ProductCreated<br/>InventoryReserved"]
        Handlers["Query Handlers<br/>(LINQ to PostgreSQL)"]
        EventPub["EventHubPublisher<br/>(Azure SDK)"]
        DbContext["AnalyticsDbContext<br/>(EF Core)"]
    end
    
    subgraph Domain["Domain Layer"]
        Entities["Entities<br/>OrderEvent<br/>PaymentEvent<br/>ProductEvent<br/>InventoryEvent"]
    end
    
    subgraph External["External Systems"]
        RMQ[RabbitMQ]
        PG[(PostgreSQL)]
        EH[Event Hubs]
    end
    
    RMQ -->|Events| Consumers
    Consumers -->|Save| DbContext
    Consumers -->|Publish| EventPub
    DbContext -->|Write/Read| PG
    EventPub -->|Stream| EH
    Endpoints -->|Send Query| Queries
    Queries -->|Handle| Handlers
    Handlers -->|Query| DbContext
    DbContext -->|Use| Entities
    EventPub -.->|Implements| IEventHub
    
    style WebAPI fill:#e3f2fd
    style Application fill:#fff3e0
    style Infrastructure fill:#f3e5f5
    style Domain fill:#e8f5e9
    style External fill:#fce4ec
```

### Layer Responsibilities

**Domain Layer (Core):**
- **Pure entities**: No dependencies on external frameworks
- **Business rules**: Validation logic embedded in entities
- **Value objects**: Immutable data structures
- **No external dependencies**: Can be tested in isolation
- **Examples**: OrderEvent, PaymentEvent with properties and validation

**Application Layer (Use Cases):**
- **MediatR Queries**: Define what the system can do
- **Interfaces**: Abstract external dependencies (IEventHubPublisher)
- **DTOs**: Data transfer objects for queries and responses
- **Orchestration**: Coordinate domain entities and infrastructure
- **Examples**: GetAnalyticsSummary query, GetDailyOrders query

**Infrastructure Layer (Plumbing):**
- **MassTransit Consumers**: Handle message consumption from RabbitMQ
- **Query Handlers**: Implement Application layer queries using EF Core
- **EventHubPublisher**: Concrete implementation of IEventHubPublisher
- **DbContext**: Entity Framework Core database context
- **External SDKs**: Azure Event Hubs SDK, Npgsql driver

**WebAPI Layer (Entry Point):**
- **Minimal APIs**: HTTP endpoints using .NET 10 Minimal API pattern
- **Service Registration**: Dependency injection configuration
- **Middleware**: Logging, error handling, health checks
- **API Documentation**: OpenAPI/Swagger integration

### Dependency Flow

Dependencies flow **inward** toward the Domain:
- WebAPI → Application → Domain
- Infrastructure → Application → Domain
- No layer depends on outer layers
- Application defines interfaces, Infrastructure implements them

### Benefits of This Architecture

**Testability:**
- Domain can be unit tested without any infrastructure
- Application queries tested with in-memory implementations
- Infrastructure tested with integration tests

**Maintainability:**
- Clear separation of concerns
- Easy to locate and modify code
- Changes in one layer don't ripple through others

**Flexibility:**
- Can swap PostgreSQL for another database
- Can replace Event Hubs with Kafka or other systems
- Framework-agnostic business logic

**Scalability:**
- Each layer can be optimized independently
- Clear boundaries for microservice decomposition
- Easy to add new queries or consumers

---

## 5. End-to-End Data Flow Journey

This diagram visualizes the complete journey of data from user interaction to Power BI dashboard across five distinct phases.

```mermaid
flowchart TB
    Start([User Places Order])
    
    subgraph Phase1["Phase 1: Order Creation"]
        Order[Order Service<br/>Creates Order]
        Publish1[Publish to<br/>RabbitMQ]
    end
    
    subgraph Phase2["Phase 2: Analytics Capture"]
        Consume[Analytics Service<br/>Consumes Event]
        SavePG[Save to<br/>PostgreSQL]
        SendEH[Send to<br/>Event Hubs]
    end
    
    subgraph Phase3["Phase 3: Cloud Processing"]
        Stream[Eventstream<br/>Processes]
        Filter[Apply Filter<br/>EventType]
        Map[Map Schema<br/>to Delta]
    end
    
    subgraph Phase4["Phase 4: Storage & Analytics"]
        Write[Write to<br/>Delta Table]
        Store[OneLake<br/>Storage]
    end
    
    subgraph Phase5["Phase 5: Visualization"]
        Query[Power BI<br/>Queries]
        Dash[Dashboard<br/>Updates]
    end
    
    End([Real-time Analytics])
    
    Start --> Order
    Order --> Publish1
    Publish1 --> Consume
    Consume --> SavePG
    Consume --> SendEH
    SendEH --> Stream
    Stream --> Filter
    Filter --> Map
    Map --> Write
    Write --> Store
    Store --> Query
    Query --> Dash
    Dash --> End
    
    style Start fill:#4CAF50,color:#fff
    style End fill:#4CAF50,color:#fff
    style Phase1 fill:#e3f2fd
    style Phase2 fill:#fff3e0
    style Phase3 fill:#f3e5f5
    style Phase4 fill:#e8f5e9
    style Phase5 fill:#fce4ec
```

### Phase-by-Phase Analysis

**Phase 1: Order Creation (Local/Development)**
- **Duration**: < 100ms
- **Components**: OrderService, RabbitMQ
- **Action**: User submits order through UI, OrderService validates and creates order, publishes event to RabbitMQ
- **Output**: OrderCreatedEvent in RabbitMQ queue

**Phase 2: Analytics Capture (Dual Path)**
- **Duration**: < 200ms (parallel)
- **Components**: AnalyticsService, PostgreSQL, Event Hubs
- **Action**: 
  - MassTransit consumer receives event
  - Simultaneously saves to PostgreSQL (fast, local)
  - Sends to Event Hubs (network call to Azure)
- **Output**: Data in PostgreSQL + Event in Event Hubs

**Phase 3: Cloud Processing (Azure/Fabric)**
- **Duration**: < 500ms
- **Components**: Fabric Eventstream
- **Action**:
  - Eventstream ingests from Event Hubs
  - Applies EventType filter to route to correct table
  - Maps JSON fields to Delta table schema
- **Output**: Transformed event ready for storage

**Phase 4: Storage & Analytics (Fabric Lakehouse)**
- **Duration**: < 1 second
- **Components**: OneLake, Delta tables
- **Action**:
  - Writes to Delta table with ACID guarantees
  - Updates table statistics and metadata
  - Optimizes file layout for query performance
- **Output**: Queryable data in lakehouse

**Phase 5: Visualization (Power BI)**
- **Duration**: 5-15 seconds (refresh interval)
- **Components**: Power BI Service/Desktop
- **Action**:
  - Power BI queries SQL analytics endpoint
  - Retrieves latest data for dashboard
  - Renders updated visualizations
- **Output**: Real-time dashboard showing latest metrics

### Total Latency

**End-to-End**: Typically 5-20 seconds from order placement to dashboard update

**Optimization Opportunities:**
- Use Power BI DirectQuery for < 5 second latency
- Configure Eventstream for batch writes (higher throughput, slightly higher latency)
- Optimize Delta table partitioning for query performance

---

## 6. Eventstream Configuration Structure

This diagram shows the actual Eventstream configuration with source, filters, and destinations as they appear in Microsoft Fabric.

```mermaid
flowchart LR
    subgraph Source["Source Configuration"]
        EH["Azure Event Hubs<br/>evhns-product-ordering<br/>analytics-events<br/>Consumer: $Default"]
    end
    
    subgraph Transform["Event Processing"]
        direction TB
        E1[EventType =<br/>OrderEvent]
        E2[EventType =<br/>PaymentEvent]
        E3[EventType =<br/>ProductEvent]
        E4[EventType =<br/>InventoryEvent]
    end
    
    subgraph Dest["Lakehouse Destinations"]
        direction TB
        D1[(OrderEvents<br/>Table)]
        D2[(PaymentEvents<br/>Table)]
        D3[(ProductEvents<br/>Table)]
        D4[(InventoryEvents<br/>Table)]
    end
    
    EH --> E1
    EH --> E2
    EH --> E3
    EH --> E4
    
    E1 --> D1
    E2 --> D2
    E3 --> D3
    E4 --> D4
    
    style EH fill:#FF9800,color:#fff
    style E1 fill:#4CAF50,color:#fff
    style E2 fill:#2196F3,color:#fff
    style E3 fill:#FF5722,color:#fff
    style E4 fill:#9C27B0,color:#fff
```

### Configuration Details

**Source Configuration:**
- **Type**: Azure Event Hubs
- **Namespace**: evhns-product-ordering.servicebus.windows.net
- **Event Hub**: analytics-events
- **Consumer Group**: $Default (or dedicated: fabric-consumer)
- **Authentication**: Shared Access Key
- **Data Format**: JSON
- **Compression**: None

**Event Processing (Transformation Layer):**

Each filter represents a separate data flow in Eventstream:

1. **OrderEvent Filter** (Green):
   - Expression: `EventType = "OrderEvent"`
   - Processes: Order creation events
   - Fields: OrderId, CustomerId, TotalAmount, Status, ItemCount

2. **PaymentEvent Filter** (Blue):
   - Expression: `EventType = "PaymentEvent"`
   - Processes: Payment processing events
   - Fields: PaymentId, OrderId, Amount, Status, PaymentMethod

3. **ProductEvent Filter** (Orange):
   - Expression: `EventType = "ProductEvent"`
   - Processes: Product lifecycle events
   - Fields: ProductId, Name, Category, Price

4. **InventoryEvent Filter** (Purple):
   - Expression: `EventType = "InventoryEvent"`
   - Processes: Inventory changes
   - Fields: ProductId, OrderId, QuantityChange, QuantityAfter

**Lakehouse Destinations:**

Each destination writes to a dedicated Delta table:
- **Write Mode**: Append (insert-only)
- **Format**: Delta Lake
- **Storage**: OneLake (Azure Data Lake Storage Gen2)
- **Partitioning**: By EventTimestamp (optional, for large volumes)
- **Indexing**: Auto-indexed on key columns

### Monitoring This Configuration

In Fabric Eventstream Monitor tab, you can view:
- **Source Events/sec**: Rate of events from Event Hubs
- **Filtered Events**: Events matching each filter
- **Destination Writes**: Successful writes to each table
- **Errors**: Failed transformations or writes
- **Latency**: End-to-end processing time

---

## 7. Alternative Simpler Architecture (Single Table)

For teams wanting a simpler initial setup, this diagram shows a single-table approach.

```mermaid
flowchart LR
    subgraph Source["Source"]
        EH["Azure Event Hubs<br/>analytics-events"]
    end
    
    subgraph Transform["Processing"]
        NoFilter["No Filtering<br/>(All Events)"]
    end
    
    subgraph Dest["Lakehouse"]
        AllEvents[("AllAnalyticsEvents<br/>(Single Table)<br/><br/>Columns:<br/>EventType<br/>EventData (JSON)<br/>Timestamp")]
    end
    
    EH --> NoFilter
    NoFilter --> AllEvents
    
    style EH fill:#FF9800,color:#fff
    style NoFilter fill:#9E9E9E,color:#fff
    style AllEvents fill:#2196F3,color:#fff
```

### Single Table Schema

```sql
CREATE TABLE AllAnalyticsEvents (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    EventType VARCHAR(50) NOT NULL,
    EventData NVARCHAR(MAX),  -- Full JSON
    Timestamp DATETIME2 NOT NULL,
    PartitionKey VARCHAR(100)
)
```

### Querying Single Table

```sql
-- Get orders
SELECT 
    EventType,
    JSON_VALUE(EventData, '$.Data.OrderId') AS OrderId,
    JSON_VALUE(EventData, '$.Data.TotalAmount') AS TotalAmount,
    Timestamp
FROM AllAnalyticsEvents
WHERE EventType = 'OrderEvent'
ORDER BY Timestamp DESC;

-- Get payments
SELECT 
    EventType,
    JSON_VALUE(EventData, '$.Data.PaymentId') AS PaymentId,
    JSON_VALUE(EventData, '$.Data.Status') AS Status,
    Timestamp
FROM AllAnalyticsEvents
WHERE EventType = 'PaymentEvent'
ORDER BY Timestamp DESC;
```

### Pros and Cons

**Advantages:**
- ✅ Simpler Eventstream setup (1 destination vs 4)
- ✅ Faster initial implementation
- ✅ No need to maintain multiple schemas
- ✅ Flexible for ad-hoc queries
- ✅ Can evolve to separate tables later

**Disadvantages:**
- ❌ Slower queries (must scan entire table)
- ❌ JSON parsing overhead
- ❌ No type safety in queries
- ❌ Harder to optimize with indexes
- ❌ All-or-nothing access control

### When to Use Single Table

**Good for:**
- Proof of concept / MVP
- Low event volumes (< 1M events/month)
- Exploratory analytics
- Quick prototyping

**Not recommended for:**
- High query performance requirements
- Large event volumes (> 10M events/month)
- Production workloads with SLAs
- Teams needing separate table ownership

### Migration Path

Start with single table, then:
1. Create separate tables using Spark notebooks
2. Migrate historical data with SQL
3. Update Eventstream to use filters
4. Deprecate single table

---

## 8. Power BI Dashboard Architecture

This diagram shows how Power BI connects to the Lakehouse and builds real-time dashboards.

```mermaid
flowchart TB
    subgraph Lakehouse["ProductOrderingLakehouse"]
        T1[(OrderEvents)]
        T2[(PaymentEvents)]
        T3[(ProductEvents)]
        T4[(InventoryEvents)]
        SQL["SQL Analytics<br/>Endpoint"]
        
        T1 --> SQL
        T2 --> SQL
        T3 --> SQL
        T4 --> SQL
    end
    
    subgraph PowerBI["Power BI"]
        Dataset["Semantic Model<br/>(Dataset)"]
        
        subgraph Measures["DAX Measures"]
            M1["Total Orders"]
            M2["Total Revenue"]
            M3["Avg Order Value"]
            M4["Success Rate"]
        end
        
        subgraph Visuals["Dashboard Visuals"]
            V1["Order Trend<br/>(Line Chart)"]
            V2["Revenue by Day<br/>(Bar Chart)"]
            V3["Top Products<br/>(Table)"]
            V4["Payment Status<br/>(Pie Chart)"]
            V5["KPI Cards"]
        end
    end
    
    subgraph Users["End Users"]
        Desktop["Power BI Desktop"]
        Service["Power BI Service"]
        Mobile["Power BI Mobile"]
        Teams["Microsoft Teams"]
    end
    
    SQL -->|DirectQuery or Import| Dataset
    Dataset --> M1
    Dataset --> M2
    Dataset --> M3
    Dataset --> M4
    
    M1 --> V1
    M2 --> V2
    Dataset --> V3
    M4 --> V4
    M1 --> V5
    M2 --> V5
    
    V1 --> Desktop
    V1 --> Service
    V1 --> Mobile
    V1 --> Teams
    
    style Lakehouse fill:#e8f5e9
    style PowerBI fill:#fff3e0
    style Users fill:#e3f2fd
```

### Power BI Integration Options

**DirectQuery Mode:**
- Queries Lakehouse in real-time
- Always shows latest data
- No data import required
- Higher latency per query
- Best for: Real-time dashboards

**Import Mode:**
- Copies data into Power BI
- Faster query performance
- Scheduled refresh (e.g., every 15 minutes)
- Lower latency
- Best for: Historical analysis

### Sample DAX Measures

```dax
Total Orders = COUNT(OrderEvents[OrderId])

Total Revenue = 
    CALCULATE(
        SUM(PaymentEvents[Amount]),
        PaymentEvents[Status] = "succeeded"
    )

Avg Order Value = 
    DIVIDE([Total Revenue], [Total Orders], 0)

Payment Success Rate = 
    DIVIDE(
        CALCULATE(COUNT(PaymentEvents[PaymentId]), PaymentEvents[Status] = "succeeded"),
        COUNT(PaymentEvents[PaymentId])
    )

Top Products = 
    TOPN(
        10,
        SUMMARIZE(
            InventoryEvents,
            ProductEvents[Name],
            "Quantity Sold", SUM(InventoryEvents[QuantityChange])
        ),
        [Quantity Sold],
        DESC
    )
```

---

## Summary

These diagrams provide multiple perspectives on the Analytics Service architecture:

1. **System Architecture**: Overall integration with Azure and Fabric
2. **Event Processing**: Detailed sequence of operations
3. **Event Routing**: How events are filtered and stored
4. **Clean Architecture**: Internal service structure
5. **Data Flow**: End-to-end journey
6. **Eventstream Config**: Actual Fabric configuration
7. **Single Table Alternative**: Simpler approach
8. **Power BI Integration**: Dashboard architecture

Each diagram serves a specific purpose:
- **Technical teams**: Use Clean Architecture and Event Processing diagrams
- **Business stakeholders**: Focus on Data Flow and Power BI diagrams
- **Operations teams**: Reference System Architecture and Eventstream Config
- **New team members**: Start with System Architecture and progress through others

For implementation details, see:
- [Fabric Eventstream Setup Guide](./Fabric-Eventstream-Setup.md)
- [Analytics Event Hub Integration](./Analytics-EventHub-Integration.md)
