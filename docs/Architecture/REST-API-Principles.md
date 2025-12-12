# REST API Principles Implementation

## Overview
This document describes the REST API principles implemented across all microservices in the Product Ordering System.

## Principles Applied

### 1. POST Endpoints - Resource Creation

**Principle**: POST endpoints that create resources return `201 Created` status code with a `Location` header pointing to the newly created resource, and an **empty response body**.

**Benefits**:
- Follows REST conventions
- Reduces response payload size
- Forces clients to use hypermedia (HATEOAS principle)
- Clear separation between resource creation and retrieval

**Implementation Pattern**:
```csharp
private static async Task<IResult> CreateResource(
    CreateResourceRequest request, 
    IMediator mediator, 
    HttpContext httpContext)
{
    var result = await mediator.Send(command);
    
    return result.Match(
        resource =>
        {
            // Follow REST principles: 201 Created with Location header, empty body
            var locationUri = $"/api/resources/{resource.Id}";
            httpContext.Response.Headers.Location = locationUri;
            return Results.StatusCode(201); // 201 Created with no body
        },
        errors => MapErrorsToResult(errors)
    );
}
```

**Client Usage Pattern**:
```powershell
# Create resource
$response = Invoke-WebRequest -Uri "$baseUrl/api/resources" -Method POST -Body $payload

# Extract ID from Location header
$location = $response.Headers["Location"]
$resourceId = $location -split '/' | Select-Object -Last 1

# Retrieve full resource details
$getResponse = Invoke-WebRequest -Uri "$baseUrl$location" -Method GET
$resource = $getResponse.Content | ConvertFrom-Json
```

### 2. Paginated GET Endpoints

**Principle**: GET endpoints that return paginated results return only the **data array in the response body**, with **pagination metadata in a `Pagination` response header** as JSON.

**Benefits**:
- Separates concerns: data vs metadata
- Cleaner response body (just the array)
- Easier to parse on client side
- Pagination metadata doesn't pollute the data structure
- Better for frontend frameworks that expect arrays

**Implementation Pattern**:
```csharp
private static async Task<IResult> GetResources(
    int page,
    int pageSize,
    IMediator mediator,
    HttpContext httpContext)
{
    var result = await mediator.Send(query);
    
    return result.Match(
        queryResult => 
        {
            // Calculate pagination metadata
            var paginationMetadata = new PaginationMetadata(
                Page: result.Page,
                PageSize: result.PageSize,
                TotalCount: result.TotalCount,
                TotalPages: (int)Math.Ceiling(result.TotalCount / (double)result.PageSize),
                HasPrevious: result.Page > 1,
                HasNext: result.Page < totalPages
            );

            // Add pagination metadata to response header as JSON
            httpContext.Response.Headers["Pagination"] = 
                System.Text.Json.JsonSerializer.Serialize(paginationMetadata);

            // Return only the data array in the body
            var items = result.Items.Select(MapToDto).ToList();
            return Results.Ok(items);
        },
        errors => MapErrorsToResult(errors)
    );
}
```

**Pagination Metadata Structure**:
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

**Client Usage Pattern**:
```powershell
# Request paginated data
$response = Invoke-WebRequest -Uri "$baseUrl/api/resources?page=1&pageSize=10" -Method GET

# Parse data array directly
$items = $response.Content | ConvertFrom-Json

# Parse pagination metadata from header
$paginationHeader = $response.Headers["Pagination"]
$pagination = $paginationHeader | ConvertFrom-Json

Write-Host "Page $($pagination.Page) of $($pagination.TotalPages)"
Write-Host "Total items: $($pagination.TotalCount)"
Write-Host "Has next page: $($pagination.HasNext)"
```

## Services Refactored

### Product Service ✅
- **POST /api/products** - Create product
  - Returns: 201 + Location header + empty body
  - Location format: `/api/products/{productId}`
  
- **GET /api/products** - Search products with pagination
  - Returns: 200 + product array in body + Pagination header
  - Query params: `?page=1&pageSize=10&searchTerm=...&category=...&minPrice=...&maxPrice=...`

### Order Service ✅
- **POST /api/orders** - Create order
  - Returns: 201 + Location header + empty body
  - Location format: `/api/orders/{orderId}`
  
- **GET /api/orders** - Get orders with pagination
  - Returns: 200 + order array in body + Pagination header
  - Query params: `?page=1&pageSize=10&customerId=...&status=...&startDate=...&endDate=...`

### Identity Service ✅
- **POST /api/auth/register** - Register new user
  - Returns: 201 + Location header + empty body
  - Location format: `/api/auth/users/{userId}`
  - **Note**: Login endpoint still returns user data + token in body (special case for authentication)

### Cart Service
- Cart service endpoints are currently **stateful** and return the updated cart state
- These do not follow the POST → 201 + Location pattern because:
  1. Cart operations are updates to existing state (not pure resource creation)
  2. Clients need immediate cart state after operations
  3. Cart is session-based, not a permanent resource with hypermedia links
- **Decision**: Keep current implementation for Cart Service

## Testing

### Test File
**`tests/Test-EndToEnd.ps1`** - Comprehensive end-to-end test suite including REST compliance
- **Tests 1-22**: Core functionality (authentication, products, cart, orders, event-driven integration)
- **Tests 23-27**: REST API compliance
  - Test 23: POST /api/products returns 201 + Location header + empty body
  - Test 24: POST /api/orders returns 201 + Location header + empty body
  - Test 25: GET /api/products returns array + Pagination header
  - Test 26: GET /api/orders returns array + Pagination header
  - Test 27: Resources are accessible via Location header

### Running Tests
```powershell
# Complete test suite (must have Aspire running)
cd c:\Repos\microservices
.\tests\Test-EndToEnd.ps1
```

Expected output: **27 tests** (22 core functionality + 5 REST compliance tests)

## Frontend Integration

### Benefits for Frontend Development
1. **Consistent patterns** across all services
2. **Cleaner data structures** - arrays are directly usable
3. **Separation of concerns** - data vs pagination metadata
4. **Type safety** - pagination metadata structure is consistent
5. **Reduced bundle size** - smaller response payloads
6. **Better caching** - Location headers enable proper HTTP caching strategies

### Example Frontend Usage (React/TypeScript)

```typescript
// Type definitions
interface PaginationMetadata {
  Page: number;
  PageSize: number;
  TotalCount: number;
  TotalPages: number;
  HasPrevious: boolean;
  HasNext: boolean;
}

interface Product {
  id: string;
  name: string;
  price: number;
  // ... other fields
}

// Create product
async function createProduct(productData: CreateProductRequest): Promise<string> {
  const response = await fetch('/api/products', {
    method: 'POST',
    headers: { 
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(productData)
  });
  
  if (response.status === 201) {
    const location = response.headers.get('Location');
    const productId = location?.split('/').pop();
    return productId!;
  }
  
  throw new Error('Failed to create product');
}

// Get paginated products
async function getProducts(
  page: number = 1, 
  pageSize: number = 10
): Promise<{ products: Product[], pagination: PaginationMetadata }> {
  const response = await fetch(
    `/api/products?page=${page}&pageSize=${pageSize}`
  );
  
  const products: Product[] = await response.json();
  
  const paginationHeader = response.headers.get('Pagination');
  const pagination: PaginationMetadata = JSON.parse(paginationHeader!);
  
  return { products, pagination };
}

// Usage in component
function ProductList() {
  const [products, setProducts] = useState<Product[]>([]);
  const [pagination, setPagination] = useState<PaginationMetadata | null>(null);
  
  useEffect(() => {
    getProducts(1, 10).then(result => {
      setProducts(result.products);
      setPagination(result.pagination);
    });
  }, []);
  
  return (
    <div>
      {products.map(product => (
        <ProductCard key={product.id} product={product} />
      ))}
      
      {pagination && (
        <Pagination
          currentPage={pagination.Page}
          totalPages={pagination.TotalPages}
          hasNext={pagination.HasNext}
          hasPrevious={pagination.HasPrevious}
        />
      )}
    </div>
  );
}
```

## OpenAPI Documentation Updates

All endpoints have been updated in their OpenAPI documentation:

```csharp
// POST endpoints
.WithSummary("Create a new resource (returns 201 with Location header)")
.Produces(201) // Changed from .Produces<ResourceDto>(201)

// Paginated GET endpoints
.WithSummary("Get resources with pagination (pagination metadata in Pagination header)")
.Produces<IEnumerable<ResourceDto>>(200) // Changed from .Produces<ResourceResponse>(200)
```

## Migration Notes

### Breaking Changes
⚠️ **This is a breaking change for existing clients!**

Clients must be updated to:
1. Parse `Location` header from POST responses instead of reading response body
2. Parse `Pagination` header from paginated GET responses
3. Make follow-up GET requests to retrieve created resources

### Backward Compatibility
If backward compatibility is required, consider:
1. API versioning (e.g., `/api/v2/products`)
2. Content negotiation headers
3. Query parameter flags (e.g., `?useLocationHeader=true`)

However, for a new system or during initial development, it's best to adopt REST principles from the start.

## References
- [REST API Design - Resource Creation](https://restfulapi.net/http-status-201-created/)
- [HATEOAS Principle](https://restfulapi.net/hateoas/)
- [HTTP Headers for Pagination](https://www.rfc-editor.org/rfc/rfc8288)
- [REST API Best Practices](https://stackoverflow.blog/2020/03/02/best-practices-for-rest-api-design/)
