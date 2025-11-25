# Test Suite Documentation

## Overview
This directory contains the comprehensive test suite for the Product Ordering System microservices.

## Test Files

### `Test-EndToEnd.ps1` - Complete Test Suite ✅
**Purpose**: Comprehensive end-to-end testing of all microservices, including REST API compliance verification.

**Total Tests**: 27

**Test Coverage**:

#### Part 1: Authentication Tests (Tests 1-4)
- Test 1: User Registration (REST: 201 + Location header)
- Test 2: User Login
- Test 3: Get Current User (Authenticated)
- Test 4: Get Current User (Unauthenticated - should fail)

#### Part 2: Product Tests (Tests 5-7)
- Test 5: Create Product (Authenticated, REST: 201 + Location header)
- Test 6: Create Product (Unauthenticated - should fail)
- Test 7: Create Second Product (REST: 201 + Location header)

#### Part 3: Cart Tests (Tests 8-16)
- Test 8: Get Empty Cart
- Test 9: Add First Item to Cart
- Test 10: Get Cart by ID
- Test 11: Get Cart by Customer ID
- Test 12: Add Same Product (Increase Quantity)
- Test 13: Add Second Product to Cart
- Test 14: Update Item Quantity
- Test 15: Remove Item from Cart
- Test 16: Verify Cart Total Calculation

#### Part 4: Order Tests (Tests 17-21)
- Test 17: Create Order (Authenticated, REST: 201 + Location header)
- Test 18: Create Order (Unauthenticated - should fail)
- Test 19: Verify Cart Auto-Cleared After Order (Event-Driven)
- Test 20: Manual Clear Cart (Fallback)
- Test 21: Cart Operations (Unauthenticated - should fail)

#### Part 5: Event-Driven Integration Tests (Test 22)
- Test 22: Product Cache Synchronization (ProductCreatedEvent)

#### Part 6: REST API Compliance Tests (Tests 23-27)
- Test 23: POST /api/products REST compliance (201 + Location + empty body)
- Test 24: POST /api/orders REST compliance (201 + Location + empty body)
- Test 25: GET /api/products pagination (array + Pagination header)
- Test 26: GET /api/orders pagination (array + Pagination header)
- Test 27: Resources accessible via Location header

## Running the Tests

### Prerequisites
1. **Docker Desktop** must be running (for RabbitMQ, SQL Server, Redis)
2. **Aspire AppHost** must be running:
   ```powershell
   cd c:\Repos\microservices
   dotnet run --project src/Aspire/ProductOrderingSystem.AppHost/ProductOrderingSystem.AppHost.csproj
   ```
3. Wait for all services to be healthy (check Aspire Dashboard at http://localhost:15888)

### Run Complete Test Suite
```powershell
cd c:\Repos\microservices
.\tests\Test-EndToEnd.ps1
```

### Expected Output
```
========================================
Product Ordering System - End-to-End Tests
========================================

PART 1: Authentication Tests
✓ Test 1 PASSED: User Registration
✓ Test 2 PASSED: User Login
✓ Test 3 PASSED: Get Current User (Authenticated)
✓ Test 4 PASSED: Get Current User (Unauthenticated)

PART 2: Product Tests
✓ Test 5 PASSED: Create Product (Authenticated)
✓ Test 6 PASSED: Create Product (Unauthenticated)
✓ Test 7 PASSED: Create Second Product

[... tests continue ...]

PART 6: REST API Compliance Tests
✓ Test 23 PASSED: POST /api/products REST Compliance
✓ Test 24 PASSED: POST /api/orders REST Compliance
✓ Test 25 PASSED: GET /api/products Pagination REST Compliance
✓ Test 26 PASSED: GET /api/orders Pagination REST Compliance
✓ Test 27 PASSED: Resources Accessible via Location Header

========================================
Test Summary
========================================
Total Tests: 27
Passed: 27
Failed: 0

🎉 ALL TESTS PASSED! 🎉
```

## Test Features

### REST API Compliance Testing
The test suite verifies that all endpoints follow REST principles:

**POST Endpoints:**
- Return `201 Created` status code
- Include `Location` header with URI to created resource
- Return empty response body
- Created resources are accessible via the Location URI

**Paginated GET Endpoints:**
- Return `200 OK` status code
- Return data array directly in response body
- Include `Pagination` header with JSON metadata:
  ```json
  {
    "Page": 1,
    "PageSize": 10,
    "TotalCount": 45,
    "TotalPages": 5,
    "HasPrevious": false,
    "HasNext": true
  }
  ```

### Event-Driven Integration Testing
Tests verify RabbitMQ event processing:
- `OrderCreatedEvent` → Cart Service clears customer's cart
- `ProductCreatedEvent` → Cart Service caches product data

### Authentication Testing
Tests verify JWT token-based authentication:
- Registration and login
- Protected endpoints require valid Bearer token
- Unauthorized requests return 401

## Troubleshooting

### Common Issues

**Issue**: `No connection could be made because the target machine actively refused it`
- **Solution**: Ensure Aspire is running and all services are healthy
- Check Aspire Dashboard: http://localhost:15888

**Issue**: Test 19 fails (Cart Auto-Clear After Order)
- **Solution**: RabbitMQ connection timing issue
- Restart Aspire and wait 10-15 seconds before running tests

**Issue**: REST compliance tests fail
- **Solution**: Check that you're using the updated API endpoints
- Verify OpenAPI documentation at: http://localhost:5555/swagger

**Issue**: Pagination tests fail
- **Solution**: Ensure `Pagination` header is being set by endpoints
- Check response headers in browser DevTools or Postman

## Test Data

### Generated Data
Each test run creates unique test data:
- **User**: `testuser_{timestamp}@test.com`
- **Products**: `Test Product {timestamp}`
- **Orders**: Unique order per test run
- **Cart**: One cart per test user

### Data Cleanup
Test data persists in the databases. To clean up:
```powershell
# Stop Aspire (Ctrl+C)
# Restart Aspire (databases will be recreated)
dotnet run --project src/Aspire/ProductOrderingSystem.AppHost/ProductOrderingSystem.AppHost.csproj
```

## CI/CD Integration

This test suite can be integrated into CI/CD pipelines:

```yaml
# Example GitHub Actions workflow
- name: Run Tests
  run: |
    dotnet run --project src/Aspire/ProductOrderingSystem.AppHost/ProductOrderingSystem.AppHost.csproj &
    sleep 30  # Wait for services to start
    pwsh ./tests/Test-EndToEnd.ps1
  env:
    ASPIRE_ALLOW_UNSECURED_TRANSPORT: true
```

## Documentation

For more information about REST API principles:
- See `docs/REST-API-Principles.md` for detailed REST implementation documentation
- See `docs/REST-Implementation-Summary.md` for quick reference

## Contributing

When adding new endpoints or features:
1. Add corresponding tests to `Test-EndToEnd.ps1`
2. Ensure REST compliance for POST and paginated GET endpoints
3. Update this README if adding new test sections
4. Run full test suite to verify no regressions
