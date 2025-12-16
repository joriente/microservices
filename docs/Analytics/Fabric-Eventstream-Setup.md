# Microsoft Fabric Eventstream Setup Guide

## Overview
This guide walks you through configuring Microsoft Fabric Eventstream to receive real-time analytics events from Azure Event Hubs and store them in your ProductOrderingLakehouse.

> 📊 **Architecture Diagrams**: For detailed architectural diagrams with explanations, see [Analytics-Architecture-Diagrams.md](./Analytics-Architecture-Diagrams.md)

## Prerequisites
- ✅ Azure Event Hubs namespace: `evhns-product-ordering.servicebus.windows.net`
- ✅ Event Hub created: `analytics-events`
- ✅ Microsoft Fabric workspace with ProductOrderingLakehouse
- ✅ AnalyticsService configured with Event Hub connection string
- ✅ Shared Access Key with Send permissions

---

## Step-by-Step Configuration

### Step 1: Navigate to Your Fabric Workspace

1. Open your browser and navigate to [Microsoft Fabric](https://app.fabric.microsoft.com)
2. Sign in with your Azure credentials
3. Click on **Workspaces** in the left navigation
4. Select or navigate to the workspace containing your **ProductOrderingLakehouse**

### Step 2: Create a New Eventstream

1. In your workspace, click **+ New** button (top left corner)
2. Scroll down to the **Real-Time Intelligence** section
3. Select **Eventstream**
4. In the creation dialog:
   - **Name:** `ProductOrdering-Analytics`
   - **Description:** (optional) "Real-time analytics events from Product Ordering System"
5. Click **Create**
6. Wait for the Eventstream designer to open (shows a blank canvas)

### Step 3: Add Azure Event Hubs as Source

#### 3.1 Add Source
1. In the Eventstream canvas, click **Add source** or click the **New source** button
2. From the source types list, select **Azure Event Hubs**

#### 3.2 Configure Connection
1. **Cloud connection:**
   - Click **+ New connection** or **Create new connection**
   - Fill in the connection details:
     - **Connection name:** `ProductOrdering-EventHub`
     - **Event Hub namespace:** `evhns-product-ordering.servicebus.windows.net`
     - **Authentication kind:** Shared Access Key
     - **Shared Access Key name:** `RootManageSharedAccessKey`
     - **Shared Access Key:** `ttr3fovZ0598iQKkEFwVIRiG7BQLcLIuz+AEhL3vlwk=`
   - Click **Create** or **Connect**

#### 3.3 Configure Source Settings
1. **Event Hub:** Select or enter `analytics-events`
2. **Consumer group:** Select `$Default` 
   - *Recommended for production:* Create dedicated consumer group `fabric-consumer` in Azure Portal
3. **Data format:** `Json`
4. **Compression type:** `None`
5. **Event processing time:** Leave default (Event time)

#### 3.4 Complete Source Setup
1. Review the configuration
2. Click **Add** or **Next**
3. The Event Hub source should now appear on the canvas

### Step 4: Add Lakehouse Destinations

**IMPORTANT:** Before adding destinations, ensure your Event Hub source is properly configured and saved. The source must be active on the canvas before you can connect destinations to it.

You'll create **4 separate destinations**, one for each event type. This allows proper schema mapping and querying.

> ⚠️ **Common Issue**: If you get an error "this operation is missing an input to work", it means:
> 1. The Event Hub source hasn't been added/saved yet - complete Step 3 first
> 2. You need to **drag a connection** from the Event Hub source node to the destination
> 3. Or click the **+** button on the Event Hub source node, then select "Add destination"
>
> **Solution**: After adding the source, click on the Event Hub source node on the canvas, then click the **+ Add destination** button that appears, or drag from the source output to create a new destination.

---

#### Destination 1: OrderEvents Table

##### 4.1.1 Add Destination
1. **Method 1 (Recommended)**: Click on the Event Hub source node on the canvas, then click **+ Add destination** or the **+** icon
2. **Method 2**: Drag a connection line from the Event Hub source output port to a blank area, then select **Lakehouse**
3. **Method 3**: Use the top menu **Add destination** → **Lakehouse**
4. Select **Lakehouse** as the destination type

##### 4.1.2 Configure Lakehouse Connection
1. **Workspace:** Select your current workspace from dropdown
2. **Lakehouse:** Select `ProductOrderingLakehouse` from dropdown
3. **Delta table:** 
   - Select **Create new** or **New table** option
   - Table name: `OrderEvents`
4. Ensure there's a **connection line** from the Event Hub source to this destination on the canvas

##### 4.1.3 Add Event Filter
1. In the transformation/processing section, click **Add filter** or **Add event processor**
2. Configure the filter:
   - **Field:** `EventType`
   - **Condition:** `Equals`
   - **Value:** `OrderEvent`
   - This ensures only order events go to this table

##### 4.1.4 Configure Schema Mapping
Map the JSON fields to table columns:

| Source Field | Destination Column | Data Type |
|--------------|-------------------|-----------|
| `Data.OrderId` | `OrderId` | string |
| `Data.CustomerId` | `CustomerId` | string |
| `Data.TotalAmount` | `TotalAmount` | decimal |
| `Data.Status` | `Status` | string |
| `Data.ItemCount` | `ItemCount` | int |
| `Timestamp` | `EventTimestamp` | timestamp |
| `EventType` | `EventType` | string |

##### 4.1.5 Configure Write Mode
- **Write mode:** `Append`
- **Checkpoint location:** Auto (default)

##### 4.1.6 Save
1. Review the configuration
2. Click **Add** or **Save**

---

#### Destination 2: PaymentEvents Table

##### 4.2.1 Add Another Destination
1. Click **Add destination** again from the Event Hub source
2. Select **Lakehouse**

##### 4.2.2 Configure Connection
1. **Workspace:** Your workspace
2. **Lakehouse:** `ProductOrderingLakehouse`
3. **Delta table:** `PaymentEvents` (new table)

##### 4.2.3 Filter Configuration
- **Field:** `EventType`
- **Condition:** `Equals`
- **Value:** `PaymentEvent`

##### 4.2.4 Schema Mapping

| Source Field | Destination Column | Data Type |
|--------------|-------------------|-----------|
| `Data.PaymentId` | `PaymentId` | string |
| `Data.OrderId` | `OrderId` | string |
| `Data.Amount` | `Amount` | decimal |
| `Data.Status` | `Status` | string |
| `Data.PaymentMethod` | `PaymentMethod` | string |
| `Timestamp` | `EventTimestamp` | timestamp |
| `EventType` | `EventType` | string |

##### 4.2.5 Save
Click **Add** or **Save**

---

#### Destination 3: ProductEvents Table

##### 4.3.1 Add Destination
1. Add another Lakehouse destination
2. Select `ProductOrderingLakehouse`
3. Create table: `ProductEvents`

##### 4.3.2 Filter
- **Field:** `EventType`
- **Condition:** `Equals`
- **Value:** `ProductEvent`

##### 4.3.3 Schema Mapping

| Source Field | Destination Column | Data Type |
|--------------|-------------------|-----------|
| `Data.ProductId` | `ProductId` | string |
| `Data.Name` | `Name` | string |
| `Data.Category` | `Category` | string |
| `Data.Price` | `Price` | decimal |
| `Data.EventType` | `ProductEventType` | string |
| `Timestamp` | `EventTimestamp` | timestamp |
| `EventType` | `EventType` | string |

##### 4.3.4 Save
Click **Add** or **Save**

---

#### Destination 4: InventoryEvents Table

##### 4.4.1 Add Destination
1. Add final Lakehouse destination
2. Select `ProductOrderingLakehouse`
3. Create table: `InventoryEvents`

##### 4.4.2 Filter
- **Field:** `EventType`
- **Condition:** `Equals`
- **Value:** `InventoryEvent`

##### 4.4.3 Schema Mapping

| Source Field | Destination Column | Data Type |
|--------------|-------------------|-----------|
| `Data.ProductId` | `ProductId` | string |
| `Data.OrderId` | `OrderId` | string |
| `Data.QuantityChange` | `QuantityChange` | int |
| `Data.QuantityAfter` | `QuantityAfter` | int |
| `Data.EventType` | `InventoryEventType` | string |
| `Timestamp` | `EventTimestamp` | timestamp |
| `EventType` | `EventType` | string |

##### 4.4.4 Save
Click **Add** or **Save**

---

### Step 5: Review and Publish Eventstream

#### 5.1 Review Configuration
Your Eventstream canvas should now show:
- **1 Source:** Azure Event Hubs (analytics-events)
- **4 Destinations:** OrderEvents, PaymentEvents, ProductEvents, InventoryEvents
- **Arrows/connections** showing data flow

#### 5.2 Validate
1. Click each destination to verify:
   - Correct table name
   - Filter is applied (EventType condition)
   - Schema mapping is correct

#### 5.3 Publish/Start
1. Click **Publish** or **Start** button in the top menu bar
2. Confirm any prompts
3. Wait for the status to change to **Running**
4. The status indicator should be green

---

## Alternative Simpler Approach

If the multi-destination setup is too complex initially, use this simpler single-table approach:

### Simple Setup: One Table for All Events

1. **Create just ONE Lakehouse destination**
2. **No filters** - let all events flow through
3. **Table name:** `AllAnalyticsEvents`
4. **Schema:** Auto-detect or map entire JSON
5. **Benefit:** Simpler setup, faster to get started
6. **Drawback:** Need to filter by EventType when querying

#### Query Examples with Single Table
```sql
-- Get order events
SELECT * FROM AllAnalyticsEvents 
WHERE EventType = 'OrderEvent' 
ORDER BY Timestamp DESC;

-- Get payment events
SELECT * FROM AllAnalyticsEvents 
WHERE EventType = 'PaymentEvent' 
ORDER BY Timestamp DESC;
```

You can always migrate to separate tables later using Spark notebooks or Delta table transformations.

---

## Step 6: Verify Events are Flowing

### 6.1 Start Your Application
```powershell
# From microservices root directory
.\start-all.ps1
# Or start Aspire AppHost from Visual Studio
```

### 6.2 Generate Test Events
1. Open the application UI (usually `https://localhost:7xxx`)
2. Place a test order:
   - Browse products
   - Add items to cart
   - Checkout
   - Complete payment
3. This will generate 4+ events:
   - OrderCreatedEvent
   - ProductReservedEvent (for each item)
   - PaymentProcessedEvent

### 6.3 Check Event Hub Metrics
1. Open [Azure Portal](https://portal.azure.com)
2. Navigate to your Event Hubs namespace: `evhns-product-ordering`
3. Click on Event Hub: `analytics-events`
4. Go to **Metrics** tab
5. Add metric: **Incoming Messages**
6. You should see messages appearing (may take 1-2 minutes)

### 6.4 Monitor Eventstream
1. In Fabric, open your Eventstream
2. Click the **Monitor** tab
3. Check the metrics:
   - **Source events:** Should show incoming events from Event Hub
   - **Destination events:** Should show events being written to Lakehouse
   - **Errors:** Should be 0 (if any, review error messages)

### 6.5 Query Lakehouse Tables
1. In Fabric workspace, open **ProductOrderingLakehouse**
2. Go to **SQL analytics endpoint** or **Open** → **Notebook**
3. Run queries to verify data:

```sql
-- Check OrderEvents
SELECT * FROM OrderEvents 
ORDER BY EventTimestamp DESC 
LIMIT 10;

-- Check PaymentEvents
SELECT * FROM PaymentEvents 
ORDER BY EventTimestamp DESC 
LIMIT 10;

-- Check ProductEvents
SELECT * FROM ProductEvents 
ORDER BY EventTimestamp DESC 
LIMIT 10;

-- Check InventoryEvents
SELECT * FROM InventoryEvents 
ORDER BY EventTimestamp DESC 
LIMIT 10;

-- Summary query
SELECT 
    EventType,
    COUNT(*) as EventCount,
    MAX(EventTimestamp) as LatestEvent
FROM (
    SELECT EventType, EventTimestamp FROM OrderEvents
    UNION ALL
    SELECT EventType, EventTimestamp FROM PaymentEvents
    UNION ALL
    SELECT EventType, EventTimestamp FROM ProductEvents
    UNION ALL
    SELECT EventType, EventTimestamp FROM InventoryEvents
)
GROUP BY EventType;
```

### 6.6 Check AnalyticsService Logs
In your Aspire dashboard or terminal, look for log messages:
```
Analytics: Capturing OrderCreated event for Order {OrderId}
Analytics: Stored OrderCreated event for Order {OrderId}
Published OrderEvent to Event Hub
```

---

## Troubleshooting
### "This operation is missing an input to work" Error

**This error occurs when adding a destination without properly connecting it to a source.**

**Symptoms:**
- Error appears when trying to configure or save a Lakehouse destination
- Destination node appears disconnected on canvas

**Solutions:**

1. **Ensure Event Hub Source is Added First:**
   - Step 3 must be completed before Step 4
   - The Event Hub source should be visible on the canvas
   - Source should show "Running" or "Configured" status

2. **Connect Source to Destination:**
   - Click on the **Event Hub source node**
   - Click the **+ (plus) icon** that appears on the source
   - Select **Add destination** → **Lakehouse**
   - This automatically creates the connection

3. **Alternative - Manual Connection:**
   - If destination is already added but disconnected
   - Hover over the Event Hub source output port (small circle on right side)
   - Click and drag to the destination input port (small circle on left side)
   - Release to create the connection line

4. **Verify Canvas Shows Connection:**
   - You should see a **line/arrow** from Event Hub source → Lakehouse destination
   - If no line exists, the destination won't receive data
   - Delete the destination and re-add it using the + button method

5. **Start Fresh if Needed:**
   - Delete the problematic destination
   - Click the Event Hub source node
   - Use **+ Add destination** from the source itself
   - This ensures proper connection from the start
### Events Not Appearing in Event Hub

**Symptoms:**
- Azure Portal metrics show 0 incoming messages
- No events in Eventstream monitor

**Solutions:**
1. **Check AnalyticsService logs** for errors
2. **Verify connection string:**
   ```powershell
   dotnet user-secrets list --project src/Services/AnalyticsService/ProductOrderingSystem.AnalyticsService.WebAPI
   ```
3. **Check Event Hub exists:**
   - Azure Portal → Event Hubs Namespace
   - Verify `analytics-events` Event Hub exists
4. **Verify permissions:**
   - Shared Access Key should have **Send** permission
5. **Test connection manually:**
   - Use Azure Portal **Process data** feature on Event Hub
6. **Check AnalyticsService is running:**
   - Verify in Aspire dashboard
   - Check it has green status

### Events Not Appearing in Fabric Eventstream

**Symptoms:**
- Event Hub has messages but Eventstream shows 0 events

**Solutions:**
1. **Verify Eventstream status is "Running"** (not paused/stopped)
2. **Check authentication:**
   - Review connection settings in Eventstream source
   - Verify Shared Access Key is correct
   - Test connection
3. **Review Monitor tab** for error messages
4. **Verify Event Hub name** matches exactly: `analytics-events`
5. **Check consumer group:**
   - If using custom consumer group, verify it exists in Event Hub
   - Try switching to `$Default` consumer group
6. **Restart Eventstream:**
   - Stop the Eventstream
   - Review configuration
   - Start again

### Events Not Appearing in Lakehouse Tables

**Symptoms:**
- Eventstream shows events flowing but tables are empty

**Solutions:**
1. **Check filters:**
   - Verify EventType filter conditions are correct
   - Check for typos: `OrderEvent` vs `orderEvent` (case-sensitive)
2. **Review schema mapping:**
   - Ensure field names match exactly: `Data.OrderId` not `OrderId`
   - Check data types are compatible
3. **Check write mode:** Should be `Append`
4. **Review destination logs** in Eventstream Monitor tab
5. **Verify tables exist:**
   - Open Lakehouse
   - Check Tables section shows: OrderEvents, PaymentEvents, etc.
6. **Check for schema errors:**
   - Eventstream Monitor may show "Schema mismatch" errors
   - Review and correct field mappings

### Schema Mapping Issues

**Solutions:**
1. **Use simpler single-table approach** initially
2. **Inspect actual event structure:**
   - Azure Portal → Event Hub → Process data
   - View sample events to see exact JSON structure
3. **Use auto-detect schema:**
   - In Lakehouse destination settings
   - Let Fabric auto-detect schema from first few events
4. **Test with raw JSON column:**
   - Add a column to store entire event as JSON string
   - Parse later with SQL or Spark

### Performance Issues

**Symptoms:**
- High latency between event creation and Lakehouse appearance
- Events backing up in Event Hub

**Solutions:**
1. **Check Fabric capacity:**
   - Ensure workspace has adequate capacity units
   - Monitor capacity metrics
2. **Review Event Hub throughput:**
   - May need to increase throughput units
   - Check for throttling in metrics
3. **Optimize Eventstream:**
   - Consider combining similar transformations
   - Reduce complex filtering logic
4. **Batch write settings:**
   - Adjust checkpoint intervals if available

---

## Next Steps: Power BI Dashboard

Once data is flowing to Lakehouse:

### 1. Connect Power BI to Lakehouse
- Open Power BI Desktop or Power BI Service
- **Get Data** → **OneLake data hub**
- Select **ProductOrderingLakehouse**
- Choose tables: OrderEvents, PaymentEvents, ProductEvents, InventoryEvents

### 2. Create Relationships
```
OrderEvents.OrderId → PaymentEvents.OrderId
OrderEvents.CustomerId → (Customer dimension if you add it)
InventoryEvents.ProductId → ProductEvents.ProductId
```

### 3. Create Measures
```DAX
Total Orders = COUNT(OrderEvents[OrderId])
Total Revenue = SUM(PaymentEvents[Amount])
Avg Order Value = DIVIDE([Total Revenue], [Total Orders])
Success Rate = DIVIDE(
    COUNTROWS(FILTER(PaymentEvents, PaymentEvents[Status] = "succeeded")),
    COUNTROWS(PaymentEvents)
)
```

### 4. Build Visualizations
- **Line chart:** Daily order volume trend
- **Card:** Total revenue, total orders, avg order value
- **Bar chart:** Top products by quantity reserved
- **Pie chart:** Payment success vs. failed
- **Table:** Recent orders with drill-through details

### 5. Enable Auto-Refresh
- Configure automatic refresh every 15-30 minutes
- Or use DirectQuery for real-time updates

---

## Production Recommendations

### Security
- ✅ **Use Azure Key Vault** for Event Hub connection string
- ✅ **Create dedicated consumer group** for Fabric: `fabric-consumer`
- ✅ **Use Managed Identity** instead of Shared Access Keys (if supported)
- ✅ **Limit permissions** - use Send-only policy for AnalyticsService

### Reliability
- ✅ **Enable dead-letter queue** on Event Hub
- ✅ **Set appropriate retention** - 7 days for Event Hub (Standard tier)
- ✅ **Monitor Eventstream health** - set up alerts
- ✅ **Checkpoint regularly** to avoid replay issues

### Performance
- ✅ **Use dedicated Fabric capacity** for production workloads
- ✅ **Configure partitioning** on Event Hub (by OrderId or EventType)
- ✅ **Optimize Delta tables** with Z-ORDER on commonly queried columns
- ✅ **Set up table maintenance** - OPTIMIZE and VACUUM on schedule

### Cost Optimization
- ✅ **Use Basic Event Hub tier** for development
- ✅ **Scale to Standard** only when needed
- ✅ **Configure Fabric auto-pause** when idle
- ✅ **Use Cool storage tier** for archived analytics data
- ✅ **Set appropriate retention** - don't store events longer than needed

### Monitoring
- ✅ **Azure Monitor alerts** for Event Hub metrics
- ✅ **Fabric capacity monitoring** for performance
- ✅ **Application Insights** for AnalyticsService
- ✅ **Power BI usage metrics** for dashboard adoption

---

## Reference: Event Schemas

### OrderEvent
```json
{
  "EventType": "OrderEvent",
  "Timestamp": "2025-12-16T10:30:00.123Z",
  "Data": {
    "OrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "CustomerId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "TotalAmount": 149.99,
    "Status": "Created",
    "ItemCount": 2
  }
}
```

### PaymentEvent
```json
{
  "EventType": "PaymentEvent",
  "Timestamp": "2025-12-16T10:30:05.456Z",
  "Data": {
    "PaymentId": "8b3d4e2f-1a2b-4c5d-6e7f-8a9b0c1d2e3f",
    "OrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "Amount": 149.99,
    "Status": "succeeded",
    "PaymentMethod": "USD"
  }
}
```

### ProductEvent
```json
{
  "EventType": "ProductEvent",
  "Timestamp": "2025-12-16T09:00:00.789Z",
  "Data": {
    "ProductId": "2d4e6f8a-1b3c-5d7e-9f0a-2b4c6d8e0f1a",
    "Name": "Wireless Headphones",
    "Category": "Electronics",
    "Price": 79.99,
    "EventType": "Created"
  }
}
```

### InventoryEvent
```json
{
  "EventType": "InventoryEvent",
  "Timestamp": "2025-12-16T10:30:03.321Z",
  "Data": {
    "ProductId": "2d4e6f8a-1b3c-5d7e-9f0a-2b4c6d8e0f1a",
    "OrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "QuantityChange": -2,
    "QuantityAfter": 0,
    "EventType": "Reserved"
  }
}
```

---

## Summary Checklist

### Initial Setup
- [ ] Microsoft Fabric workspace created
- [ ] ProductOrderingLakehouse exists
- [ ] Azure Event Hub `analytics-events` created
- [ ] Event Hub connection string configured in AnalyticsService
- [ ] AnalyticsService building and running

### Eventstream Configuration
- [ ] Eventstream created in Fabric
- [ ] Azure Event Hubs source added and connected
- [ ] 4 Lakehouse destinations configured (or 1 for simple approach)
- [ ] Filters applied for each EventType
- [ ] Schema mappings configured
- [ ] Eventstream published and status is "Running"

### Verification
- [ ] Test order placed in application
- [ ] Events visible in Event Hub metrics (Azure Portal)
- [ ] Events flowing in Eventstream monitor
- [ ] Data appearing in Lakehouse tables
- [ ] SQL queries return data

### Optional Power BI
- [ ] Power BI connected to Lakehouse
- [ ] Tables imported
- [ ] Relationships configured
- [ ] Basic dashboard created

---

## Support Resources

- **Microsoft Fabric Documentation:** https://learn.microsoft.com/fabric/
- **Eventstream Guide:** https://learn.microsoft.com/fabric/real-time-intelligence/event-streams/overview
- **Azure Event Hubs:** https://learn.microsoft.com/azure/event-hubs/
- **Power BI Lakehouse Integration:** https://learn.microsoft.com/power-bi/transform-model/dataflows/dataflows-azure-data-lake-storage-integration

For issues specific to this implementation, check:
- [Analytics-EventHub-Integration.md](./Analytics-EventHub-Integration.md)
- AnalyticsService logs in Aspire dashboard
- Azure Portal Event Hub metrics
