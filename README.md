# CRM Backend - ASP.NET Core API

## 📋 Table of Contents
- [Overview](#overview)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Database Setup](#database-setup)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [API Documentation](#api-documentation)
- [Authentication & Authorization](#authentication--authorization)
- [Testing](#testing)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)
- [Development Guidelines](#development-guidelines)

## 🎯 Overview

The CRM Backend is a RESTful API service built with ASP.NET Core 8.0 that provides comprehensive customer relationship management functionality. It features JWT-based authentication, role-based authorization, and a robust data access layer using ADO.NET with SQL Server.

### Key Features
- **User Management**: Registration, authentication, and role-based access control
- **Product Catalog**: Complete CRUD operations with category management
- **Shopping Cart**: Session-based cart management with persistence
- **Order Processing**: Order creation, tracking, and status management
- **Admin Functions**: User promotion, system configuration, and data management
- **Security**: JWT tokens, password hashing, and SQL injection prevention

## 🏗️ Architecture

### High-Level Architecture
```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   React Client  │────▶│  ASP.NET Core   │────▶│   SQL Server    │
│   (Frontend)    │◀────│      API        │◀────│   Database      │
└─────────────────┘     └─────────────────┘     └─────────────────┘
        │                        │                        │
        │      JWT Tokens       │    ADO.NET            │
        │      REST/JSON        │    Stored Procs       │
        └───────────────────────┴────────────────────────┘
```

### Layer Architecture
```
CRM/
├── Controllers/          # API endpoints (Presentation Layer)
├── Services/            # Business logic (Service Layer)
├── Infra/              # Infrastructure concerns
│   ├── Database/       # Data access layer
│   │   ├── Operations/ # CRUD operations
│   │   └── Context/    # Database context management
│   └── Common/         # Shared utilities
├── Models/             # Domain models and DTOs
├── Security/           # Authentication & authorization
└── Middleware/         # Cross-cutting concerns
```

### Design Patterns Used
- **Repository Pattern**: Abstracted data access through `BasicCrud` class
- **Service Layer Pattern**: Business logic separated from controllers
- **Dependency Injection**: IoC container for service management
- **Factory Pattern**: Database connection creation
- **Singleton Pattern**: Configuration and logging services

## 💻 Technology Stack

- **Framework**: ASP.NET Core 8.0
- **Language**: C# 12
- **Database**: SQL Server 2022
- **Authentication**: JWT Bearer Tokens
- **API Documentation**: Swagger/OpenAPI
- **Logging**: Custom file-based logging
- **Testing**: xUnit, Moq
- **Containerization**: Docker (for SQL Server)

## 📁 Project Structure

```
CRM/
├── Controllers/
│   ├── AuthController.cs         # Authentication endpoints
│   ├── UserController.cs         # User management
│   ├── ProductController.cs      # Product operations
│   ├── CartController.cs         # Shopping cart
│   ├── OrderController.cs        # Order processing
│   └── SetupController.cs        # Admin setup
├── Services/
│   ├── AuthService.cs           # Authentication logic
│   ├── UserService.cs           # User business logic
│   ├── ProductService.cs        # Product business logic
│   └── OrderService.cs          # Order processing logic
├── Infra/
│   ├── Database/
│   │   ├── Operations/
│   │   │   └── BasicCrud.cs     # Core CRUD operations
│   │   ├── Context/
│   │   │   └── DatabaseContext.cs # Connection management
│   │   └── SQL/
│   │       ├── Tables/          # Table creation scripts
│   │       └── Procedures/      # Stored procedures
│   └── Common/
│       ├── Logger.cs            # Logging implementation
│       └── Extensions.cs        # Helper extensions
├── Models/
│   ├── User.cs                  # User entity
│   ├── Product.cs               # Product entity
│   ├── Cart.cs                  # Cart entities
│   ├── Order.cs                 # Order entities
│   └── DTOs/                    # Data transfer objects
├── Security/
│   ├── JwtService.cs            # JWT token generation
│   ├── PasswordHasher.cs        # BCrypt hashing
│   └── AuthorizeAdmin.cs        # Admin authorization
├── Middleware/
│   ├── ErrorHandling.cs         # Global error handler
│   └── RequestLogging.cs        # Request/response logging
├── Program.cs                   # Application entry point
├── appsettings.json            # Configuration
└── CRM.csproj                  # Project file
```

## 📋 Prerequisites

### Required Software
- .NET SDK 8.0 or higher
- Docker Desktop or Docker Engine
- SQL Server Management Studio (optional)
- Visual Studio 2022 or VS Code
- Git

### System Requirements
- **OS**: Windows 10/11, macOS, or Linux (Ubuntu 20.04+, Arch)
- **RAM**: Minimum 4GB (8GB recommended)
- **Storage**: 2GB for Docker images + database

## 🚀 Installation

### 1. Clone the Repository
```bash
git clone <repository-url>
cd dev-academy-CRM
```

### 2. Install .NET Dependencies
```bash
cd CRM
dotnet restore
```

### 3. Check Prerequisites
```bash
# Verify .NET installation
dotnet --version

# Verify Docker installation
docker --version
```

## 🗄️ Database Setup

### Option 1: Automated Setup (Recommended)
```bash
cd scripts
sudo ./db_setup.sh
```

This script will:
1. Check for Docker installation
2. Pull SQL Server 2022 image
3. Create persistent volume
4. Start SQL Server container
5. Wait for SQL Server to be ready
6. Create CRM database
7. Run all table creation scripts
8. Create stored procedures
9. Seed initial admin user

### Option 2: Manual Setup

#### Step 1: Start SQL Server Container
```bash
# Create Docker volume for data persistence
sudo docker volume create mssql-data

# Run SQL Server container
sudo docker run -d \
  --name crm_sql_server \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd123!" \
  -e "MSSQL_PID=Express" \
  -p 1433:1433 \
  -v mssql-data:/var/opt/mssql \
  mcr.microsoft.com/mssql/server:2022-latest
```

#### Step 2: Create Database
```bash
# Connect to SQL Server
docker exec -it crm_sql_server /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U SA -P "YourStrong@Passw0rd123!"

# Create database
CREATE DATABASE CRM;
GO
USE CRM;
GO
```

#### Step 3: Run SQL Scripts
Execute scripts in order:
1. `scripts/sql/01_create_tables.sql`
2. `scripts/sql/02_create_indexes.sql`
3. `scripts/sql/03_create_procedures.sql`
4. `scripts/sql/04_seed_data.sql`

### Database Schema

#### Users Table
```sql
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(50) DEFAULT 'Regular',
    CreatedDate DATETIME DEFAULT GETDATE(),
    LastLoginDate DATETIME,
    IsActive BIT DEFAULT 1
);
```

#### Products Table
```sql
CREATE TABLE Products (
    ProductId INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    Category NVARCHAR(100),
    Price DECIMAL(10,2) NOT NULL,
    Stock INT DEFAULT 0,
    ImageUrl NVARCHAR(500),
    CreatedDate DATETIME DEFAULT GETDATE(),
    UpdatedDate DATETIME,
    IsActive BIT DEFAULT 1
);
```

#### Orders Table
```sql
CREATE TABLE Orders (
    OrderId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT FOREIGN KEY REFERENCES Users(Id),
    OrderDate DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(50) DEFAULT 'Pending',
    TotalAmount DECIMAL(10,2),
    ShippingAddress NVARCHAR(500),
    PaymentMethod NVARCHAR(50),
    TrackingNumber NVARCHAR(100)
);
```

## ⚙️ Configuration

### appsettings.json
```json
{
  "ConnectionString": {
    "SqlServer": "Server=localhost,1433;Database=CRM;User Id=SA;Password=YourStrong@Passw0rd123!;TrustServerCertificate=True",
    "DatabaseName": "CRM"
  },
  "Jwt": {
    "SecretKey": "your-very-long-secret-key-at-least-32-characters",
    "Issuer": "CRM-API",
    "Audience": "CRM-Client",
    "ExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "FilePath": "Logs/CRM_Logs.txt"
  },
  "CORS": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:3001"]
  }
}
```

### Environment Variables (Optional)
```bash
export CRM_DB_PASSWORD="YourStrong@Passw0rd123!"
export CRM_JWT_SECRET="your-very-long-secret-key"
export ASPNETCORE_ENVIRONMENT="Development"
```

## 🏃 Running the Application

### Development Mode
```bash
cd CRM
dotnet run
```

### Watch Mode (Auto-reload)
```bash
dotnet watch run
```

### Production Mode
```bash
dotnet run --configuration Release
```

### Access Points
- API Base URL: `http://localhost:5205`
- Swagger UI: `http://localhost:5205/swagger`
- Health Check: `http://localhost:5205/health`

## 📡 API Documentation

### Authentication

#### Register New User
```http
POST /api/User/register
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "role": "Regular"
}

Response: 201 Created
{
  "message": "User registered successfully",
  "userId": 123
}
```

#### Login
```http
POST /api/Auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "SecurePass123!"
}

Response: 200 OK
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": 123,
    "name": "John Doe",
    "email": "john@example.com",
    "role": "Regular"
  }
}
```

### Products

#### Get All Products
```http
GET /api/product
Authorization: Bearer {token}

Response: 200 OK
[
  {
    "productId": 1,
    "name": "Product Name",
    "description": "Description",
    "category": "Electronics",
    "price": 99.99,
    "stock": 50
  }
]
```

#### Add Product (Admin Only)
```http
POST /api/product/add
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "New Product",
  "description": "Product description",
  "category": "Electronics",
  "price": 149.99,
  "stock": 25
}
```

### Cart Operations

#### Add to Cart
```http
POST /api/cart/add
Authorization: Bearer {token}
Content-Type: application/json

{
  "productId": 1,
  "quantity": 2
}
```

#### Get Cart
```http
GET /api/cart
Authorization: Bearer {token}
```

### Orders

#### Create Order
```http
POST /api/order/create
Authorization: Bearer {token}
Content-Type: application/json

{
  "shippingAddress": "123 Main St, City, Country",
  "paymentMethod": "CreditCard"
}
```

## 🔐 Authentication & Authorization

### JWT Token Structure
```json
{
  "sub": "user@example.com",
  "jti": "unique-token-id",
  "userId": "123",
  "role": "Admin",
  "exp": 1234567890,
  "iss": "CRM-API",
  "aud": "CRM-Client"
}
```

### Authorization Attributes
```csharp
[Authorize]                    // Requires authentication
[Authorize(Roles = "Admin")]   // Requires Admin role
[AllowAnonymous]              // No authentication required
```

### Security Features
- BCrypt password hashing with salt
- JWT token expiration
- Role-based access control
- SQL injection prevention via parameterized queries
- CORS policy enforcement
- Request rate limiting
- Input validation and sanitization

## 🧪 Testing

### Run All Tests
```bash
cd CRM.Tests
dotnet test
```

### Run with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Integration Tests
The folder scripts/tests will contains multiple bash scripts to thes app integrations. 
```bash
# Example: 
cd scripts/tests
./test_all_controllers.sh
```
The above will test all the controllers that are accessible by regular users

---

```bash
# Example: 
cd scripts/tests
./test_all_controllers.sh
```
The above will test all routes that are protected and only accessible by admins

### Test Categories
- **Unit Tests**: Service layer logic
- **Integration Tests**: Database operations
- **API Tests**: Controller endpoints
- **Security Tests**: Authentication flows

## 📦 Deployment

### Build for Production
```bash
dotnet publish -c Release -o ./publish
```

### Docker Deployment
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY ./publish .
ENTRYPOINT ["dotnet", "CRM.dll"]
```

### Environment-Specific Settings
1. Update connection strings
2. Configure JWT secrets
3. Set CORS origins
4. Enable HTTPS
5. Configure logging levels

## 🔧 Troubleshooting

### Common Issues

#### 1. Database Connection Failed
**Error**: "A network-related or instance-specific error occurred"

**Solutions**:
```bash
# Check if SQL Server container is running
docker ps | grep crm_sql_server

# Check container logs
docker logs crm_sql_server

# Test connection
docker exec -it crm_sql_server /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U SA -P "YourStrong@Passw0rd123!" -Q "SELECT 1"

# Restart container if needed
docker restart crm_sql_server
```

#### 2. JWT Token Invalid
**Error**: "Bearer error='invalid_token'"

**Solutions**:
- Verify secret key matches in configuration
- Check token expiration time
- Ensure clock sync between client and server
- Validate token format in jwt.io

#### 3. CORS Issues
**Error**: "Access to XMLHttpRequest blocked by CORS policy"

**Solutions**:
```csharp
// In Program.cs, update CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder
            .WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});
```

#### 4. Port Already in Use
**Error**: "Address already in use"

**Solutions**:
```bash
# Find process using port
sudo lsof -i :5205

# Kill process
kill -9 <PID>

# Or change port in launchSettings.json
```

#### 5. Entity Framework Migration Issues
**Note**: This project uses ADO.NET, not EF Core

If considering migration to EF:
- Review current stored procedures
- Map existing database schema
- Create EF migrations carefully

### Performance Issues

#### Slow API Response
1. Check database query performance
2. Enable SQL Server query profiling
3. Review indexes on tables
4. Implement caching for frequently accessed data

#### High Memory Usage
1. Check for memory leaks in services
2. Review dependency injection lifetimes
3. Monitor SQL connection pool

### Debugging Tips

#### Enable Detailed Logging
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug"
    }
  }
}
```

#### SQL Query Debugging
```csharp
// In BasicCrud.cs, enable query logging
private void LogQuery(string query, object parameters)
{
    Logger.Log($"SQL: {query}", "DEBUG");
    Logger.Log($"Parameters: {JsonSerializer.Serialize(parameters)}", "DEBUG");
}
```

### Log File Locations
- Application logs: `CRM/Logs/CRM_Logs.txt`
- Error logs: `CRM/Logs/CRM_Errors.txt`
- SQL Server logs: `docker logs crm_sql_server`

## 👨‍💻 Development Guidelines

### Code Standards
- Follow C# naming conventions
- Use async/await for I/O operations
- Implement proper error handling
- Write unit tests for new features
- Document public APIs

### Git Workflow
1. Create feature branch from `main`
2. Commit with descriptive messages
3. Write tests for new features
4. Create pull request
5. Code review required

### Adding New Features
1. Define models in `Models/`
2. Create database tables/procedures
3. Implement data access in `BasicCrud`
4. Add business logic in `Services/`
5. Create controller endpoints
6. Write tests
7. Update API documentation

### Security Checklist
- [ ] Validate all inputs
- [ ] Use parameterized queries
- [ ] Implement proper authentication
- [ ] Apply authorization checks
- [ ] Log security events
- [ ] Review OWASP guidelines