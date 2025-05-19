# Order Management Flow Documentation

## Entities Involved

### Core Entities

1. **Order**
   - Represents a customer's order
   - Contains metadata (OrderGuid, CustomerId, UserNameOrder, Status, etc.)
   - Has a collection of OrderItems

2. **OrderItem**
   - Represents a single product in an order
   - Contains ProductId, Quantity, UnitPrice
   - LineTotal calculated as Quantity * UnitPrice

3. **Product**
   - Represents items that can be ordered
   - Contains details like Name, Description, Price, Stock

4. **Customer**
   - The person placing an order
   - Referenced by CustomerId in Order entity

### Database Schema

```
Customers
  |-- CustomerId (PK)
  |-- Name
  |-- Email
  |-- Phone
  |-- Address
  `-- Timestamps

Products
  |-- ProductId (PK)
  |-- Name
  |-- Description
  |-- Category
  |-- Price
  |-- Stock
  |-- ProductGuid
  `-- Timestamps

Orders
  |-- OrderId (PK)
  |-- OrderGuid (Unique)
  |-- CustomerId (FK → Customers)
  |-- UserNameOrder
  |-- OrderDescription
  |-- OrderDate
  |-- Status
  |-- TotalAmount
  `-- Timestamps

OrderItems
  |-- OrderItemId (PK)
  |-- OrderId (FK → Orders)
  |-- ProductId (FK → Products)
  |-- Quantity
  |-- UnitPrice
  `-- Timestamps
```

## Order Creation Flow

### 1. Request Validation
- Endpoint: `POST /api/Order`
- Controller: `OrderController.CreateOrder()`
- Validates basic request data:
  - UserNameOrder is provided
  - CustomerId is valid
  - At least one item is included

### 2. Customer Verification
- Checks if customer exists using `_basicCrud.CheckIfValueExists()`
- Returns 404 if customer not found

### 3. Order Object Creation
- Creates new `Order` object with customer information
- Sets initial status to "Pending"
- Generates new OrderGuid (UUID)

### 4. Item Processing Loop
For each requested item:
- Retrieves product data from database using `_basicCrud.GetProductData()`
- Verifies product exists (404 if not)
- Checks stock availability (400 if insufficient)
- Adds item to order with `order.AddItem(productId, quantity, price)`
- Calculates running order total

### 5. Database Transaction
- Calls `_basicCrud.InsertOrder(order)` which:
  - Inserts record into Orders table
  - Gets generated OrderId
  - Inserts each item into OrderItems table
  - Updates product stock quantities
  - All operations are performed within implicit transaction

### 6. Response
- Returns 201 Created response
- Includes OrderGuid, CustomerId, TotalAmount, Status, ItemCount
- Location header points to GetOrder endpoint

## Order Retrieval Flow

### 1. Individual Order Retrieval
- Endpoint: `GET /api/Order/{orderGuid}`
- Controller: `OrderController.GetOrder()`
- Retrieves order by GUID using `_basicCrud.GetOrderFromGuid()`
- Returns 404 if order not found

### 2. Order Items Retrieval
- Gets order items using `_basicCrud.GetAllOrderItems(orderId)`
- Transforms DataTable to structured object
- Includes product names and calculated line totals

### 3. Response
- Returns complete order with items as a nested collection
- Includes all order metadata and full item details

### 4. Customer Orders Retrieval
- Endpoint: `GET /api/Order/customer/{customerId}`
- Controller: `OrderController.GetOrdersByCustomer()`
- Verifies customer exists with `_basicCrud.CustomerExists()`
- Gets all orders for customer using `_basicCrud.GetAllOrderForCustomer()`
- Returns list of orders (without items detail)

## Exception Handling

- All operations are wrapped in try-catch blocks
- Errors during order creation process:
  - Log detailed error information
  - Return appropriate HTTP status code (400, 404, 500)
  - Provide user-friendly error message
- Errors during order retrieval:
  - Log error details
  - Return 404 for not found items
  - Return 500 for server errors

## Database Operations

- Order insertion in single transaction to maintain data integrity
- Product stock updated atomically with order item creation
- Foreign key constraints ensure referential integrity
- All operations logged for audit and troubleshooting

This document provides a comprehensive overview of the order management process in the CRM system, from creation to retrieval, including all entities and database operations involved.
