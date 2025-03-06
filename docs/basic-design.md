# Where to find project proposal
Email subject: __DEV Project__ sent on the 3rd of march 2025.


Starting a project like this can feel overwhelming due to the number of tasks, dependencies, and technologies involved. I’ll help you break it down into a structured plan with clear steps, timelines, and guidance on organizing the work, including when and how to write tests. The key is to tackle it incrementally, following the phases outlined (Fundamentals, Advanced Features, Scalability & Performance), while ensuring each piece builds on the last. Below is a detailed plan to get you started and keep you organized.

---

### High-Level Strategy
1. **Follow the Phases**: Stick to the three-phase structure (Fundamentals, Advanced Features, Scalability & Performance) as it naturally progresses from core functionality to refinements.
2. **Incremental Development**: Build and test small, functional pieces before moving to the next task.
3. **Tests Alongside Features**: Write unit tests as you implement features to catch issues early and ensure functionality aligns with requirements.
4. **Prioritize Dependencies**: Start with foundational components (e.g., database, models, API) that other features depend on.
5. **Timebox Tasks**: Estimate time for each subtask to stay on track and avoid getting stuck.

---

### Step-by-Step Plan

#### Phase 1: Application Fundamentals (30p)
Focus: Set up the backbone of the application—models, database, and basic API.

##### Step 1: Define OOP Models (10p)  
**Duration**: ~4-6 hours  
**Tasks**:
- **Design Classes**: Create C# classes for `User`, `Product`, `Order`, and `OrderItem` (or `CartItem`).
  - `User`: `Id`, `Name`, `Email`, `PasswordHash`, `Role` (enum: Regular, Admin).
  - `Product`: `Id`, `Name`, `Description`, `Price`, `StockQuantity`, `Category`.
  - `Order`: `Id`, `UserId`, `TotalPrice`, `Status` (enum: Pending, Shipped, Delivered), `OrderDate`.
  - `OrderItem`: `OrderId`, `ProductId`, `Quantity`.
- **Apply OOP Principles**:
  - Encapsulation: Use properties with private setters where appropriate.
  - Inheritance: Consider a base class (e.g., `Entity`) for `Id` if reusable.
  - Abstraction: Define interfaces (e.g., `IEntity`) for common behaviors.
  - Polymorphism: Use if extending functionality (e.g., different order types later).
- **Data Validation**: Add attributes (e.g., `[Required]`, `[Range]`) or custom validation logic in setters.
- **Tools**: Visual Studio (C#), StyleCop for code quality.

**Tests**:
- Write xUnit/NUnit tests for validation logic (e.g., `User.Email` must be valid, `Product.Price` must be positive).
- Example: `Assert.Throws<ArgumentException>(() => new Product { Price = -1 });`

---

##### Step 2: Set Up SQL Server Database with EF Core (10p)  
**Duration**: ~6-8 hours  
**Tasks**:
- **Create Database**: Use SQL Server Management Studio (SSMS) or EF Core migrations.
- **Define Schema**:
  - `Users`: `Id (PK)`, `Name`, `Email (unique)`, `PasswordHash`, `Role`.
  - `Products`: `Id (PK)`, `Name`, `Description`, `Price`, `StockQuantity`, `Category`.
  - `Orders`: `Id (PK)`, `UserId (FK)`, `TotalPrice`, `Status`, `OrderDate`.
  - `OrderItems`: `OrderId (FK)`, `ProductId (FK)`, `Quantity`, composite PK (`OrderId`, `ProductId`).
- **Relationships**:
  - `Users` 1-to-Many `Orders` (FK: `UserId`).
  - `Orders` 1-to-Many `OrderItems` (FK: `OrderId`).
  - `Products` Many-to-Many `Orders` via `OrderItems` (FKs: `ProductId`, `OrderId`).
- **EF Core Setup**:
  - Install `Microsoft.EntityFrameworkCore.SqlServer`.
  - Create `AppDbContext` with `DbSet` for each entity.
  - Use Fluent API or attributes to configure relationships and constraints.
- **Seed Data**: Add sample users, products, and orders via migrations or a seed method.
- **Tools**: SSMS, EF Core CLI (`dotnet ef migrations add InitialCreate`).

**Tests**:
- Test database connectivity and CRUD operations (e.g., `context.Users.Add(user)`).
- Use an in-memory database (`Microsoft.EntityFrameworkCore.InMemory`) for unit tests.

---

##### Step 3: Build Basic RESTful API (10p)  
**Duration**: ~6-8 hours  
**Tasks**:
- **Setup ASP.NET Core Project**:
  - Create a new Web API project (`dotnet new webapi`).
  - Add EF Core dependency and configure `AppDbContext` in `Program.cs`.
- **Controllers**:
  - `UsersController`: `POST /api/users/register`, `GET /api/users/{id}`.
  - `ProductsController`: `GET /api/products`, `POST /api/products` (admin only, later).
  - `OrdersController`: `POST /api/orders`, `GET /api/orders/{id}`.
- **HTTP Responses**:
  - Return `201 Created` for successful creation, `400 Bad Request` for validation errors, etc.
- **Validation**: Use model validation (`[ApiController]` attribute auto-validates).
- **Tools**: Postman (initial manual testing), Swagger (built-in for API exploration).

**Tests**:
- Write xUnit tests for API endpoints (e.g., `POST /api/products` creates a product).
- Use `WebApplicationFactory` to mock the app for integration tests.

---

#### Phase 2: Advanced Features (30p)
Focus: Add security, search, and testing.

##### Step 4: Authentication & Authorization (10p)  
**Duration**: ~6-8 hours  
**Tasks**:
- **JWT Setup**:
  - Install `Microsoft.AspNetCore.Authentication.JwtBearer`.
  - Configure JWT in `Program.cs` (issuer, audience, secret key).
- **Auth Endpoints**:
  - `POST /api/auth/register`: Hash password (use `BCrypt.Net` or `Identity`).
  - `POST /api/auth/login`: Return JWT token.
  - `POST /api/auth/logout`: (Optional, client-side token discard).
- **Role-Based Access**:
  - Add `[Authorize(Roles = "Admin")]` to admin-only endpoints (e.g., product creation).
- **Tools**: Postman (test token generation).

**Tests**:
- Test token generation and validation.
- Test unauthorized access (`401`) and role restrictions (`403`).

---

##### Step 5: Product Search Optimization (10p)  
**Duration**: ~4-6 hours  
**Tasks**:
- **Basic Search**: Add `GET /api/products?search={term}&category={cat}`.
- **Optimization**:
  - Use EF Core LINQ for filtering (`Where(p => p.Name.Contains(term))`).
  - Consider SQL Full-Text Search for large datasets (requires SQL Server setup).
- **Indexing**: Add indexes on `Products(Name)` and `Products(Category)` in migrations.

**Tests**:
- Test search returns correct results and handles edge cases (e.g., empty term).

---

##### Step 6: API Testing with Postman/Fiddler (10p)  
**Duration**: ~4-6 hours  
**Tasks**:
- **Postman Collection**:
  - Create requests for all endpoints (e.g., `GET /api/products`, `POST /api/orders`).
  - Add tests (e.g., `pm.test("Status is 200", () => pm.response.to.have.status(200))`).
- **Scenarios**:
  - Valid data, invalid data, unauthorized access, etc.
- **Fiddler**: Inspect HTTP traffic for debugging.

**Tests**: Export Postman collection as evidence.

---

#### Phase 3: Scalability & Performance (30p)
Focus: Refine with patterns, reporting, and optimization.

##### Step 7: Implement Design Pattern (10p)  
**Duration**: ~4-6 hours  
**Tasks**:
- **Repository Pattern**:
  - Create `IRepository<T>` with methods like `Add`, `GetById`, `Update`.
  - Implement `UserRepository`, `ProductRepository`, etc., using `AppDbContext`.
- **UnitOfWork**:
  - Create `IUnitOfWork` with repositories and `SaveChanges`.
- **Integration**: Use in controllers.

**Tests**:
- Test repository CRUD operations.

---

##### Step 8: Reporting & Analytics (10p)  
**Duration**: ~6-8 hours  
**Tasks**:
- **Endpoints**:
  - `GET /api/reports/sales?year={year}`: Total sales.
  - `GET /api/reports/top-products`: Most popular products.
  - `GET /api/reports/top-customers`: Top buyers.
- **Queries**:
  - Use LINQ or raw SQL (e.g., `SELECT SUM(TotalPrice) FROM Orders WHERE YEAR(OrderDate) = @year`).

**Tests**:
- Test report accuracy with seeded data.

---

##### Step 9: Performance Optimization & Unit Testing (10p)  
**Duration**: ~6-8 hours  
**Tasks**:
- **Indexing**: Already added in Step 5.
- **Caching**: Use `IMemoryCache` for products or reports.
- **Unit Tests**: Expand tests for all major features (auth, CRUD, reports).

**Tests**: Aim for ~70-80% code coverage.

---

### Timeline (Rough Estimate)
- **Phase 1**: 16-22 hours (~2-3 days).
- **Phase 2**: 14-20 hours (~2-3 days).
- **Phase 3**: 16-22 hours (~2-3 days).
- **Total**: ~46-64 hours (~1-2 weeks, depending on pace).

---

### Should You Write Tests as You Go?
Yes, absolutely! Here’s why and how:
- **Why**: Writing tests alongside features ensures they work as intended, reduces debugging later, and meets the “Testing” evaluation criterion.
- **How**:
  - Write unit tests for models and business logic immediately (e.g., validation).
  - Add integration tests after API endpoints are built.
  - Use a test-driven development (TDD) lite approach: Write a test, implement the feature, verify it passes.
- **Tools**: xUnit/NUnit, Moq (for mocking), EF Core In-Memory Database.

---

### Tips to Stay Organized
1. **Version Control**: Use Azure Repos (or Git). Commit after each subtask (e.g., “Add User model and tests”).
2. **Task Tracking**: Use a simple list (e.g., Trello, Notion) to mark completed tasks.
3. **Documentation**: Keep a `README.md` with setup instructions and progress notes.
4. **Modular Code**: Organize by feature (e.g., `Controllers/`, `Models/`, `Repositories/`).

---

### Where to Start?
Start with **Step 1: Define OOP Models**. It’s the foundation for everything else—database, API, and features. Once you have solid models and tests, move to the database (Step 2), then the API (Step 3). This order ensures dependencies are met logically.

Let me know if you want a deeper dive into any step or help with code snippets!

 
