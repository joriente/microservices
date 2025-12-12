---
tags:
  - architecture
  - docker
  - containers
  - orchestration
  - deployment
  - infrastructure
---

# Docker Container Organization

## Changes Made

All Docker containers are now organized under the **ProductOrdering** project name for better visibility in Docker Desktop.

## Container Naming Convention

All containers now use the `ProductOrdering-` prefix:

### Aspire-Managed Containers
- `ProductOrdering-mongodb` - MongoDB database
- `ProductOrdering-postgres` - PostgreSQL database with pgAdmin
- `ProductOrdering-rabbitmq` - RabbitMQ message broker

### Docker Compose Containers (Legacy)
- `ProductOrdering-mongodb-legacy` - MongoDB from docker-compose
- `ProductOrdering-rabbitmq-legacy` - RabbitMQ from docker-compose

## Docker Desktop Organization

In Docker Desktop, all containers appear under the **ProductOrdering** group, making it easy to:
- See all related containers at a glance
- Start/stop the entire system
- Monitor resource usage per project
- Quickly identify which containers belong to this project

## Updated Files

1. **Start-all.ps1**
   - Uses `-p ProductOrdering` flag for docker-compose
   - Updated service URLs and port listings
   - Added all 7 microservices to manual mode instructions

2. **src/Aspire/ProductOrderingSystem.AppHost/Program.cs**
   - Added `.WithContainerName("ProductOrdering-{service}")` to all resources
   - Ensures consistent naming across all Aspire-managed containers

3. **deployment/docker/docker-compose.yml**
   - Updated container names to use ProductOrdering prefix
   - Added "-legacy" suffix to distinguish from Aspire containers

4. **Cleanup-Docker.ps1** (New)
   - Automated cleanup script
   - Removes all ProductOrdering containers
   - Optional volume removal for fresh start

## Cleanup Old Containers

To remove old containers and start fresh:

```powershell
.\Cleanup-Docker.ps1
```

To also remove volumes (deletes all data):

```powershell
.\Cleanup-Docker.ps1 -RemoveVolumes
```

## Viewing Containers

### Docker Desktop
All containers appear under the "ProductOrdering" group

### Command Line
```powershell
# List all ProductOrdering containers
docker ps -a --filter "name=ProductOrdering"

# List running ProductOrdering containers
docker ps --filter "name=ProductOrdering"

# View logs for a specific container
docker logs ProductOrdering-mongodb -f
```

## Service URLs (Quick Reference)

After running `.\Start-all.ps1`:

**Main Application:**
- 🌐 Web App: http://localhost:5261

**Management UIs:**
- 📊 Aspire Dashboard: http://localhost:15888
- 🐰 RabbitMQ: http://localhost:15672 (guest/guest)
- 🍃 Mongo Express: http://localhost:8081 (admin/admin123)
- 🐘 pgAdmin: Via Aspire dashboard → postgres resource

**Microservices:**
- 🚪 API Gateway: http://localhost:5000
- 🔐 Identity: http://localhost:5001
- 📦 Product: http://localhost:5002
- 🛒 Cart: http://localhost:5003
- 📋 Order: http://localhost:5004
- 💳 Payment: http://localhost:5005
- 👥 Customer: http://localhost:5006
- 📊 Inventory: http://localhost:5007

## Benefits

1. **Better Organization** - Easy to find all related containers
2. **Easier Management** - Stop/start entire project at once
3. **Clear Naming** - No confusion about which containers belong to which project
4. **Consistent Prefixes** - All containers follow same naming pattern
5. **Docker Desktop Integration** - Native project grouping support
