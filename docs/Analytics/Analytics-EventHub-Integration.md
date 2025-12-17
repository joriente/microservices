# Analytics Service - Event Hub Integration

## Overview
The AnalyticsService now streams events in real-time to Azure Event Hubs, enabling Microsoft Fabric integration for advanced analytics and Power BI dashboards.

## Architecture

```
Order/Payment/Product Events (RabbitMQ)
    ↓
MassTransit Consumer (AnalyticsService)
    ├─ PostgreSQL (Local analytics, development)
    └─ Azure Event Hubs (Cloud streaming)
         ↓
    Microsoft Fabric Eventstream
         ↓
    ProductOrderingLakehouse (OneLake Delta Tables)
         ↓
    Power BI (Real-time dashboards)
```

## Event Types

The AnalyticsService publishes 4 types of events to Event Hub:

1. **OrderEvent** - When orders are created
2. **PaymentEvent** - When payments are processed
3. **ProductEvent** - When products are created
4. **InventoryEvent** - When inventory is reserved

## Configuration

### Your Azure Resources
- **Event Hub Namespace**: `evhns-product-ordering.servicebus.windows.net`
- **Storage Account**: `stprodorderanalytics`
- **Event Hub Name**: `analytics-events`

### Setup Steps

#### 1. Create Event Hub (if it doesn't exist)
```bash
# Or use Azure Portal:
# Navigate to Event Hubs Namespace -> Event Hubs -> + Event Hub
# Name: analytics-events
```

#### 2. Get Connection String
From Azure Portal:
1. Go to Event Hubs Namespace: `evhns-product-ordering`
2. Settings → Shared access policies
3. Select or create a policy with **Send** permissions
4. Copy the **Connection string-primary key**

#### 3. Configure User Secrets (Development)
```powershell
dotnet user-secrets set "EventHub:ConnectionString" "YOUR_CONNECTION_STRING_HERE" --project src/Services/AnalyticsService/ProductOrderingSystem.AnalyticsService.WebAPI
```

#### 4. Configure Microsoft Fabric Eventstream

1. **Open Fabric Workspace** with ProductOrderingLakehouse
2. **Create Eventstream**
   - Name: `ProductOrdering-Analytics`
3. **Configure Source**
   - Type: `Azure Event Hubs`
   - Cloud connection: Create new or select existing
   - Event Hub namespace: `evhns-product-ordering.servicebus.windows.net`
   - Event Hub: `analytics-events`
   - Consumer group: `$Default` (or create dedicated: `fabric-consumer`)
   - Authentication: Shared Access Key
4. **Configure Destination**
   - Type: `Lakehouse`
   - Workspace: Your workspace
   - Lakehouse: `ProductOrderingLakehouse`
   - Delta table names:
     - `OrderEvents`
     - `PaymentEvents`
     - `ProductEvents`
     - `InventoryEvents`
   - Input data format: `Json`

## Event Schema

### OrderEvent
```json
{
  "EventType": "OrderEvent",
  "Timestamp": "2025-12-16T10:30:00Z",
  "Data": {
    "OrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "CustomerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "TotalAmount": 99.99,
    "Status": "Created",
    "ItemCount": 3
  }
}
```

### PaymentEvent
```json
{
  "EventType": "PaymentEvent",
  "Timestamp": "2025-12-16T10:30:05Z",
  "Data": {
    "PaymentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "OrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "Amount": 99.99,
    "Status": "succeeded",
    "PaymentMethod": "USD"
  }
}
```

### ProductEvent
```json
{
  "EventType": "ProductEvent",
  "Timestamp": "2025-12-16T09:00:00Z",
  "Data": {
    "ProductId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "Name": "Laptop",
    "Category": "Electronics",
    "Price": 999.99,
    "EventType": "Created"
  }
}
```

### InventoryEvent
```json
{
  "EventType": "InventoryEvent",
  "Timestamp": "2025-12-16T10:30:03Z",
  "Data": {
    "ProductId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "OrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "QuantityChange": -2,
    "QuantityAfter": 0,
    "EventType": "Reserved"
  }
}
```

## Graceful Degradation

The AnalyticsService implements graceful degradation:

- ✅ **With Event Hub configured**: Events go to PostgreSQL + Event Hub
- ✅ **Without Event Hub configured**: Events only go to PostgreSQL (no errors)
- ✅ **Event Hub failures**: Logged as warnings, PostgreSQL save still succeeds

This allows local development without Azure connectivity.

## Monitoring

### Verify Events in Event Hub
1. Azure Portal → Event Hubs Namespace → Event Hub (`analytics-events`)
2. Check **Metrics** → Incoming Messages
3. Use **Process data** → Test connection

### Verify Events in Fabric
1. Fabric Workspace → Eventstream → Monitor tab
2. Check event flow from source to destination
3. Query Lakehouse tables:
```sql
SELECT * FROM ProductOrderingLakehouse.OrderEvents
ORDER BY Timestamp DESC
LIMIT 10;
```

### Verify Events in PostgreSQL (Local)
```sql
SELECT * FROM "OrderEvents" ORDER BY "EventTimestamp" DESC LIMIT 10;
SELECT * FROM "PaymentEvents" ORDER BY "EventTimestamp" DESC LIMIT 10;
SELECT * FROM "ProductEvents" ORDER BY "EventTimestamp" DESC LIMIT 10;
SELECT * FROM "InventoryEvents" ORDER BY "EventTimestamp" DESC LIMIT 10;
```

## Power BI Integration

Once events flow to Lakehouse, create Power BI reports:

1. **Open Power BI Desktop** or **Power BI Service**
2. **Get Data** → OneLake data hub
3. **Select** ProductOrderingLakehouse tables
4. **Create Real-time Reports**:
   - Daily order volume
   - Revenue trends
   - Payment success rates
   - Popular products
   - Inventory turnover

## Troubleshooting

### Events not appearing in Event Hub
- Verify connection string is set in user secrets
- Check EventHubPublisher logs in console
- Verify Event Hub exists and has Send permissions
- Check Azure Portal metrics for incoming messages

### Events not appearing in Fabric
- Verify Eventstream is running (not paused)
- Check Eventstream monitor tab for errors
- Verify consumer group in Eventstream matches Event Hub
- Check authentication/connection to Event Hub

### Build errors
- Ensure `Azure.Messaging.EventHubs` package is in Directory.Packages.props
- Run `dotnet restore` and `dotnet build`

## Production Considerations

- **Connection String**: Use Azure Key Vault or Managed Identity in production
- **Consumer Groups**: Create dedicated consumer group for Fabric (e.g., `fabric-consumer`)
- **Partitioning**: Event Hub auto-partitions by partition key (can add OrderId as key)
- **Retention**: Configure Event Hub retention (default 1 day, max 7 days for Standard tier)
- **Throughput**: Monitor throughput units if event volume increases
- **Error Handling**: Review EventHubPublisher logs for any send failures

## Cost Optimization

- **Event Hub**: Basic tier is sufficient for development
- **Fabric**: Compute auto-pauses when idle
- **Storage Account**: Use Cool tier for long-term analytics data
- **Eventstream**: No additional cost, part of Fabric capacity

## Next Steps

1. ✅ Set Event Hub connection string in user secrets
2. ✅ Create `analytics-events` Event Hub in Azure Portal
3. ✅ Configure Fabric Eventstream with source and destination
4. ✅ Run the application and place a test order
5. ✅ Verify events flow: PostgreSQL → Event Hub → Fabric → Lakehouse
6. ✅ Create Power BI report from Lakehouse tables


connection string
dotnet user-secrets set "EventHub:ConnectionString" "Endpoint=sb://evhns-product-ordering.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=ttr3fovZ0598iQKkEFwVIRiG7BQLcLIuz+AEhL3vlwk="  --project src/Services/AnalyticsService/ProductOrderingSystem.AnalyticsService.WebAPI