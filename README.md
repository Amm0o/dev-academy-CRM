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

### 3. Run setup env script
```bash
# The bellow script works for Ubuntu and Arch
cd CRM/scripts/setup_dev_env.sh
``` 

## 🗄️ Database Setup

### Automated Setup
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

### Database Schema

#### Users Table
```sql
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);
```

#### Products Table
```sql
CREATE TABLE Products (
    ProductId INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    Category NVARCHAR(50),
    Price DECIMAL(18,2) NOT NULL,
    Stock INT NOT NULL DEFAULT 0,
    ProductGuid UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);
```

#### Orders Table
```sql
CREATE TABLE Orders (
    OrderId INT PRIMARY KEY IDENTITY(1,1),
    OrderGuid UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    UserId INT NOT NULL,
    UserNameOrder NVARCHAR(100) NOT NULL,
    OrderDescription NVARCHAR(500),
    OrderDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) 
        REFERENCES Users(UserId)
);

CREATE INDEX IX_Orders_OrderGuid ON Orders(OrderGuid);
```

#### OrderItems Table
```sql
CREATE TABLE OrderItems (
    OrderItemId INT PRIMARY KEY IDENTITY(1,1),
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) 
        REFERENCES Orders(OrderId),
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) 
        REFERENCES Products(ProductId)
);
```

#### Carts Table
```sql
CREATE TABLE Carts (
    CartId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL UNIQUE,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Carts_Users FOREIGN KEY (UserId)
        REFERENCES Users(UserId)
);
```

#### CartItems Table
```sql
CREATE TABLE CartItems (
    CartItemId INT PRIMARY KEY IDENTITY(1,1),
    CartId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_CartItems_Carts FOREIGN KEY (CartId) 
        REFERENCES Carts(CartId),
    CONSTRAINT FK_CartItems_Products FOREIGN KEY (ProductId) 
        REFERENCES Products(ProductId)
);
```

## ⚙️ Configuration

### appsettings.json
```json
{
  "Logging": {
    "minimumLogLevel": "Debug",
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionString": {
    "SqlServer": "Server=localhost,1433;Database=CRM;User Id=SA;Password=YourStrong@Passw0rd123!;TrustServerCertificate=True;Max Pool Size=100;Min Pool Size=5;"
  },
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong",
    "Issuer": "CRM-API",
    "Audience": "CRM-Users",
    "ExpiryInMinutes": 60
  },
  "AdminUser" : {
    "Email": "admin@crm.com",
    "Password": "StrongPassword123",
    "Name": "System Admin"
  }
}
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

## 📡 API Documentation
Available after running app: http://localhost:5205/swagger

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