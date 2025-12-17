# Microsoft Fabric Data Pipeline Orchestration

This guide shows how to orchestrate the analytics data pipeline in Microsoft Fabric to automate Bronze → Silver → Gold transformations.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    Analytics Data Pipeline                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  AnalyticsService (C#)                                          │
│         ↓                                                        │
│  Azure Event Hubs (analytics-events)                            │
│         ↓                                                        │
│  Fabric Eventstream (Real-time)                                 │
│         ↓                                                        │
│  Bronze Layer: OrderEvents Table                                │
│         ↓                                                        │
│  ┌──────────────────────────────────────┐                      │
│  │  Data Pipeline (Scheduled/Triggered)  │                      │
│  │  ┌────────────────────────────────┐  │                      │
│  │  │  Activity 1:                   │  │                      │
│  │  │  silver-transformations        │  │                      │
│  │  │  (Bronze → Silver)             │  │                      │
│  │  └─────────────┬──────────────────┘  │                      │
│  │                ↓ (On Success)         │                      │
│  │  ┌────────────────────────────────┐  │                      │
│  │  │  Activity 2:                   │  │                      │
│  │  │  gold-transformations          │  │                      │
│  │  │  (Silver → Gold)               │  │                      │
│  │  └─────────────┬──────────────────┘  │                      │
│  │                ↓ (Optional)           │                      │
│  │  ┌────────────────────────────────┐  │                      │
│  │  │  Activity 3:                   │  │                      │
│  │  │  Refresh Power BI Dataset      │  │                      │
│  │  └────────────────────────────────┘  │                      │
│  └──────────────────────────────────────┘                      │
│         ↓                                                        │
│  Power BI Dashboard (Real-time)                                 │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Pipeline Layers

| Layer | Storage | Update Method | Latency |
|-------|---------|---------------|---------|
| **Bronze** | OrderEvents table | Eventstream (real-time) | < 1 second |
| **Silver** | Curated tables (4) | Scheduled notebook | 15 min - 1 hour |
| **Gold** | Analytics tables (8) | Scheduled notebook | 15 min - 1 hour |
| **Power BI** | Dashboard visuals | Auto-refresh | 15 min - 1 hour |

## Prerequisites

- ✅ Bronze layer (OrderEvents table) receiving events
- ✅ silver-transformations notebook created and tested
- ✅ gold-transformations notebook created and tested
- ✅ ProductOrderingLakehouse configured
- ✅ Contributor or higher permissions in Fabric workspace

## Step 1: Create Data Pipeline

### 1.1 Navigate to Fabric Workspace

1. Open your Fabric workspace: **ProductOrderingAnalytics**
2. Click **+ New** button
3. Scroll down and select **Data pipeline**
4. Name: `Analytics-ETL-Pipeline`
5. Click **Create**

### 1.2 Configure Pipeline Canvas

You'll see a blank pipeline canvas with:
- **Activities** toolbar on the left
- **Pipeline canvas** in the center
- **Properties** pane on the right

## Step 2: Add Silver Transformation Activity

### 2.1 Add Notebook Activity

1. In the **Activities** toolbar, expand **Notebook**
2. Drag **Notebook** activity onto the canvas
3. Name the activity: `Silver-Transformations`

### 2.2 Configure Notebook Settings

1. Click on the **Silver-Transformations** activity
2. In the **Settings** tab (bottom pane):
   - **Notebook**: Select `silver-transformations`
   - **Lakehouse**: Select `ProductOrderingLakehouse`
   - **Spark pool**: Use default (Starter Pool)
   - **Executor size**: Small (4 cores, 28GB memory)

### 2.3 Configure Activity Settings

In the **General** tab:
- **Name**: `Silver-Transformations`
- **Description**: `Transform Bronze OrderEvents to Silver curated tables`
- **Timeout**: `01:00:00` (1 hour)
- **Retry**: `2` attempts
- **Retry interval**: `30` seconds

## Step 3: Add Gold Transformation Activity

### 3.1 Add Second Notebook Activity

1. Drag another **Notebook** activity onto the canvas
2. Name: `Gold-Transformations`
3. Position it to the right of Silver-Transformations

### 3.2 Create Dependency

1. Hover over the **Silver-Transformations** activity
2. Drag the **green checkmark arrow** (Success output) to **Gold-Transformations**
3. This ensures Gold runs only after Silver succeeds

### 3.3 Configure Notebook Settings

1. Click on **Gold-Transformations** activity
2. In the **Settings** tab:
   - **Notebook**: Select `gold-transformations`
   - **Lakehouse**: Select `ProductOrderingLakehouse`
   - **Spark pool**: Use default
   - **Executor size**: Small

### 3.4 Configure Activity Settings

In the **General** tab:
- **Name**: `Gold-Transformations`
- **Description**: `Transform Silver curated tables to Gold analytics tables`
- **Timeout**: `01:00:00`
- **Retry**: `2` attempts
- **Retry interval**: `30` seconds

## Step 4: Configure Pipeline Trigger

### 4.1 Schedule Trigger (Recommended)

1. Click **Home** tab in pipeline editor
2. Click **Add trigger** → **New trigger**
3. Configure trigger:
   - **Name**: `Scheduled-ETL-Trigger`
   - **Type**: **Schedule**
   - **Recurrence**: Choose one:

#### Option A: High-Frequency (Near Real-Time)
```
Recurrence: Every 15 minutes
Start: Today at 00:00
End: No end date
Time zone: (UTC-05:00) Eastern Time (US & Canada)
```
**Use for**: Real-time dashboards, active monitoring

#### Option B: Hourly Processing
```
Recurrence: Every 1 hour
Start: Today at 00:00
End: No end date
Time zone: (UTC-05:00) Eastern Time (US & Canada)
```
**Use for**: Standard BI reporting

#### Option C: Daily Batch Processing
```
Recurrence: Daily
Time: 02:00 AM
Days: All days
Time zone: (UTC-05:00) Eastern Time (US & Canada)
```
**Use for**: Overnight batch processing, lower costs

### 4.2 Event-Based Trigger (Advanced)

For event-driven execution when new data arrives:

1. Click **Add trigger** → **Event-based trigger**
2. Configure:
   - **Event source**: Lakehouse
   - **Lakehouse**: ProductOrderingLakehouse
   - **Table**: OrderEvents
   - **Event type**: Data changed
3. **Condition**: File count > 1000 rows (optional)

**Note**: May cause frequent executions; use throttling.

### 4.3 Manual Trigger (Testing)

For on-demand execution:

1. Don't configure automatic trigger
2. Run manually by clicking **Run** in pipeline editor
3. Useful for testing and troubleshooting

## Step 5: Add Optional Power BI Refresh

### 5.1 Add Refresh Dataset Activity (Optional)

If you want to auto-refresh Power BI after transformations:

1. Drag **Power BI** activity → **Refresh dataset** onto canvas
2. Connect Gold-Transformations → Refresh Dataset (success arrow)
3. Configure:
   - **Workspace**: Your Power BI workspace
   - **Dataset**: Your analytics dataset
   - **Refresh type**: Full

**Note**: Requires Power BI Premium or Fabric capacity.

## Step 6: Configure Pipeline Settings

### 6.1 Pipeline Parameters (Optional)

Add parameters for flexible execution:

1. Click **Parameters** tab in pipeline editor
2. Click **+ New**
3. Add parameters:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `refreshMode` | String | `full` | Full or incremental refresh |
| `dateFilter` | String | `today` | Date range for processing |
| `debugMode` | Boolean | `false` | Enable verbose logging |

### 6.2 Use Parameters in Notebooks

In notebook activities, pass parameters:

**Settings** → **Base parameters**:
```json
{
  "refresh_mode": "@pipeline().parameters.refreshMode",
  "date_filter": "@pipeline().parameters.dateFilter",
  "debug": "@pipeline().parameters.debugMode"
}
```

Update notebooks to accept parameters:
```python
# In notebook cell
dbutils.widgets.text("refresh_mode", "full")
dbutils.widgets.text("date_filter", "today")
dbutils.widgets.text("debug", "false")

refresh_mode = dbutils.widgets.get("refresh_mode")
```

## Step 7: Configure Error Handling

### 7.1 Add Failure Activities

1. Add **Web** activity for failure notifications
2. Connect Silver/Gold-Transformations **red X** (failure output) to Web activity
3. Configure:
   - **URL**: Teams webhook or email API endpoint
   - **Method**: POST
   - **Body**:
   ```json
   {
     "pipelineName": "@pipeline().Pipeline",
     "runId": "@pipeline().RunId",
     "error": "@activity('Silver-Transformations').Error.message",
     "timestamp": "@utcnow()"
   }
   ```

### 7.2 Add Logging

1. Add **Set variable** activity after each notebook
2. Create pipeline variable: `lastRunStatus`
3. Set value: `@activity('Silver-Transformations').Status`
4. Useful for debugging and monitoring

## Step 8: Test Pipeline

### 8.1 Validate Pipeline

1. Click **Validate** button (top toolbar)
2. Fix any validation errors
3. Ensure all activities have green checkmarks

### 8.2 Debug Run

1. Click **Debug** button
2. Monitor execution in **Output** tab
3. Check each activity:
   - **Queued** → **In Progress** → **Succeeded**
4. Review notebook outputs for errors

### 8.3 Verify Results

After successful run:

1. Navigate to **ProductOrderingLakehouse**
2. Check **Tables**:
   - ✅ orders, payments, products, inventory (Silver)
   - ✅ daily_orders, order_status, product_performance, category_performance, payment_metrics, daily_payments, inventory_summary, customer_metrics (Gold)
3. Query sample data:
   ```sql
   SELECT * FROM daily_orders ORDER BY order_date DESC LIMIT 10
   ```

## Step 9: Publish and Monitor

### 9.1 Publish Pipeline

1. Click **Publish** button (top toolbar)
2. Add comment: "Initial pipeline with Silver → Gold transformations"
3. Click **Publish**

### 9.2 Monitor Pipeline Runs

1. Navigate to **Monitoring Hub** in Fabric
2. Click **Pipeline runs**
3. Filter by pipeline name: `Analytics-ETL-Pipeline`
4. View run history:
   - **Status**: Succeeded, Failed, In Progress
   - **Duration**: Execution time
   - **Start time**: Trigger time

### 9.3 View Activity Details

1. Click on a pipeline run
2. View activity-level details:
   - Silver-Transformations: Duration, output
   - Gold-Transformations: Duration, output
3. Click on activity for detailed logs

## Step 10: Optimize Performance

### 10.1 Incremental Processing (Advanced)

Modify notebooks to process only new data:

**silver-transformations.py**:
```python
# Get last processed timestamp
last_run = spark.sql("""
    SELECT MAX(EventTimestamp) as max_ts 
    FROM orders
""").collect()[0]['max_ts']

# Filter bronze data
if last_run:
    bronze_df = bronze_df.filter(col("Timestamp") > last_run)
    print(f"Processing events after {last_run}")
else:
    print("Full refresh - processing all data")
```

### 10.2 Partitioning

Partition Gold tables by date for better query performance:

```python
# Partition by date
daily_orders.write \
    .format("delta") \
    .mode("overwrite") \
    .partitionBy("order_date") \
    .saveAsTable("daily_orders")
```

### 10.3 Optimize Spark Configuration

In notebook settings, add Spark configuration:

```
spark.sql.adaptive.enabled = true
spark.sql.adaptive.coalescePartitions.enabled = true
spark.sql.files.maxPartitionBytes = 134217728
```

## Pipeline Execution Patterns

### Pattern 1: Real-Time Analytics (15-min)

```
Trigger: Every 15 minutes
Silver: Incremental (last 15 min of data)
Gold: Incremental aggregations
Power BI: Auto-refresh every 15 min
Cost: High (frequent executions)
Use case: Live dashboards, monitoring
```

### Pattern 2: Hourly Processing (1-hour)

```
Trigger: Every hour
Silver: Incremental (last hour)
Gold: Incremental aggregations
Power BI: Auto-refresh hourly
Cost: Medium
Use case: Standard BI reporting
```

### Pattern 3: Daily Batch (Overnight)

```
Trigger: Daily at 2 AM
Silver: Full refresh
Gold: Full refresh
Power BI: Morning refresh
Cost: Low (single execution)
Use case: Daily reports, historical analysis
```

### Pattern 4: Event-Driven (On-Demand)

```
Trigger: When Bronze data changes
Silver: Process new events
Gold: Update affected aggregations
Power BI: Triggered refresh
Cost: Variable (based on event volume)
Use case: Unpredictable data arrival
```

## Monitoring and Alerts

### Configure Alerts

1. Go to **Monitoring Hub** → **Alerts**
2. Click **+ New alert rule**
3. Configure:
   - **Resource**: Analytics-ETL-Pipeline
   - **Condition**: Failed pipeline runs
   - **Action**: Send email/Teams notification
   - **Frequency**: Immediate

### Metrics to Monitor

| Metric | Threshold | Action |
|--------|-----------|--------|
| Pipeline success rate | < 95% | Investigate failures |
| Execution duration | > 30 min | Optimize notebooks |
| Silver table row count | 0 rows | Check Eventstream |
| Gold table freshness | > 2 hours | Check schedule |

## Troubleshooting

### Pipeline Not Running

**Check**:
- ✅ Trigger is published and active
- ✅ Workspace capacity is running (not paused)
- ✅ Permissions are correct (Contributor role)

### Notebook Failures

**Check**:
- ✅ Lakehouse is attached to notebooks
- ✅ Bronze table has data (`SELECT COUNT(*) FROM OrderEvents`)
- ✅ Spark pool has sufficient resources
- ✅ Review notebook error logs in pipeline output

### Performance Issues

**Optimize**:
- ✅ Use incremental processing (not full refresh)
- ✅ Partition large tables by date
- ✅ Increase Spark executor size if needed
- ✅ Add filters to reduce data volume

### Data Quality Issues

**Validate**:
- ✅ Check Bronze data quality (nulls, duplicates)
- ✅ Review transformation logic in notebooks
- ✅ Add data quality checks before writing

## Cost Optimization

### Reduce Costs

1. **Adjust frequency**: Use daily instead of hourly
2. **Use incremental**: Process only new data
3. **Right-size Spark**: Use smaller executors if possible
4. **Pause when idle**: Pause capacity during off-hours
5. **Monitor usage**: Review capacity metrics weekly

### Cost Comparison

| Frequency | Monthly Runs | Estimated CU Hours | Relative Cost |
|-----------|--------------|-------------------|---------------|
| 15 minutes | 2,880 | ~240 | High ($$$$) |
| 1 hour | 720 | ~60 | Medium ($$$) |
| Daily | 30 | ~2.5 | Low ($) |

**Note**: Costs vary by Fabric capacity tier (F2, F4, F8, etc.)

## Next Steps

1. ✅ Create and configure Analytics-ETL-Pipeline
2. ✅ Add Silver and Gold transformation activities
3. ✅ Configure schedule trigger (start with hourly)
4. ✅ Test with debug run
5. ✅ Publish pipeline
6. ✅ Monitor first few runs
7. ✅ Optimize based on performance metrics
8. ✅ Configure alerts and notifications
9. ✅ Connect Power BI to Gold tables
10. ✅ Set up Power BI auto-refresh

## Best Practices

✅ **Start with hourly** - Test before moving to higher frequency  
✅ **Use incremental processing** - Reduce cost and latency  
✅ **Add error handling** - Notifications and retries  
✅ **Monitor performance** - Track duration and success rate  
✅ **Version notebooks** - Use source control for changes  
✅ **Test in dev** - Don't test in production workspace  
✅ **Document changes** - Track pipeline modifications  
✅ **Set up alerts** - Know when failures occur  

## Resources

- [Fabric Data Pipelines Documentation](https://learn.microsoft.com/fabric/data-factory/pipeline-overview)
- [Notebook Activity Reference](https://learn.microsoft.com/fabric/data-factory/notebook-activity)
- [Pipeline Scheduling](https://learn.microsoft.com/fabric/data-factory/pipeline-triggers)
- [Monitoring and Alerts](https://learn.microsoft.com/fabric/data-factory/monitor-pipeline-runs)
