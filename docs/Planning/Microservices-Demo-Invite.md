---
tags:
  - planning
  - demo
  - presentation
  - overview
---
# Microservices Architecture Demo - Meeting Invite

**Duration:** 30-45 minutes  
**Format:** Live demonstration with Q&A

---

## Overview

Join us for a live demonstration of a production-ready microservices architecture built with .NET 10, showcasing event-driven communication, independent scaling, and modern cloud-native patterns.

## What You'll See

### Architecture Highlights
- **9 independent microservices** (Product, Order, Cart, Customer, Inventory, Payment, Identity, Analytics, Notification)
- **Polyglot microservices** (.NET + Java/Spring Boot NotificationService)
- **Event-driven communication** via RabbitMQ
- **Database per service** pattern (PostgreSQL, MongoDB)
- **Real-time analytics pipeline** (Event Hubs → Microsoft Fabric → Power BI)
- **Medallion architecture** (Bronze/Silver/Gold data layers)
- **API Gateway** with Yarp reverse proxy
- **Service orchestration** with .NET Aspire
- **Centralized observability** with Aspire Dashboard
- **Automated security scanning** with GitHub CodeQL

### Live Demonstrations

**0. Git Workflows & Security (10 min)**
- Repository structure and branching strategy
- GitHub CodeQL security scanning demonstration
- Pull request workflow with security checks
- Automated vulnerability detection and remediation

**1. Development Environment Setup (5 min)**
- VS Code extensions for microservices development
- C# Dev Kit, Azure tools, Docker integration
- Team consistency with extension list
- Productivity tools (GitHub Copilot, GitLens, Thunder Client)

**1.5. Documentation & Architectural Decision Records (3-5 min)**
- ADR (Architectural Decision Record) management with Obsidian
- Using Obsidian for interconnected markdown documentation
- Why we chose MongoDB for analytics vs PostgreSQL for inventory
- Microsoft Fabric analytics architecture decisions
- Documentation structure for team collaboration
- Markdown-based decision tracking with bidirectional linking

**2. Architecture Overview (15 min)**
- Solution structure walkthrough in VS Code
- 9 independent microservices with clean separation
- Domain-Driven Design with Clean Architecture
- Polyglot architecture (.NET + Java)
- Database per service rationale

**3. Live Service Orchestration (5-8 min)**
- Start entire system with one PowerShell command
- Aspire Dashboard for service health monitoring
- Running containers: PostgreSQL, MongoDB, RabbitMQ
- Service discovery and configuration management

**4. End-to-End User Flow (10-12 min)**
- Browse products → Add to cart → Place order
- Behind-the-scenes event chain explanation
- Real-time order status updates
- Payment processing with Stripe integration

**5. Analytics Service & Microsoft Fabric Integration (10 min)**
- Complete analytics pipeline: Microservices → Event Hubs → Fabric
- Dual-write pattern: PostgreSQL (operational) + Event Hubs (analytical)
- Medallion architecture demonstration:
  - **Bronze Layer**: Raw event ingestion from Event Hub
  - **Silver Layer**: Curated transformations with PySpark notebooks
  - **Gold Layer**: Pre-aggregated business metrics
- Power BI dashboard with real-time insights
- Data pipeline orchestration in Fabric

**6. Event-Driven Architecture Deep Dive (8-10 min)**
- RabbitMQ Management Console walkthrough
- Live event flow demonstration
- Create product → Watch inventory initialization
- Place order → Trace event chain across services
- Analytics service consuming events for dashboards

**7. Observability & Monitoring (5-7 min)**
- Centralized logging with Aspire Dashboard
- Structured logs and cross-service tracing
- Search by OrderId to see full journey
- Failure handling demonstration (insufficient inventory)

**8. Database Per Service Pattern (3-5 min)**
- PostgreSQL (InventoryService) - relational data with EF Core
- MongoDB (Multiple Services) - flexible schemas for carts, analytics, products, orders
- Database independence and API-only communication

### Key Concepts Covered
✅ Loose coupling and service independence  
✅ Event sourcing and eventual consistency  
✅ Fault tolerance and graceful degradation  
✅ Technology diversity (polyglot persistence & polyglot microservices)  
✅ Real-time analytics pipeline (Event Hubs → Fabric → Power BI)  
✅ Medallion architecture (Bronze/Silver/Gold data layers)  
✅ Dual-write pattern for operational and analytical workloads  
✅ GitHub security workflows with CodeQL scanning  
✅ Independent scaling and deployment  
✅ Distributed tracing and observability  
✅ Modern development tooling with VS Code extensions  
✅ Architectural decision documentation with ADRs  

## Technology Stack
- .NET 10 with C#
- Java 21 with Spring Boot 3.4 (NotificationService)
- .NET Aspire for orchestration and observability
- RabbitMQ (MassTransit for .NET, Spring AMQP for Java)
- PostgreSQL (InventoryService, AnalyticsService)
- MongoDB (ProductService, OrderService, CartService)
- **Azure Event Hubs** for cloud analytics streaming
- **Microsoft Fabric** with Medallion architecture (Bronze/Silver/Gold)
- **PySpark notebooks** for data transformations
- **Fabric Data Pipeline** for ETL orchestration
- **Power BI** for business intelligence dashboards
- Blazor WebAssembly frontend with MudBlazor
- Yarp API Gateway for reverse proxy
- Stripe payment integration
- SendGrid email integration (NotificationService)
- Docker containerization
- **GitHub CodeQL** for security scanning

## Who Should Attend
- Software architects and engineers
- DevOps/Platform engineers
- Technical leaders evaluating microservices
- Anyone interested in distributed systems

## What to Bring
- Questions about microservices patterns!
- Scenarios from your own projects
- Curiosity about event-driven architecture
- Interest in how we document architectural decisions

---

**Note:** Demo includes live coding walkthrough and failure scenario demonstrations. Time will be reserved for Q&A.

