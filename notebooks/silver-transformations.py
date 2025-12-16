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

# # Silver Transformations for Analytics Events
# 
# This notebook demonstrates how to transform raw (bronze) analytics events from the Lakehouse into curated (silver) Delta tables for downstream analytics and reporting.

# CELL ********************

# 1. Import Required Libraries
from pyspark.sql import SparkSession
from pyspark.sql.functions import col, to_json, from_json
from pyspark.sql.types import *

spark = SparkSession.builder.getOrCreate()

# MARKDOWN ********************

# ## 2. Load Raw Data
# 
# Load the raw (bronze) events Delta table from the Lakehouse.

# CELL ********************

# 2. Load Raw Data from Bronze Layer (OrderEvents table)
bronze_df = spark.read.table("OrderEvents")

# Display schema and sample data
bronze_df.printSchema()
display(bronze_df.limit(10))

# MARKDOWN ********************

# MARKDOWN ********************

# ## 3. Define Event Schemas
# 
# Define schemas for parsing each event type from the Data column.

# CELL ********************

# 3. Define schemas for each event type

# OrderEvent schema
order_schema = StructType([
    StructField("OrderId", StringType(), True),
    StructField("CustomerId", StringType(), True),
    StructField("TotalAmount", DoubleType(), True),
    StructField("Status", StringType(), True),
    StructField("ItemCount", IntegerType(), True)
])

# PaymentEvent schema
payment_schema = StructType([
    StructField("PaymentId", StringType(), True),
    StructField("OrderId", StringType(), True),
    StructField("Amount", DoubleType(), True),
    StructField("Status", StringType(), True),
    StructField("PaymentMethod", StringType(), True)
])

# ProductEvent schema
product_schema = StructType([
    StructField("ProductId", StringType(), True),
    StructField("Name", StringType(), True),
    StructField("Category", StringType(), True),
    StructField("Price", DoubleType(), True),
    StructField("EventType", StringType(), True)
])

# InventoryEvent schema
inventory_schema = StructType([
    StructField("ProductId", StringType(), True),
    StructField("OrderId", StringType(), True),
    StructField("QuantityChange", IntegerType(), True),
    StructField("QuantityAfter", IntegerType(), True),
    StructField("EventType", StringType(), True)
])

# MARKDOWN ********************

# ## 3. Transform Events
# 
# Convert the Data struct to JSON string, then parse with appropriate schema for each event type.

# MARKDOWN ********************

# ## 4. Apply Transformations to Raw Data
# 
# Filter and transform each event type separately, flattening the JSON Data field.

# CELL ********************

# 4a. Transform OrderEvents
order_events_filtered = bronze_df.filter(col("EventType") == "OrderEvent")

if order_events_filtered.count() > 0:
    order_events = order_events_filtered \
        .withColumn("data_json", to_json(col("Data"))) \
        .withColumn("data_parsed", from_json(col("data_json"), order_schema)) \
        .select(
            col("Timestamp").alias("EventTimestamp"),
            col("EventProcessedUtcTime"),
            col("PartitionId"),
            col("EventEnqueuedUtcTime"),
            col("data_parsed.OrderId").alias("OrderId"),
            col("data_parsed.CustomerId").alias("CustomerId"),
            col("data_parsed.TotalAmount").alias("TotalAmount"),
            col("data_parsed.Status").alias("Status"),
            col("data_parsed.ItemCount").alias("ItemCount")
        )
    
    order_events_clean = order_events \
        .filter(col("OrderId").isNotNull()) \
        .dropDuplicates(["OrderId", "EventTimestamp"])
    
    print(f"Order Events Count: {order_events_clean.count()}")
    display(order_events_clean.limit(5))
else:
    print("No OrderEvent records found")
    order_events_clean = spark.createDataFrame([], 
        "EventTimestamp timestamp, EventProcessedUtcTime timestamp, PartitionId long, EventEnqueuedUtcTime timestamp, OrderId string, CustomerId string, TotalAmount double, Status string, ItemCount int")

# CELL ********************

# 4b. Transform PaymentEvents
payment_events_filtered = bronze_df.filter(col("EventType") == "PaymentEvent")

if payment_events_filtered.count() > 0:
    payment_events = payment_events_filtered \
        .withColumn("data_json", to_json(col("Data"))) \
        .withColumn("data_parsed", from_json(col("data_json"), payment_schema)) \
        .select(
            col("Timestamp").alias("EventTimestamp"),
            col("EventProcessedUtcTime"),
            col("PartitionId"),
            col("EventEnqueuedUtcTime"),
            col("data_parsed.PaymentId").alias("PaymentId"),
            col("data_parsed.OrderId").alias("OrderId"),
            col("data_parsed.Amount").alias("Amount"),
            col("data_parsed.Status").alias("Status"),
            col("data_parsed.PaymentMethod").alias("PaymentMethod")
        )
    
    payment_events_clean = payment_events \
        .filter(col("PaymentId").isNotNull()) \
        .dropDuplicates(["PaymentId", "EventTimestamp"])
    
    print(f"Payment Events Count: {payment_events_clean.count()}")
    display(payment_events_clean.limit(5))
else:
    print("No PaymentEvent records found")
    payment_events_clean = spark.createDataFrame([],
        "EventTimestamp timestamp, EventProcessedUtcTime timestamp, PartitionId long, EventEnqueuedUtcTime timestamp, PaymentId string, OrderId string, Amount double, Status string, PaymentMethod string")

# CELL ********************

# 4c. Transform ProductEvents
product_events = bronze_df.filter(col("EventType") == "ProductEvent") \
    .select(
        col("Timestamp").alias("EventTimestamp"),
        col("EventProcessedUtcTime"),
        col("PartitionId"),
        col("EventEnqueuedUtcTime"),
        col("Data.ProductId").alias("ProductId"),
        col("Data.Name").alias("ProductName"),
        col("Data.Category").alias("Category"),
        col("Data.Price").alias("Price"),
        col("Data.EventType").alias("ProductEventType")
    )

product_events_clean = product_events \
    .filter(col("ProductId").isNotNull()) \
    .dropDuplicates(["ProductId", "EventTimestamp"])

print(f"Product Events Count: {product_events_clean.count()}")
display(product_events_clean.limit(5))

# CELL ********************

# 4d. Transform InventoryEvents
inventory_events_filtered = bronze_df.filter(col("EventType") == "InventoryEvent")

if inventory_events_filtered.count() > 0:
    inventory_events = inventory_events_filtered \
        .withColumn("data_json", to_json(col("Data"))) \
        .withColumn("data_parsed", from_json(col("data_json"), inventory_schema)) \
        .select(
            col("Timestamp").alias("EventTimestamp"),
            col("EventProcessedUtcTime"),
            col("PartitionId"),
            col("EventEnqueuedUtcTime"),
            col("data_parsed.ProductId").alias("ProductId"),
            col("data_parsed.OrderId").alias("OrderId"),
            col("data_parsed.QuantityChange").alias("QuantityChange"),
            col("data_parsed.QuantityAfter").alias("QuantityAfter"),
            col("data_parsed.EventType").alias("InventoryEventType")
        )

    inventory_events_clean = inventory_events \
        .filter(col("ProductId").isNotNull()) \
        .dropDuplicates(["ProductId", "OrderId", "EventTimestamp"])

    print(f"Inventory Events Count: {inventory_events_clean.count()}")
    display(inventory_events_clean.limit(5))
else:
    print("No InventoryEvent records found")
    inventory_events_clean = spark.createDataFrame([],
        "EventTimestamp timestamp, EventProcessedUtcTime timestamp, PartitionId long, EventEnqueuedUtcTime timestamp, ProductId string, OrderId string, QuantityChange int, QuantityAfter int, InventoryEventType string")

# MARKDOWN ********************

# ## 5. Validate Transformed Data
# 
# Perform validation checks on the curated data.

# CELL ********************

# 5. Validate Transformed Data

# Validation summary
validation_results = {
    "OrderEvents": {
        "Total": order_events_clean.count(),
        "Null OrderIds": order_events_clean.filter(col("OrderId").isNull()).count(),
        "Null TotalAmount": order_events_clean.filter(col("TotalAmount").isNull()).count()
    },
    "PaymentEvents": {
        "Total": payment_events_clean.count(),
        "Null PaymentIds": payment_events_clean.filter(col("PaymentId").isNull()).count(),
        "Null Amount": payment_events_clean.filter(col("Amount").isNull()).count()
    },
    "ProductEvents": {
        "Total": product_events_clean.count(),
        "Null ProductIds": product_events_clean.filter(col("ProductId").isNull()).count(),
        "Null Price": product_events_clean.filter(col("Price").isNull()).count()
    },
    "InventoryEvents": {
        "Total": inventory_events_clean.count(),
        "Null ProductIds": inventory_events_clean.filter(col("ProductId").isNull()).count(),
        "Null QuantityAfter": inventory_events_clean.filter(col("QuantityAfter").isNull()).count()
    }
}

# Print validation results
for event_type, metrics in validation_results.items():
    print(f"\n{event_type}:")
    for metric, value in metrics.items():
        print(f"  {metric}: {value}")

# MARKDOWN ********************

# ## 6. Save Silver Data Output
# 
# Write the curated (silver) data to Delta tables in the `/curated/` folder.

# CELL ********************

# 6. Save Silver Data to Curated Layer

# Define output paths
curated_base_path = "Files/analytics-datalake/curated"

# Write OrderEvents to Delta
order_events_clean.write \
    .format("delta") \
    .mode("overwrite") \
    .option("overwriteSchema", "true") \
    .save(f"{curated_base_path}/orders")

print("✅ OrderEvents written to curated/orders")

# Write PaymentEvents to Delta
payment_events_clean.write \
    .format("delta") \
    .mode("overwrite") \
    .option("overwriteSchema", "true") \
    .save(f"{curated_base_path}/payments")

print("✅ PaymentEvents written to curated/payments")

# Write ProductEvents to Delta
product_events_clean.write \
    .format("delta") \
    .mode("overwrite") \
    .option("overwriteSchema", "true") \
    .save(f"{curated_base_path}/products")

print("✅ ProductEvents written to curated/products")

# Write InventoryEvents to Delta
inventory_events_clean.write \
    .format("delta") \
    .mode("overwrite") \
    .option("overwriteSchema", "true") \
    .save(f"{curated_base_path}/inventory")

print("✅ InventoryEvents written to curated/inventory")

# MARKDOWN ********************

# ## 7. Verify Silver Tables
# 
# Read back the Delta tables to verify they were created successfully.

# CELL ********************

# 7. Verify Silver Tables

# Read back and display counts
orders_silver = spark.read.format("delta").load(f"{curated_base_path}/orders")
payments_silver = spark.read.format("delta").load(f"{curated_base_path}/payments")
products_silver = spark.read.format("delta").load(f"{curated_base_path}/products")
inventory_silver = spark.read.format("delta").load(f"{curated_base_path}/inventory")

print("\n📊 Silver Layer Summary:")
print(f"  Orders: {orders_silver.count()} records")
print(f"  Payments: {payments_silver.count()} records")
print(f"  Products: {products_silver.count()} records")
print(f"  Inventory: {inventory_silver.count()} records")

print("\n✅ Silver transformations complete!")
