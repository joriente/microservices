# Power BI Dashboard Setup for Analytics

This guide walks through connecting Power BI to the Microsoft Fabric Lakehouse and building an analytics dashboard using the Gold layer tables.

## Prerequisites

- Power BI Desktop installed (or use Power BI in Fabric workspace)
- Access to ProductOrderingLakehouse in Microsoft Fabric
- Gold layer tables created from gold-transformations notebook

## 1. Connect Power BI to Fabric Lakehouse

### Option A: Power BI Desktop

1. Open Power BI Desktop
2. Click **Get Data** → **More...**
3. Search for **Microsoft Fabric Lakehouse**
4. Select **Microsoft Fabric Lakehouse** and click **Connect**
5. Enter your Fabric workspace URL or select from list
6. Select **ProductOrderingLakehouse**
7. Authenticate with your Microsoft account

### Option B: Power BI in Fabric (Recommended)

1. Navigate to your Fabric workspace
2. Click **+ New** → **Power BI report**
3. Select **ProductOrderingLakehouse** as the data source
4. The Lakehouse tables will be available automatically

## 2. Import Gold Layer Tables

Import these tables from the Gold layer:

- ✅ **daily_orders** - Daily order trends
- ✅ **order_status** - Order status breakdown
- ✅ **product_performance** - Product metrics
- ✅ **category_performance** - Category aggregations
- ✅ **payment_metrics** - Payment success rates
- ✅ **daily_payments** - Daily payment trends
- ✅ **customer_metrics** - Customer lifetime value
- ✅ **inventory_summary** - Inventory movement

**In Power BI Desktop:**
1. In the Navigator window, expand **Files** → **analytics-datalake** → **analytics**
2. Check all 8 tables listed above
3. Click **Load**

**In Fabric Power BI:**
- Tables are automatically available in the Data pane

## 3. Create Data Model (if needed)

The Gold layer tables are pre-aggregated and can be used independently. However, you can create relationships if combining data:

```
daily_orders (order_date) → daily_payments (payment_date)
product_performance (ProductId) → inventory_summary (ProductId)
```

**To create relationships:**
1. Go to **Model** view
2. Drag fields between tables to create relationships
3. Set relationship type to **Many-to-One** where appropriate

## 4. Create Measures and Calculations

Add these DAX measures for enhanced analytics:

### Revenue Metrics

```dax
Total Revenue = SUM(daily_orders[total_revenue])

Revenue Growth % = 
VAR CurrentRevenue = SUM(daily_orders[total_revenue])
VAR PreviousRevenue = CALCULATE(SUM(daily_orders[total_revenue]), DATEADD(daily_orders[order_date], -1, DAY))
RETURN DIVIDE(CurrentRevenue - PreviousRevenue, PreviousRevenue, 0)

Average Order Value = AVERAGE(daily_orders[avg_order_value])
```

### Order Metrics

```dax
Total Orders = SUM(daily_orders[total_orders])

Order Completion Rate = 
VAR CompletedOrders = CALCULATE(SUM(order_status[order_count]), order_status[Status] = "Completed")
VAR TotalOrders = SUM(order_status[order_count])
RETURN DIVIDE(CompletedOrders, TotalOrders, 0)
```

### Payment Metrics

```dax
Payment Success Rate = 
VAR SuccessfulPayments = CALCULATE(SUM(payment_metrics[payment_count]), payment_metrics[Status] = "Completed")
VAR TotalPayments = SUM(payment_metrics[payment_count])
RETURN DIVIDE(SuccessfulPayments, TotalPayments, 0)

Total Payment Volume = SUM(payment_metrics[total_amount])
```

### Customer Metrics

```dax
Total Customers = COUNTROWS(customer_metrics)

Average Customer LTV = AVERAGE(customer_metrics[lifetime_value])

Top 10% Customer Value = 
CALCULATE(
    SUM(customer_metrics[lifetime_value]),
    TOPN(
        ROUNDUP(COUNTROWS(customer_metrics) * 0.1, 0),
        customer_metrics,
        customer_metrics[lifetime_value],
        DESC
    )
)
```

## 5. Build Dashboard Visualizations

### Page 1: Executive Overview

**Card Visuals (KPIs):**
- Total Revenue (from `daily_orders[total_revenue]`)
- Total Orders (from `daily_orders[total_orders]`)
- Average Order Value (from measure)
- Payment Success Rate (from measure)

**Line Chart: Revenue Trends**
- X-axis: `daily_orders[order_date]`
- Y-axis: `daily_orders[total_revenue]`
- Legend: None
- Title: "Daily Revenue Trend"

**Bar Chart: Order Status Breakdown**
- X-axis: `order_status[order_count]`
- Y-axis: `order_status[Status]`
- Title: "Orders by Status"

**Donut Chart: Payment Methods**
- Values: `payment_metrics[payment_count]`
- Legend: `payment_metrics[PaymentMethod]`
- Title: "Payment Methods Distribution"

### Page 2: Product Performance

**Table: Top Products**
- Columns: `product_performance[ProductName]`, `product_performance[Category]`, `product_performance[event_count]`, `product_performance[avg_price]`
- Sort by: `event_count` descending
- Title: "Top 20 Products"

**Bar Chart: Category Performance**
- X-axis: `category_performance[product_count]`
- Y-axis: `category_performance[Category]`
- Sort by: `product_count` descending
- Title: "Products by Category"

**Scatter Chart: Price vs Volume**
- X-axis: `product_performance[avg_price]`
- Y-axis: `product_performance[event_count]`
- Legend: `product_performance[Category]`
- Size: `product_performance[event_count]`
- Title: "Price vs Event Volume"

### Page 3: Customer Analytics

**Table: Top Customers**
- Columns: `customer_metrics[CustomerId]`, `customer_metrics[total_orders]`, `customer_metrics[lifetime_value]`, `customer_metrics[avg_order_value]`
- Sort by: `lifetime_value` descending
- Title: "Top 20 Customers by LTV"

**Column Chart: Customer Lifetime Value Distribution**
- X-axis: Create bins from `customer_metrics[lifetime_value]`
- Y-axis: Count of customers
- Title: "Customer LTV Distribution"

**Scatter Chart: Orders vs Lifetime Value**
- X-axis: `customer_metrics[total_orders]`
- Y-axis: `customer_metrics[lifetime_value]`
- Title: "Customer Order Frequency vs Value"

### Page 4: Inventory & Operations

**Table: Inventory Movement**
- Columns: `inventory_summary[ProductId]`, `inventory_summary[transaction_count]`, `inventory_summary[total_quantity_change]`, `inventory_summary[avg_quantity_after]`
- Sort by: `transaction_count` descending
- Title: "Top Inventory Movement"

**Gauge: Payment Success Rate**
- Value: Payment Success Rate measure
- Min: 0
- Max: 1
- Target: 0.95
- Title: "Payment Success Rate"

**Line Chart: Daily Payment Volume**
- X-axis: `daily_payments[payment_date]`
- Y-axis: `daily_payments[total_amount]`
- Legend: `daily_payments[Status]`
- Title: "Daily Payment Volume by Status"

## 6. Apply Formatting and Themes

1. **Apply Theme:**
   - View → Themes → Choose modern theme (e.g., "Executive", "Bold")

2. **Format Card Visuals:**
   - Data labels: Large font (36-48pt)
   - Category labels: Descriptive text
   - Background: Light colored cards
   - Add icons for visual appeal

3. **Format Charts:**
   - Titles: 14-16pt, bold
   - Axis labels: 10-12pt
   - Data labels: Show on hover
   - Colors: Use consistent color palette
   - Gridlines: Subtle or hidden

4. **Add Filters:**
   - Page-level filter: Date range slicer for `order_date`
   - Report-level filter: Category slicer for product analysis

## 7. Configure Auto-Refresh

### Power BI Service (Fabric)

1. Publish report to Fabric workspace
2. Go to **Settings** → **Datasets**
3. Under **Scheduled refresh**, click **Configure**
4. Set refresh frequency:
   - **Hourly** for real-time dashboards
   - **Daily** for overnight processing
5. Configure credentials for Lakehouse connection

### Incremental Refresh (Optional)

For large datasets, configure incremental refresh:

1. In Power BI Desktop, go to **Modeling** → **Manage Parameters**
2. Create `RangeStart` and `RangeEnd` parameters
3. Filter tables by date range using parameters
4. Configure incremental refresh policy:
   - Archive data: 1 year
   - Incremental refresh: Last 7 days
5. Publish to Power BI Service

## 8. Add Interactivity

**Cross-filtering:**
- Click on any visual to filter other visuals on the page
- Ctrl+Click to select multiple items

**Drill-through:**
1. Create detail page (e.g., Product Details)
2. Add drill-through field (e.g., `ProductId`)
3. Right-click on data point → Drill through

**Tooltips:**
1. Create tooltip page with detailed metrics
2. Format → Report page settings → Tooltip
3. Assign to visuals

## 9. Share and Collaborate

### Publish to Fabric Workspace

1. Click **File** → **Publish** → **Publish to Power BI**
2. Select workspace
3. Click **Open in Power BI**

### Share Report

1. In Power BI Service, open report
2. Click **Share** button
3. Enter email addresses
4. Set permissions (view/edit)
5. Send invitation

### Create App

1. In workspace, click **Create app**
2. Add reports to app
3. Configure navigation
4. Set permissions
5. Publish app

## 10. Monitor and Optimize

**Performance Best Practices:**

1. ✅ Use Gold layer tables (pre-aggregated)
2. ✅ Avoid importing Bronze/Silver layers into Power BI
3. ✅ Use DirectQuery for real-time data (optional)
4. ✅ Create aggregations for large tables
5. ✅ Use slicers and filters efficiently
6. ✅ Limit number of visuals per page (5-8 max)

**Monitor Usage:**

1. Go to workspace settings → **Usage metrics**
2. Track report views, users, and performance
3. Identify slow visuals and optimize

## Example Dashboard Layout

```
┌─────────────────────────────────────────────────────┐
│  Analytics Dashboard - Executive Overview          │
├──────────┬──────────┬──────────┬──────────────────┤
│  Total   │  Total   │   Avg    │    Payment      │
│ Revenue  │  Orders  │  Order   │  Success Rate   │
│  $45.2K  │   892    │  $50.67  │     94.5%       │
├──────────┴──────────┴──────────┴──────────────────┤
│                                                     │
│  Daily Revenue Trend                               │
│  [Line chart showing revenue over time]            │
│                                                     │
├─────────────────────────┬──────────────────────────┤
│  Orders by Status       │  Payment Methods         │
│  [Bar chart]            │  [Donut chart]           │
│                         │                          │
├─────────────────────────┴──────────────────────────┤
│  Filters: Date Range [=======●=========]           │
└─────────────────────────────────────────────────────┘
```

## Next Steps

1. ✅ Run gold-transformations notebook to create analytics tables
2. ✅ Connect Power BI to Fabric Lakehouse
3. ✅ Import Gold layer tables
4. ✅ Create measures and calculations
5. ✅ Build visualizations across multiple pages
6. ✅ Apply formatting and themes
7. ✅ Configure auto-refresh
8. ✅ Publish and share with stakeholders

## Troubleshooting

**Connection Issues:**
- Verify Lakehouse permissions in Fabric
- Ensure tables exist in `/analytics/` folder
- Check Azure credentials

**Data Not Refreshing:**
- Verify Eventstream is running
- Check silver/gold notebooks executed successfully
- Review refresh schedule in Power BI Service

**Performance Issues:**
- Use Gold layer tables only (not Bronze/Silver)
- Apply date range filters
- Consider DirectQuery mode for real-time data
- Use aggregations for large datasets

---

**Resources:**
- [Power BI Documentation](https://docs.microsoft.com/power-bi/)
- [Microsoft Fabric Analytics](https://docs.microsoft.com/fabric/)
- [DAX Function Reference](https://dax.guide/)
