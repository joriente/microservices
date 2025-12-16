# Fabric notebook source

# METADATA ********************

# META {
# META   "kernel_info": {
# META     "name": "synapse_pyspark"
# META   },
# META   "dependencies": {
# META     "lakehouse": {
# META       "default_lakehouse": "ProductOrderingLakehouse",
# META       "default_lakehouse_name": "ProductOrderingLakehouse",
# META       "default_lakehouse_workspace_id": ""
# META     }
# META   }
# META }

# MARKDOWN ********************

# # Gold Transformations for Analytics Events
# 
# This notebook creates aggregated analytics tables (gold layer) from the curated silver layer data.
# Gold tables are optimized for business intelligence and reporting.

# CELL ********************

# 1. Import Required Libraries
from pyspark.sql import SparkSession
from pyspark.sql.functions import col, count, sum, avg, min, max, round, date_trunc, current_timestamp
from pyspark.sql.window import Window

spark = SparkSession.builder.getOrCreate()

# MARKDOWN ********************

# ## 2. Load Silver Layer Data
# 
# Load the curated (silver) Delta tables from the Lakehouse.

# CELL ********************

# 2. Load Silver Layer Data
curated_base_path = "Files/analytics-datalake/curated"

orders_silver = spark.read.format("delta").load(f"{curated_base_path}/orders")
payments_silver = spark.read.format("delta").load(f"{curated_base_path}/payments")
products_silver = spark.read.format("delta").load(f"{curated_base_path}/products")
inventory_silver = spark.read.format("delta").load(f"{curated_base_path}/inventory")

print("\n📊 Silver Layer Data Loaded:")
print(f"  Orders: {orders_silver.count()} records")
print(f"  Payments: {payments_silver.count()} records")
print(f"  Products: {products_silver.count()} records")
print(f"  Inventory: {inventory_silver.count()} records")

# MARKDOWN ********************

# ## 3. Daily Order Summaries
# 
# Aggregate orders by day to track trends over time.

# CELL ********************

# 3. Daily Order Summaries
daily_orders = orders_silver \
    .withColumn("order_date", date_trunc("day", col("EventTimestamp"))) \
    .groupBy("order_date") \
    .agg(
        count("OrderId").alias("total_orders"),
        sum("TotalAmount").alias("total_revenue"),
        round(avg("TotalAmount"), 2).alias("avg_order_value"),
        sum("ItemCount").alias("total_items"),
        round(avg("ItemCount"), 2).alias("avg_items_per_order")
    ) \
    .orderBy("order_date")

print(f"\n📅 Daily Order Summaries: {daily_orders.count()} days")
display(daily_orders)

# MARKDOWN ********************

# ## 4. Order Status Summary
# 
# Count orders by status to track fulfillment pipeline.

# CELL ********************

# 4. Order Status Summary
order_status_summary = orders_silver \
    .groupBy("Status") \
    .agg(
        count("OrderId").alias("order_count"),
        sum("TotalAmount").alias("total_amount"),
        round(avg("TotalAmount"), 2).alias("avg_amount")
    ) \
    .orderBy(col("order_count").desc())

print(f"\n📊 Order Status Summary:")
display(order_status_summary)

# MARKDOWN ********************

# ## 5. Product Performance Metrics
# 
# Analyze product sales and pricing trends.

# CELL ********************

# 5. Product Performance Metrics
product_performance = products_silver \
    .groupBy("ProductId", "ProductName", "Category") \
    .agg(
        count("*").alias("event_count"),
        round(avg("Price"), 2).alias("avg_price"),
        min("Price").alias("min_price"),
        max("Price").alias("max_price")
    ) \
    .orderBy(col("event_count").desc())

print(f"\n🏆 Product Performance: {product_performance.count()} products")
display(product_performance.limit(20))

# MARKDOWN ********************

# ## 6. Category Performance
# 
# Aggregate performance by product category.

# CELL ********************

# 6. Category Performance
category_performance = products_silver \
    .groupBy("Category") \
    .agg(
        count("ProductId").alias("product_count"),
        round(avg("Price"), 2).alias("avg_price")
    ) \
    .orderBy(col("product_count").desc())

print(f"\n📂 Category Performance:")
display(category_performance)

# MARKDOWN ********************

# ## 7. Payment Metrics
# 
# Analyze payment success rates and methods.

# CELL ********************

# 7. Payment Metrics
payment_metrics = payments_silver \
    .groupBy("Status", "PaymentMethod") \
    .agg(
        count("PaymentId").alias("payment_count"),
        sum("Amount").alias("total_amount"),
        round(avg("Amount"), 2).alias("avg_amount")
    ) \
    .orderBy("Status", col("payment_count").desc())

print(f"\n💳 Payment Metrics:")
display(payment_metrics)

# MARKDOWN ********************

# ## 8. Daily Payment Summary
# 
# Track payment trends over time.

# CELL ********************

# 8. Daily Payment Summary
daily_payments = payments_silver \
    .withColumn("payment_date", date_trunc("day", col("EventTimestamp"))) \
    .groupBy("payment_date", "Status") \
    .agg(
        count("PaymentId").alias("payment_count"),
        sum("Amount").alias("total_amount")
    ) \
    .orderBy("payment_date", "Status")

print(f"\n📅 Daily Payment Summary:")
display(daily_payments)

# MARKDOWN ********************

# ## 9. Inventory Movement Summary
# 
# Analyze inventory changes and trends.

# CELL ********************

# 9. Inventory Movement Summary
inventory_summary = inventory_silver \
    .groupBy("ProductId") \
    .agg(
        count("*").alias("transaction_count"),
        sum("QuantityChange").alias("total_quantity_change"),
        round(avg("QuantityAfter"), 2).alias("avg_quantity_after"),
        min("QuantityAfter").alias("min_quantity"),
        max("QuantityAfter").alias("max_quantity")
    ) \
    .orderBy(col("transaction_count").desc())

print(f"\n📦 Inventory Movement Summary: {inventory_summary.count()} products")
display(inventory_summary.limit(20))

# MARKDOWN ********************

# ## 10. Customer Order Metrics
# 
# Aggregate order data by customer.

# CELL ********************

# 10. Customer Order Metrics
customer_metrics = orders_silver \
    .groupBy("CustomerId") \
    .agg(
        count("OrderId").alias("total_orders"),
        sum("TotalAmount").alias("lifetime_value"),
        round(avg("TotalAmount"), 2).alias("avg_order_value"),
        sum("ItemCount").alias("total_items_purchased")
    ) \
    .orderBy(col("lifetime_value").desc())

print(f"\n👥 Customer Metrics: {customer_metrics.count()} customers")
display(customer_metrics.limit(20))

# MARKDOWN ********************

# ## 11. Save Gold Layer Tables
# 
# Write aggregated data to the gold layer for BI consumption.

# CELL ********************

# 11. Save Gold Layer Tables as Lakehouse Managed Tables

# Write Daily Order Summaries
daily_orders.write \
    .format("delta") \
    .mode("overwrite") \
    .option("overwriteSchema", "true") \
    .saveAsTable("daily_orders")

print("✅ Daily orders written to daily_orders table")

# Write Order Status Summary
order_status_summary.write \
    .format("delta") \
    .mode("overwrite") \
    .option("overwriteSchema", "true") \
    .saveAsTable("order_status")

print("✅ Order status written to order_status table")

# Write Product Performance
product_performance.write \
    .format("delta") \
    .mode("overwrite") \
    .option("overwriteSchema", "true") \
    .saveAsTable("product_performance")

print("✅ Product performance written to product_performance table")

# Write Category Performance
category_performance.write \
    .format("delta") \
    .mode("overwrite") \
    .option("overwriteSchema", "true") \
    .saveAsTable("category_performance")

print("✅ Category performance written to category_performance table")

# Write Payment Metrics
payment_metrics.write \
    .format("delta") \
    .mode("overwrite") \
    .option("overwriteSchema", "true") \
    .saveAsTable("payment_metrics")

print("✅ Payment metrics written to payment_metrics table")

# Write Daily Payment Summary
daily_payments.write \
    .format("delta") \
    .mode("overwrite") \
    .option("overwriteSchema", "true") \
    .saveAsTable("daily_payments")

print("✅ Daily payments written to daily_payments table")

# Write Inventory Summary
inventory_summary.write \
    .format("delta") \
    .mode("overwrite") \
    .option("overwriteSchema", "true") \
    .saveAsTable("inventory_summary")

print("✅ Inventory summary written to inventory_summary table")

# Write Customer Metrics
customer_metrics.write \
    .format("delta") \
    .mode("overwrite") \
    .option("overwriteSchema", "true") \
    .saveAsTable("customer_metrics")

print("✅ Customer metrics written to customer_metrics table")

# MARKDOWN ********************

# ## 12. Verify Gold Tables
# 
# Read back the Delta tables to verify they were created successfully.

# CELL ********************

# 12. Verify Gold Tables

print("\n📊 Gold Layer Summary:")
print(f"  Daily Orders: {spark.table('daily_orders').count()} records")
print(f"  Order Status: {spark.table('order_status').count()} records")
print(f"  Product Performance: {spark.table('product_performance').count()} records")
print(f"  Category Performance: {spark.table('category_performance').count()} records")
print(f"  Payment Metrics: {spark.table('payment_metrics').count()} records")
print(f"  Daily Payments: {spark.table('daily_payments').count()} records")
print(f"  Inventory Summary: {spark.table('inventory_summary').count()} records")
print(f"  Customer Metrics: {spark.table('customer_metrics').count()} records")

print("\n✅ Gold transformations complete! Tables available in Lakehouse for Power BI.")
