# Use Microsoft Fabric and Azure Data Lake for Analytics Architecture

- Status: accepted
- Deciders: Architecture Team, Data Engineering Team
- Date: 2025-12-12
- Tags: analytics, microsoft-fabric, data-lake, azure, business-intelligence

Technical Story: Implementation of AnalyticsService to provide real-time and historical business intelligence across the microservices platform.

## Context and Problem Statement

The Product Ordering System generates valuable business events across multiple microservices (orders, products, payments, inventory, customer behavior). We need a scalable analytics solution that can handle both real-time dashboards and complex historical analysis while supporting future machine learning capabilities. How should we architect our analytics platform to meet current needs and scale for advanced analytics scenarios?

## Decision Drivers

- Need for real-time analytics dashboards (sales metrics, product performance, cart abandonment)
- Requirement for historical trend analysis and reporting
- Support for future machine learning and predictive analytics
- Cost-effectiveness for long-term data storage
- Integration with existing event-driven architecture (RabbitMQ)
- Ability to handle increasing data volume as the business scales
- Support for both technical and business users (Power BI integration)
- Cloud-native solution that aligns with Azure ecosystem
- Minimize operational overhead for data pipeline management

## Considered Options

1. Microsoft Fabric + Azure Data Lake Storage Gen2
2. Azure Synapse Analytics + Azure Data Lake Storage Gen2
3. Custom analytics with MongoDB aggregations only
4. Elasticsearch + Kibana for analytics
5. Amazon Redshift + S3 (AWS alternative)

## Decision Outcome

Chosen option: "Microsoft Fabric + Azure Data Lake Storage Gen2", because it provides a unified analytics platform that seamlessly integrates real-time and batch processing, offers native Power BI integration, supports advanced machine learning scenarios through OneLake, and provides the best total cost of ownership for our hybrid hot/cold data architecture.

### Positive Consequences

- Unified analytics platform reduces complexity - single service for data warehousing, lakehouses, real-time analytics, and Power BI
- OneLake provides single storage layer automatically accessible by all Fabric workloads
- Native integration with Power BI for business users without additional licensing
- Real-time analytics hub enables streaming scenarios for live dashboards
- Built-in data governance and security across all analytics workloads
- Scalable architecture supports growth from real-time dashboards to advanced ML scenarios
- Parquet/Delta Lake format in Data Lake provides cost-effective long-term storage
- No need to manage separate data movement pipelines between systems
- Future-proof for AI/ML workloads with native Synapse Data Science integration

### Negative Consequences

- Microsoft Fabric is relatively new (GA in 2024), less mature than some alternatives
- Vendor lock-in to Microsoft Azure ecosystem
- Requires Azure subscription and expertise in Microsoft data platform
- Learning curve for team members unfamiliar with Fabric concepts
- Pricing model still evolving, need to monitor costs carefully

## Pros and Cons of the Options

### Microsoft Fabric + Azure Data Lake Storage Gen2

Unified analytics platform with integrated data warehouse, lakehouse, real-time analytics, and Power BI.

- Good, because OneLake provides single unified storage layer (eliminates data silos)
- Good, because native Power BI integration without additional licensing
- Good, because supports both real-time streaming and batch analytics in one platform
- Good, because built-in data governance and lineage tracking
- Good, because serverless compute reduces operational overhead
- Good, because designed for open data formats (Delta, Parquet)
- Good, because AI/ML integration with Synapse Data Science and Azure ML
- Bad, because relatively new service with evolving feature set
- Bad, because requires commitment to Microsoft ecosystem
- Bad, because capacity-based pricing can be complex to predict

### Azure Synapse Analytics + Azure Data Lake Storage Gen2

Purpose-built analytics service with separate data lake storage.

- Good, because proven enterprise-grade analytics platform
- Good, because strong SQL analytics capabilities
- Good, because integrates well with Power BI and Azure ML
- Good, because supports both serverless and dedicated SQL pools
- Bad, because requires separate Power BI licensing for full features
- Bad, because more complex to set up and manage than Fabric
- Bad, because requires manual orchestration between Synapse and other services
- Bad, because higher total cost for similar workloads compared to Fabric

### Custom analytics with MongoDB aggregations only

Use existing MongoDB with aggregation pipelines for all analytics.

- Good, because no additional infrastructure required
- Good, because team already familiar with MongoDB
- Good, because fast for simple real-time queries
- Bad, because MongoDB not designed for complex analytical queries at scale
- Bad, because no native BI tool integration (Power BI, Tableau)
- Bad, because expensive to store large historical datasets in MongoDB
- Bad, because limited support for advanced analytics and ML
- Bad, because aggregation pipelines become complex for sophisticated reports

### Elasticsearch + Kibana for analytics

Use Elasticsearch for analytics with Kibana for visualization.

- Good, because excellent for real-time search and analytics
- Good, because Kibana provides good visualization capabilities
- Good, because handles time-series data well
- Bad, because not designed for complex business intelligence queries
- Bad, because expensive for long-term data storage
- Bad, because requires separate infrastructure and expertise
- Bad, because limited Power BI integration compared to Fabric
- Bad, because no native support for traditional data warehouse patterns

### Amazon Redshift + S3 (AWS alternative)

AWS-native data warehouse and data lake solution.

- Good, because mature and proven at scale
- Good, because S3 provides cost-effective storage
- Good, because strong ecosystem of AWS analytics tools
- Bad, because requires migration to AWS or multi-cloud management
- Bad, because less native integration with existing Azure services
- Bad, because Power BI integration requires additional connectors
- Bad, because team lacks AWS expertise
- Bad, because introduces multi-cloud complexity and costs

## Implementation Architecture

### Data Flow
1. **Hot Path (Real-time)**: AnalyticsService consumes events from RabbitMQ → stores in MongoDB → powers real-time dashboards
2. **Cold Path (Batch)**: Hourly/daily jobs export aggregated data from MongoDB → Azure Data Lake (Parquet) → Microsoft Fabric processes → Data Warehouse/Lakehouse
3. **Serving Layer**: Fabric serves data to Power BI, APIs, and ML models

### Technology Stack
- **AnalyticsService**: .NET 9 microservice consuming RabbitMQ events
- **Hot Storage**: MongoDB for real-time metrics (last 30-90 days)
- **Cold Storage**: Azure Data Lake Storage Gen2 (Parquet format)
- **Analytics Platform**: Microsoft Fabric (Lakehouse, Data Warehouse, Real-time Hub)
- **Visualization**: Power BI embedded in admin dashboards

## Links

- [Analytics Service Implementation](../Services/Analytics-Service-Implementation.md)
- [Architecture Diagrams - Analytics Architecture](../Architecture/Architecture-Diagrams.md#analytics-architecture)
- [Microsoft Fabric Documentation](https://learn.microsoft.com/en-us/fabric/)
- [Azure Data Lake Storage Gen2](https://learn.microsoft.com/en-us/azure/storage/blobs/data-lake-storage-introduction)
