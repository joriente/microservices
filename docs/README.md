# Product Ordering System Documentation

Welcome to the Product Ordering System documentation vault. This is a comprehensive microservices architecture demonstrating modern cloud-native patterns.

## 📚 Documentation Index

### Architecture & Design

- [[Architecture/Architecture-Diagrams|Architecture Diagrams]] - System architecture and component diagrams
- [[Architecture/Docker-Container-Organization|Docker Container Organization]] - Container orchestration and organization
- [[Architecture/REST-API-Principles|REST API Principles]] - RESTful API design principles

### Service Implementations

- [[Services/Analytics-Service-Implementation|Analytics Service]] - Analytics service architecture and implementation
- [[Services/CartService-RabbitMQ-Integration|Cart Service & RabbitMQ]] - Shopping cart service with message queue integration
- [[Services/ORDER_QUERIES_IMPLEMENTATION|Order Queries]] - Order query patterns and CQRS implementation
- [[Services/POLYGLOT_INTEGRATION|Polyglot Integration]] - Multi-language service integration

### Messaging & Events

- [[Messaging/MESSAGING_IMPLEMENTATION|Messaging Implementation]] - Message-based communication patterns
- [[Messaging/Event-Naming-Conventions|Event Naming Conventions]] - Event naming standards and conventions
- [[Messaging/SAGA_COMPENSATION_IMPLEMENTATION|Saga Compensation]] - Saga pattern and compensation transactions

### Configuration & Setup

- [[Configuration/API-KEYS-SETUP|API Keys Setup]] - API keys and secrets configuration
- [[Configuration/TESTING|Testing]] - Testing strategies and guidelines

### Planning & Presentations

- [[Planning/Microservices-Demo-Agenda|Demo Agenda]] - Demo walkthrough agenda
- [[Planning/Microservices-Demo-Invite|Demo Invite]] - Demo invitation and overview

## 🏗️ System Overview

The Product Ordering System is a complete microservices-based e-commerce platform featuring:

- **9 Core Services**: Product, Order, Cart, Payment, Inventory, Customer, Identity, Notification, Analytics
- **Polyglot Architecture**: .NET (C#), Java
- **Message Broker**: RabbitMQ with MassTransit
- **Databases**: PostgreSQL, MongoDB
- **API Gateway**: YARP reverse proxy
- **Orchestration**: .NET Aspire
- **Frontend**: Blazor WebAssembly

## 🔗 Quick Links

- [GitHub Repository](https://github.com/your-repo/microservices)
- [CI/CD Pipeline](.github/workflows/build-and-test.yml)
- [Docker Compose Configuration](../deployment/docker/docker-compose.yml)

## 📝 Contributing

When adding documentation:

1. Use wiki-style [[links]] to connect related topics
2. Follow the established naming conventions
3. Include code examples where applicable
4. Update this index when adding new documents

---

Last Updated: December 12, 2025
