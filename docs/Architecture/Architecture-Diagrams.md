---
tags:
  - architecture
  - diagrams
  - system-design
  - microservices
  - mermaid
  - event-driven
  - aspire
---

# Product Ordering System - Architecture Diagrams

This document contains comprehensive architecture diagrams showing the system's structure, data flow, and event-driven communication patterns.

## Table of Contents
1. [High-Level System Architecture](#high-level-system-architecture)
2. [Service Communication Flow](#service-communication-flow)
3. [Order Processing Flow](#order-processing-flow)
4. [Event-Driven Architecture](#event-driven-architecture)
5. [Database Architecture](#database-architecture)

---

## High-Level System Architecture

### System Overview

```mermaid
flowchart TB
    subgraph "Frontend"
        UI[Blazor WebAssembly<br/>Port 5261]
    end
    
    subgraph "API Layer"
        GW[API Gateway - Yarp<br/>Port 5000]
    end
    
    subgraph ".NET Microservices"
        ID[IdentityService<br/>Port 5001<br/>MongoDB]
        PROD[ProductService<br/>Port 5002<br/>MongoDB]
        CART[CartService<br/>Port 5003<br/>MongoDB]
        ORD[OrderService<br/>Port 5004<br/>MongoDB]
        PAY[PaymentService<br/>Port 5005<br/>MongoDB]
        CUST[CustomerService<br/>Port 5006<br/>MongoDB]
        INV[InventoryService<br/>Port 5007<br/>PostgreSQL]
        ANAL[AnalyticsService<br/>Port 5008<br/>MongoDB]
    end
    
    subgraph "Java Microservices"
        NOT[NotificationService<br/>Port 8085<br/>Java/Spring Boot<br/>MongoDB]
    end
    
    subgraph "Infrastructure"
        MDB[(MongoDB)]
        PG[(PostgreSQL)]
        RMQ[RabbitMQ<br/>Message Broker]
    end
    
    subgraph "Azure Cloud Services"
        FABRIC[Microsoft Fabric<br/>Advanced Analytics]
        DATALAKE[Azure Data Lake<br/>Long-term Storage]
    end
    
    subgraph "External Services"
        STRIPE[Stripe API]
        SENDGRID[SendGrid API]
    end
    
    UI --> GW
    GW --> ID & PROD & CART & ORD & PAY & CUST & INV & ANAL
    
    ID & PROD & CART & ORD & PAY & CUST & ANAL -.-> MDB
    INV -.-> PG
    NOT -.-> MDB
    
    ID & PROD & CART & ORD & PAY & CUST & INV & ANAL --> RMQ
    RMQ --> NOT & ANAL
    
    ANAL -.-> FABRIC
    ANAL -.-> DATALAKE
    
    PAY --> STRIPE
    NOT --> SENDGRID
    
    style UI fill:#e1f5ff
    style GW fill:#fff3e0
    style ID fill:#f3e5f5
    style PROD fill:#f3e5f5
    style CART fill:#f3e5f5
    style ORD fill:#f3e5f5
    style PAY fill:#f3e5f5
    style CUST fill:#f3e5f5
    style INV fill:#e8f5e9
    style ANAL fill:#e3f2fd
    style NOT fill:#fff9c4
    style RMQ fill:#ffebee
    style MDB fill:#e0f2f1
    style PG fill:#e0f2f1
    style FABRIC fill:#fce4ec
    style DATALAKE fill:#f3e5f5
```

### Technology Stack

```mermaid
flowchart LR
    subgraph "Frontend"
        FE1[Blazor WebAssembly]
        FE2[MudBlazor UI]
        FE3["C&num; 13"]
    end
    
    subgraph ".NET Services"
        BE1[.NET 9.0]
        BE2[ASP.NET Core]
        BE3[MassTransit]
        BE4[EF Core 9]
    end
    
    subgraph "Java Services"
        JA1[Java 21]
        JA2[Spring Boot 3.4]
        JA3[Spring AMQP]
        JA4[Spring Data]
    end
    
    subgraph "Databases"
        DB1[(MongoDB 8)]
        DB2[(PostgreSQL 17)]
    end
    
    subgraph "Messaging"
        MSG[RabbitMQ 4.0]
    end
    
    subgraph "Observability"
        OBS2[.NET Aspire Dashboard]
        OBS3[Spring Actuator]
    end
    
    FE1 & FE2 & FE3 --> BE1
    BE1 --> BE2 & BE3 & BE4
    JA1 --> JA2 & JA3 & JA4
    BE3 & JA3 --> MSG
    BE4 --> DB2
    BE2 & JA4 --> DB1
    BE2 --> OBS2
    JA2 --> OBS3
```

---

## Service Communication Flow

### Customer Shopping Journey

```mermaid
sequenceDiagram
    participant User
    participant Frontend
    participant Gateway as API Gateway
    participant Identity as IdentityService
    participant Product as ProductService
    participant Cart as CartService
    participant Order as OrderService
    participant Payment as PaymentService
    participant Inventory as InventoryService
    participant Notification as NotificationService<br/>(Java)
    participant RabbitMQ
    
    User->>Frontend: Browse Products
    Frontend->>Gateway: GET /api/products
    Gateway->>Product: Forward Request
    Product-->>Gateway: Product List
    Gateway-->>Frontend: Product List
    Frontend-->>User: Display Products
    
    User->>Frontend: Login
    Frontend->>Gateway: POST /api/auth/login
    Gateway->>Identity: Forward Request
    Identity-->>Gateway: JWT Token
    Gateway-->>Frontend: JWT Token
    Frontend-->>User: Logged In
    
    User->>Frontend: Add to Cart
    Frontend->>Gateway: POST /api/cart/items<br/>[Authorization: Bearer token]
    Gateway->>Cart: Forward Request
    Cart-->>Gateway: Cart Updated
    Gateway-->>Frontend: Success
    
    User->>Frontend: Checkout
    Frontend->>Gateway: POST /api/orders<br/>[Authorization: Bearer token]
    Gateway->>Order: Create Order
    Order->>RabbitMQ: Publish OrderCreatedEvent
    Order-->>Gateway: Order Created (201)
    Gateway-->>Frontend: Order ID
    
    RabbitMQ->>Inventory: OrderCreatedEvent
    Inventory->>Inventory: Reserve Stock
    Inventory->>RabbitMQ: Publish InventoryReservedEvent
    
    RabbitMQ->>Cart: OrderCreatedEvent
    Cart->>Cart: Clear Customer Cart
    
    RabbitMQ->>Notification: OrderCreatedEvent
    Notification->>Notification: Send Confirmation Email
    
    Frontend->>Gateway: POST /api/payments<br/>[Authorization: Bearer token]
    Gateway->>Payment: Process Payment
    Payment->>Payment: Call Stripe API
    
    alt Payment Success
        Payment->>RabbitMQ: Publish PaymentProcessedEvent
        RabbitMQ->>Inventory: PaymentProcessedEvent
        Inventory->>Inventory: Commit Reservation
        Inventory->>RabbitMQ: Publish InventoryCommittedEvent
        RabbitMQ->>Order: InventoryCommittedEvent
        Order->>Order: Update Status: Processing
        RabbitMQ->>Notification: PaymentProcessedEvent
        Notification->>Notification: Send Payment Success Email
    else Payment Failed
        Payment->>RabbitMQ: Publish PaymentFailedEvent
        RabbitMQ->>Inventory: PaymentFailedEvent
        Inventory->>Inventory: Release Reservation
        RabbitMQ->>Order: PaymentFailedEvent
        Order->>Order: Update Status: Cancelled
        RabbitMQ->>Notification: PaymentFailedEvent
        Notification->>Notification: Send Payment Failed Email
    end
    
    Payment-->>Gateway: Payment Result
    Gateway-->>Frontend: Payment Result
    Frontend-->>User: Order Confirmation
```

---

## Order Processing Flow

### Complete Order Lifecycle

```mermaid
flowchart TD
    Start([Customer Places Order]) --> CreateOrder[OrderService<br/>Creates Order]
    CreateOrder --> PublishOrderCreated[Publish<br/>OrderCreatedEvent]
    
    PublishOrderCreated --> |RabbitMQ| ClearCart[CartService<br/>Clear Cart]
    PublishOrderCreated --> |RabbitMQ| ReserveInventory[InventoryService<br/>Reserve Stock]
    PublishOrderCreated --> |RabbitMQ| SendOrderEmail[NotificationService<br/>Send Confirmation]
    
    ReserveInventory --> CheckStock{Stock<br/>Available?}
    CheckStock -->|Yes| PublishReserved[Publish<br/>InventoryReservedEvent]
    CheckStock -->|No| PublishFailed[Publish<br/>InventoryReservationFailedEvent]
    
    PublishReserved --> ProcessPayment[PaymentService<br/>Process Payment]
    ProcessPayment --> CallStripe[Call Stripe API]
    
    CallStripe --> PaymentResult{Payment<br/>Success?}
    
    PaymentResult -->|Success| PublishPaymentSuccess[Publish<br/>PaymentProcessedEvent]
    PaymentResult -->|Failed| PublishPaymentFailed[Publish<br/>PaymentFailedEvent]
    
    PublishPaymentSuccess --> |RabbitMQ| CommitInventory[InventoryService<br/>Commit Reservation]
    PublishPaymentSuccess --> |RabbitMQ| UpdateOrderSuccess[OrderService<br/>Status: Processing]
    PublishPaymentSuccess --> |RabbitMQ| SendPaymentSuccessEmail[NotificationService<br/>Payment Success Email]
    
    PublishPaymentFailed --> |RabbitMQ| ReleaseInventory[InventoryService<br/>Release Reservation]
    PublishPaymentFailed --> |RabbitMQ| UpdateOrderFailed[OrderService<br/>Status: Cancelled]
    PublishPaymentFailed --> |RabbitMQ| SendPaymentFailedEmail[NotificationService<br/>Payment Failed Email]
    
    CommitInventory --> PublishCommitted[Publish<br/>InventoryCommittedEvent]
    PublishCommitted --> |RabbitMQ| FinalizeOrder[OrderService<br/>Finalize Order]
    
    ReleaseInventory --> CancelOrder[OrderService<br/>Cancel Order]
    
    UpdateOrderSuccess --> OrderComplete([Order Complete])
    UpdateOrderFailed --> OrderCancelled([Order Cancelled])
    CancelOrder --> OrderCancelled
    FinalizeOrder --> OrderComplete
    
    style CreateOrder fill:#e1f5ff
    style ProcessPayment fill:#fff3e0
    style ReserveInventory fill:#e8f5e9
    style CommitInventory fill:#e8f5e9
    style SendOrderEmail fill:#fff9c4
    style SendPaymentSuccessEmail fill:#fff9c4
    style SendPaymentFailedEmail fill:#fff9c4
    style OrderComplete fill:#c8e6c9
    style OrderCancelled fill:#ffcdd2
```

### Saga Pattern - Compensation Flow

```mermaid
flowchart TB
    subgraph "Happy Path"
        H1[Order Created] --> H2[Inventory Reserved]
        H2 --> H3[Payment Processed]
        H3 --> H4[Inventory Committed]
        H4 --> H5[Order Complete]
    end
    
    subgraph "Compensation Path"
        C1[Order Created] --> C2[Inventory Reserved]
        C2 --> C3[Payment Failed]
        C3 --> C4[Inventory Released<br/>Compensation]
        C4 --> C5[Order Cancelled<br/>Compensation]
    end
    
    style H5 fill:#c8e6c9
    style C5 fill:#ffcdd2
    style C4 fill:#ffe0b2
    style C5 fill:#ffe0b2
```

---

## Event-Driven Architecture

### Event Flow Map

```mermaid
flowchart LR
    subgraph "Event Publishers"
        PROD_PUB[ProductService]
        ORD_PUB[OrderService]
        PAY_PUB[PaymentService]
        INV_PUB[InventoryService]
    end
    
    subgraph "RabbitMQ Message Broker"
        PROD_EX[ProductCreatedEvent<br/>Exchange]
        ORD_EX[OrderCreatedEvent<br/>Exchange]
        PAY_EX[PaymentProcessedEvent<br/>PaymentFailedEvent<br/>Exchanges]
        INV_EX[InventoryReservedEvent<br/>InventoryCommittedEvent<br/>Exchanges]
    end
    
    subgraph "Event Consumers"
        CART_CON[CartService]
        ORD_CON[OrderService]
        INV_CON[InventoryService]
        NOT_CON[NotificationService<br/>Java]
        ANAL_CON[AnalyticsService<br/>Metrics & BI]
    end
    
    PROD_PUB -->|Publish| PROD_EX
    ORD_PUB -->|Publish| ORD_EX
    PAY_PUB -->|Publish| PAY_EX
    INV_PUB -->|Publish| INV_EX
    
    PROD_EX -->|Subscribe| CART_CON
    PROD_EX -->|Subscribe| ANAL_CON
    ORD_EX -->|Subscribe| CART_CON
    ORD_EX -->|Subscribe| INV_CON
    ORD_EX -->|Subscribe| NOT_CON
    ORD_EX -->|Subscribe| ANAL_CON
    
    PAY_EX -->|Subscribe| INV_CON
    PAY_EX -->|Subscribe| ORD_CON
    PAY_EX -->|Subscribe| NOT_CON
    PAY_EX -->|Subscribe| ANAL_CON
    
    INV_EX -->|Subscribe| ORD_CON
    INV_EX -->|Subscribe| ANAL_CON
    
    style PROD_PUB fill:#f3e5f5
    style ORD_PUB fill:#f3e5f5
    style PAY_PUB fill:#f3e5f5
    style INV_PUB fill:#e8f5e9
    style NOT_CON fill:#fff9c4
    style CART_CON fill:#f3e5f5
    style ORD_CON fill:#f3e5f5
    style INV_CON fill:#e8f5e9
```

### Event Types and Consumers

```mermaid
flowchart TD
    subgraph "Product Events"
        PE1[ProductCreatedEvent]
        PE2[ProductUpdatedEvent]
        PE3[ProductDeletedEvent]
    end
    
    subgraph "Order Events"
        OE1[OrderCreatedEvent]
        OE2[OrderCancelledEvent]
    end
    
    subgraph "Payment Events"
        PAE1[PaymentProcessedEvent]
        PAE2[PaymentFailedEvent]
    end
    
    subgraph "Inventory Events"
        IE1[InventoryReservedEvent]
        IE2[InventoryCommittedEvent]
        IE3[InventoryReservationFailedEvent]
    end
    
    PE1 & PE2 & PE3 -.->|Cache Product| CART[CartService]
    OE1 -.->|Clear Cart| CART
    OE1 -.->|Reserve Stock| INV[InventoryService]
    OE1 -.->|Send Email| NOT[NotificationService]
    
    PAE1 -.->|Commit Stock| INV
    PAE1 -.->|Update Status| ORD[OrderService]
    PAE1 -.->|Send Email| NOT
    
    PAE2 -.->|Release Stock| INV
    PAE2 -.->|Cancel Order| ORD
    PAE2 -.->|Send Email| NOT
    
    IE1 & IE2 & IE3 -.->|Update Status| ORD
    
    style CART fill:#f3e5f5
    style INV fill:#e8f5e9
    style NOT fill:#fff9c4
    style ORD fill:#f3e5f5
```

---

## Database Architecture

### Database-Per-Service Pattern

```mermaid
flowchart TB
    subgraph "MongoDB Cluster - Port 27017"
        DB1[(identitydb)]
        DB2[(productdb)]
        DB3[(cartdb)]
        DB4[(orderdb)]
        DB5[(paymentdb)]
        DB6[(customerdb)]
        DB7[(notificationdb)]
    end
    
    subgraph "PostgreSQL - Port 5432"
        DB8[(inventorydb)]
    end
    
    subgraph "Services"
        S1[IdentityService]
        S2[ProductService]
        S3[CartService]
        S4[OrderService]
        S5[PaymentService]
        S6[CustomerService]
        S7[NotificationService<br/>Java]
        S8[InventoryService]
    end
    
    S1 -.->|MongoDB Driver| DB1
    S2 -.->|MongoDB Driver| DB2
    S3 -.->|MongoDB Driver| DB3
    S4 -.->|MongoDB Driver| DB4
    S5 -.->|MongoDB Driver| DB5
    S6 -.->|MongoDB Driver| DB6
    S7 -.->|Spring Data MongoDB| DB7
    S8 -.->|EF Core| DB8
    
    style DB8 fill:#e8f5e9
    style S8 fill:#e8f5e9
    style S7 fill:#fff9c4
    style DB1 fill:#e0f2f1
    style DB2 fill:#e0f2f1
    style DB3 fill:#e0f2f1
    style DB4 fill:#e0f2f1
    style DB5 fill:#e0f2f1
    style DB6 fill:#e0f2f1
    style DB7 fill:#e0f2f1
```

### Data Models by Service

```mermaid
flowchart LR
    subgraph "IdentityService - MongoDB"
        I1[User<br/>- Email<br/>- Password Hash<br/>- JWT Tokens]
    end
    
    subgraph "ProductService - MongoDB"
        P1[Product<br/>- Name<br/>- Description<br/>- Price<br/>- Stock]
    end
    
    subgraph "CartService - MongoDB"
        C1[Cart<br/>- CustomerId<br/>- Items<br/>- Total]
        C2[ProductCache<br/>- ProductId<br/>- Name<br/>- Price]
    end
    
    subgraph "OrderService - MongoDB"
        O1[Order<br/>- CustomerId<br/>- Items<br/>- Status<br/>- Total]
    end
    
    subgraph "PaymentService - MongoDB"
        PA1[Payment<br/>- OrderId<br/>- Amount<br/>- StripeId<br/>- Status]
    end
    
    subgraph "CustomerService - MongoDB"
        CU1[Customer<br/>- Name<br/>- Email<br/>- Addresses]
    end
    
    subgraph "InventoryService - PostgreSQL"
        IN1[InventoryItem<br/>- ProductId<br/>- Quantity<br/>- Reserved]
        IN2[InventoryReservation<br/>- OrderId<br/>- ProductId<br/>- Quantity]
    end
    
    subgraph "NotificationService - MongoDB"
        N1[Notification<br/>- OrderId<br/>- Type<br/>- Status<br/>- Email]
    end
    
    subgraph "AnalyticsService - MongoDB + Fabric + Data Lake"
        A1[OrderMetrics<br/>- OrderId<br/>- Revenue<br/>- Timestamp]
        A2[ProductViews<br/>- ProductId<br/>- ViewCount<br/>- Timestamp]
        A3[CartEvents<br/>- CartId<br/>- EventType<br/>- Abandoned]
    end
    
    style IN1 fill:#e8f5e9
    style IN2 fill:#e8f5e9
    style N1 fill:#fff9c4
    style A1 fill:#e3f2fd
    style A2 fill:#e3f2fd
    style A3 fill:#e3f2fd
```

---

## Deployment Architecture

### Container Organization

```mermaid
flowchart TB
    subgraph "Docker Containers - ProductOrdering Project"
        subgraph "Infrastructure"
            RMQ[ProductOrdering-rabbitmq<br/>Ports: 5672, 15672]
            MONGO[ProductOrdering-mongodb<br/>Port: 27017]
            PG[ProductOrdering-postgres<br/>Port: 5432]
        end
        
        subgraph "Management UIs"
            PGADMIN[pgAdmin<br/>PostgreSQL UI]
            MONGOEX[Mongo Express<br/>MongoDB UI]
            RMQUI[RabbitMQ Management<br/>Port 15672]
        end
    end
    
    subgraph "Aspire Orchestrated Services"
        ASP[Aspire Dashboard<br/>Port 15888]
        SVCS[7 .NET Microservices<br/>Ports 5001-5007]
        GW2[API Gateway<br/>Port 5000]
        FE[Frontend<br/>Port 5261]
    end
    
    subgraph "Standalone Services"
        JAVA[NotificationService<br/>Java/Spring Boot<br/>Port 8085]
    end
    
    RMQ & MONGO & PG -.-> SVCS & JAVA
    SVCS --> ASP
    GW2 --> SVCS
    FE --> GW2
    
    PGADMIN -.-> PG
    MONGOEX -.-> MONGO
    RMQUI -.-> RMQ
    
    style RMQ fill:#ffebee
    style MONGO fill:#e0f2f1
    style PG fill:#e0f2f1
    style JAVA fill:#fff9c4
    style ASP fill:#e1f5ff
```

### Port Allocation

```mermaid
flowchart LR
    subgraph "Service Ports"
        P5000[5000 - API Gateway]
        P5001[5001 - IdentityService]
        P5002[5002 - ProductService]
        P5003[5003 - CartService]
        P5004[5004 - OrderService]
        P5005[5005 - PaymentService]
        P5006[5006 - CustomerService]
        P5007[5007 - InventoryService]
        P8085[8085 - NotificationService]
        P5261[5261 - Frontend]
    end
    
    subgraph "Infrastructure Ports"
        P15888[15888 - Aspire Dashboard]
        P15672[15672 - RabbitMQ UI]
        P27017[27017 - MongoDB]
        P5432[5432 - PostgreSQL]
        P8081[8081 - Mongo Express]
    end
    
    style P8085 fill:#fff9c4
    style P5007 fill:#e8f5e9
```

---

## Security Architecture

### Authentication & Authorization Flow

```mermaid
sequenceDiagram
    participant User
    participant Frontend
    participant Gateway
    participant Identity as IdentityService
    participant Service as Any Microservice
    
    User->>Frontend: Enter Credentials
    Frontend->>Gateway: POST /api/auth/login
    Gateway->>Identity: Forward Request
    Identity->>Identity: Validate Credentials
    Identity->>Identity: Generate JWT Token
    Identity-->>Gateway: JWT Token + User Info
    Gateway-->>Frontend: JWT Token + User Info
    Frontend->>Frontend: Store Token in Memory
    
    Note over User,Service: Subsequent Requests
    
    User->>Frontend: Request Resource
    Frontend->>Gateway: GET /api/resource<br/>[Authorization: Bearer TOKEN]
    Gateway->>Gateway: Validate JWT Signature
    Gateway->>Gateway: Check Token Expiration
    Gateway->>Service: Forward with Token
    Service->>Service: Extract User Claims
    Service->>Service: Authorize Action
    Service-->>Gateway: Resource Data
    Gateway-->>Frontend: Resource Data
    Frontend-->>User: Display Data
```

### JWT Token Structure

```mermaid
flowchart LR
    subgraph "JWT Token"
        direction TB
        H[Header<br/>- Algorithm: HS256<br/>- Type: JWT]
        P[Payload<br/>- UserId<br/>- Email<br/>- Roles<br/>- Expiration]
        S[Signature<br/>- HMAC SHA256<br/>- Secret Key]
    end
    
    H --> E[Encoded Token]
    P --> E
    S --> E
    
    E --> GW[API Gateway<br/>Validates Signature]
    GW --> MS[Microservices<br/>Extract Claims]
```

---

## Observability & Monitoring

### Logging and Tracing

```mermaid
flowchart TB
    subgraph "Services"
        S1[IdentityService]
        S2[ProductService]
        S3[CartService]
        S4[OrderService]
        S5[PaymentService]
        S6[CustomerService]
        S7[InventoryService]
        S8[NotificationService]
        S9[API Gateway]
    end
    
    subgraph "Logging Infrastructure"
        ASP[Aspire Dashboard<br/>Centralized Logging & Telemetry<br/>Port 15888]
    end
    
    subgraph "Monitoring"
        ACT[Spring Actuator<br/>Java Metrics<br/>Port 8085/actuator]
    end
    
    S1 & S2 & S3 & S4 & S5 & S6 & S7 & S9 -->|Serilog| ASP
    S8 -->|SLF4J/Logback| ACT
    
    S1 & S2 & S3 & S4 & S5 & S6 & S7 & S9 -->|Telemetry| ASP
    S8 -->|Metrics| ACT
    
    style ASP fill:#e1f5ff
    style S8 fill:#fff9c4
```

### Correlation Tracking

```mermaid
sequenceDiagram
    participant Frontend
    participant Gateway
    participant OrderService
    participant RabbitMQ
    participant InventoryService
    participant PaymentService
    participant Aspire as Aspire Dashboard
    
    Note over Frontend,Aspire: CorrelationId: abc-123-xyz
    
    Frontend->>Gateway: Create Order [CorrelationId: abc-123-xyz]
    Gateway->>Aspire: Log: Request Received
    Gateway->>OrderService: Forward Request [CorrelationId: abc-123-xyz]
    OrderService->>Aspire: Log: Order Created
    OrderService->>RabbitMQ: Publish Event [CorrelationId: abc-123-xyz]
    
    RabbitMQ->>InventoryService: OrderCreatedEvent [CorrelationId: abc-123-xyz]
    InventoryService->>Aspire: Log: Stock Reserved
    
    RabbitMQ->>PaymentService: OrderCreatedEvent [CorrelationId: abc-123-xyz]
    PaymentService->>Aspire: Log: Payment Initiated
    
    Note over Aspire: All logs linked by CorrelationId<br/>Easy to trace complete flow in Dashboard
```

---

## Analytics Architecture

### Analytics Service Data Flow

```mermaid
flowchart TB
    subgraph "Event Sources"
        PROD[ProductService<br/>ProductViewedEvent]
        ORD[OrderService<br/>OrderCreatedEvent]
        CART[CartService<br/>CartAbandonedEvent]
        PAY[PaymentService<br/>PaymentProcessedEvent]
        INV[InventoryService<br/>InventoryCommittedEvent]
    end
    
    subgraph "Messaging Layer"
        RMQ[RabbitMQ<br/>Event Bus]
    end
    
    subgraph "Analytics Service"
        CONS[Event Consumers<br/>MassTransit]
        PROC[Data Processors<br/>Aggregation & Enrichment]
        REPO[Repository Layer]
    end
    
    subgraph "Data Storage"
        MONGO[(MongoDB<br/>Hot Data<br/>Real-time Metrics)]
    end
    
    subgraph "Azure Cloud Analytics"
        FABRIC[Microsoft Fabric<br/>- Data Warehouse<br/>- Lakehouses<br/>- Real-time Analytics<br/>- Power BI]
        DATALAKE[(Azure Data Lake<br/>Storage Gen2<br/>- Historical Data<br/>- Long-term Archive)]
    end
    
    subgraph "Visualization"
        DASH[Real-time Dashboards<br/>- Sales Metrics<br/>- Product Performance<br/>- Customer Behavior]
        POWERBI[Power BI Reports<br/>- Executive Dashboards<br/>- Trend Analysis]
    end
    
    PROD & ORD & CART & PAY & INV -->|Publish| RMQ
    RMQ -->|Subscribe| CONS
    CONS --> PROC
    PROC --> REPO
    REPO --> MONGO
    
    REPO -.->|Batch Upload| DATALAKE
    DATALAKE --> FABRIC
    MONGO -.->|Stream| FABRIC
    
    MONGO --> DASH
    FABRIC --> POWERBI
    
    style CONS fill:#e3f2fd
    style PROC fill:#e3f2fd
    style REPO fill:#e3f2fd
    style MONGO fill:#e0f2f1
    style FABRIC fill:#fce4ec
    style DATALAKE fill:#f3e5f5
    style DASH fill:#fff9c4
    style POWERBI fill:#fff9c4
```

### Analytics Event Processing

```mermaid
sequenceDiagram
    participant Order as OrderService
    participant RMQ as RabbitMQ
    participant Analytics as AnalyticsService
    participant Mongo as MongoDB
    participant Lake as Azure Data Lake
    participant Fabric as Microsoft Fabric
    
    Order->>RMQ: Publish OrderCreatedEvent
    RMQ->>Analytics: Consume Event
    Analytics->>Analytics: Extract Metrics<br/>(Revenue, Product IDs)
    Analytics->>Mongo: Store Real-time Metrics
    
    Note over Analytics,Lake: Batch Process (Hourly)
    
    Analytics->>Lake: Upload Aggregated Data
    Lake->>Fabric: Trigger Data Pipeline
    Fabric->>Fabric: Process & Transform
    Fabric->>Fabric: Update Data Warehouse
    
    Note over Fabric: Power BI refreshes dashboards
```

### Analytics Data Models

```mermaid
flowchart LR
    subgraph "MongoDB Collections (Hot Data)"
        M1[order_metrics<br/>- OrderId<br/>- Revenue<br/>- ProductCount<br/>- Timestamp]
        M2[product_views<br/>- ProductId<br/>- ViewCount<br/>- Timestamp<br/>- Source]
        M3[cart_events<br/>- CartId<br/>- EventType<br/>- Abandoned<br/>- Timestamp<br/>- Items]
        M4[customer_behavior<br/>- CustomerId<br/>- ActionType<br/>- Metadata]
    end
    
    subgraph "Azure Data Lake (Cold Data)"
        L1[Historical Orders<br/>Parquet Files]
        L2[Product Analytics<br/>Parquet Files]
        L3[Customer Journey<br/>Parquet Files]
    end
    
    subgraph "Microsoft Fabric"
        F1[Data Warehouse<br/>Dimensional Model]
        F2[Lakehouse<br/>Delta Tables]
        F3[Real-time Hub<br/>Event Streams]
    end
    
    M1 & M2 & M3 & M4 -.->|Batch Export| L1 & L2 & L3
    L1 & L2 & L3 --> F2
    F2 --> F1
    M1 & M2 & M3 & M4 -.->|Stream| F3
    F3 --> F2
    
    style M1 fill:#e3f2fd
    style M2 fill:#e3f2fd
    style M3 fill:#e3f2fd
    style M4 fill:#e3f2fd
    style L1 fill:#f3e5f5
    style L2 fill:#f3e5f5
    style L3 fill:#f3e5f5
    style F1 fill:#fce4ec
    style F2 fill:#fce4ec
    style F3 fill:#fce4ec
```

### Analytics Use Cases

```mermaid
flowchart TB
    subgraph "Real-time Analytics"
        RT1[Sales Dashboard<br/>Current day revenue]
        RT2[Product Performance<br/>Top sellers today]
        RT3[Cart Abandonment<br/>Live tracking]
        RT4[Customer Activity<br/>Active users]
    end
    
    subgraph "Batch Analytics"
        BA1[Trend Analysis<br/>Monthly/Yearly trends]
        BA2[Customer Segmentation<br/>RFM Analysis]
        BA3[Inventory Forecasting<br/>Demand prediction]
        BA4[Revenue Attribution<br/>Channel performance]
    end
    
    subgraph "Advanced Analytics (Fabric)"
        AA1[Machine Learning<br/>Product Recommendations]
        AA2[Predictive Analytics<br/>Churn Prediction]
        AA3[Anomaly Detection<br/>Fraud Detection]
        AA4[Cohort Analysis<br/>User Retention]
    end
    
    MONGO[(MongoDB<br/>Real-time Data)] --> RT1 & RT2 & RT3 & RT4
    LAKE[(Azure Data Lake<br/>Historical Data)] --> BA1 & BA2 & BA3 & BA4
    FABRIC[Microsoft Fabric<br/>Unified Analytics] --> AA1 & AA2 & AA3 & AA4
    
    style RT1 fill:#e3f2fd
    style RT2 fill:#e3f2fd
    style RT3 fill:#e3f2fd
    style RT4 fill:#e3f2fd
    style BA1 fill:#fff9c4
    style BA2 fill:#fff9c4
    style BA3 fill:#fff9c4
    style BA4 fill:#fff9c4
    style AA1 fill:#fce4ec
    style AA2 fill:#fce4ec
    style AA3 fill:#fce4ec
    style AA4 fill:#fce4ec
```

---

## Summary

These diagrams illustrate:

✅ **High-Level Architecture** - 8 microservices (7 .NET + 1 Java) with polyglot persistence  
✅ **Event-Driven Communication** - RabbitMQ-based async messaging  
✅ **Saga Pattern** - Distributed transaction handling with compensation  
✅ **Database-Per-Service** - Independent data stores (MongoDB + PostgreSQL)  
✅ **Analytics Architecture** - Real-time + batch analytics with Microsoft Fabric and Azure Data Lake  
✅ **Security** - JWT-based authentication and authorization  
✅ **Observability** - Centralized logging and distributed tracing  
✅ **Polyglot Architecture** - .NET and Java services working together  

For detailed implementation, see:
- [Analytics-Service-Implementation.md](../Services/Analytics-Service-Implementation.md) - Analytics service details
- [POLYGLOT_INTEGRATION.md](../Messaging/POLYGLOT_INTEGRATION.md) - Java/.NET integration
- [Event-Naming-Conventions.md](../Messaging/Event-Naming-Conventions.md) - Event contracts
- [MESSAGING_IMPLEMENTATION.md](../Messaging/MESSAGING_IMPLEMENTATION.md) - RabbitMQ patterns
- [SAGA_COMPENSATION_IMPLEMENTATION.md](../Messaging/SAGA_COMPENSATION_IMPLEMENTATION.md) - Saga implementation
