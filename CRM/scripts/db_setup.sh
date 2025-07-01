#!/bin/bash

# Variables
CONTAINER_NAME="crm_sql_server"
SA_PASSWORD="YourStrong@Passw0rd123!"  # Updated to meet complexity requirements
DB_NAME="CRM"
PORT="1433"

if [ "$EUID" -ne 0 ]; then
    echo "Please run the db setup script as root"
    exit 1
fi

# Detect OS
if [ -f /etc/os-release ]; then
    . /etc/os-release
    OS=$ID
else
    echo "Cannot detect OS. Exiting."
    exit 1
fi

# Step 1: Check if Docker is installed, install if not
if ! [ -x "$(command -v docker)" ]; then
    echo "Docker not found. Installing Docker..."
    if [ "$OS" = "arch" ]; then
        pacman -Sy --noconfirm
        pacman -S --noconfirm docker
        systemctl start docker
        systemctl enable docker
    elif [ "$OS" = "ubuntu" ]; then
        apt update
        apt install -y apt-transport-https ca-certificates curl software-properties-common lsb-release
        curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
        echo "deb [arch=amd64 signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu noble stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
        apt update
        apt install -y docker-ce
        systemctl start docker
        systemctl enable docker
    else
        echo "Unsupported OS: $OS. Please install Docker manually."
        exit 1
    fi
    echo "Docker installed."
else
    echo "Docker is already installed."
fi

# Step 2: Check if port 1433 is in use
if ss -tuln | grep -q ":$PORT "; then
    echo "Port $PORT is already in use. Please free it or choose a different port."
    docker ps -a
    echo "You can stop the container using: docker stop <container_id>"
    exit 1
fi

# Step 3: Pull the MSSQL Server image
echo "Pulling MSSQL Server Docker image..."
docker pull mcr.microsoft.com/mssql/server:2022-latest

# Step 4: Stop and remove any existing container
echo "Checking for existing container..."
docker stop $CONTAINER_NAME 2>/dev/null || true
docker rm $CONTAINER_NAME 2>/dev/null || true

# Step 5: Run the MSSQL container
echo "Starting MSSQL container..."
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=$SA_PASSWORD" \
    -p $PORT:1433 --name $CONTAINER_NAME -d mcr.microsoft.com/mssql/server:2022-latest

# Step 6: Wait for MSSQL to be ready
echo "Waiting for MSSQL to initialize..."
attempts=0
max_attempts=30
until docker logs $CONTAINER_NAME 2>&1 | grep -q "SQL Server is now ready for client connections" || [ $attempts -ge $max_attempts ]; do
    sleep 2
    attempts=$((attempts + 1))
    echo "Attempt $attempts/$max_attempts: Waiting for SQL Server to start..."
done

if [ $attempts -ge $max_attempts ]; then
    echo "Error: SQL Server failed to start. Check logs with: docker logs $CONTAINER_NAME"
    exit 1
fi

# Step 7: Install sqlcmd (Arch or Ubuntu way)
if ! [ -x "$(command -v sqlcmd)" ]; then
    echo "Installing mssql-tools (sqlcmd)..."
    if [ "$OS" = "arch" ]; then
        mkdir -p /tmp/aur_build && cd /tmp/aur_build
        git clone https://aur.archlinux.org/msodbcsql.git
        chown -R $(logname):$(logname) /tmp/aur_build
        cd msodbcsql
        sudo -u $(logname) makepkg
        sudo pacman -U --noconfirm msodbcsql-*.pkg.tar.zst
        cd /tmp/aur_build
        pacman -S --noconfirm base-devel git
        git clone https://aur.archlinux.org/mssql-tools.git
        chown -R $(logname):$(logname) /tmp/aur_build
        cd mssql-tools
        sudo -u $(logname) makepkg -s
        sudo pacman -U --noconfirm mssql-tools-*.pkg.tar.zst
        echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> /etc/profile.d/mssql-tools.sh
        source /etc/profile.d/mssql-tools.sh
        cd /
        rm -rf /tmp/aur_build
    elif [ "$OS" = "ubuntu" ]; then
        curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add -
        curl https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/prod.list > /etc/apt/sources.list.d/mssql-release.list
        apt update
        ACCEPT_EULA=Y apt install -y msodbcsql18
        ACCEPT_EULA=Y apt install -y mssql-tools18
        echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> /etc/profile.d/mssql-tools.sh
        source /etc/profile.d/mssql-tools.sh
    else
        echo "Unsupported OS: $OS. Please install mssql-tools/sqlcmd manually."
        exit 1
    fi
else
    echo "sqlcmd is already installed."
fi

# Step 8: Create the CRM database
echo "Creating the CRM database..."
sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -C -Q "CREATE DATABASE $DB_NAME"

# Step 9: Create all CRM tables (matching CreateDatabase.cs)
echo "Setting up the CRM database tables..."
sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -C -d $DB_NAME -Q "
    -- Create Users table
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
    BEGIN
        CREATE TABLE Users (
            UserId INT PRIMARY KEY IDENTITY(1,1),
            Name NVARCHAR(100) NOT NULL,
            Email NVARCHAR(100) NOT NULL UNIQUE,
            PasswordHash NVARCHAR(255) NOT NULL,
            Role NVARCHAR(255) NOT NULL,
            CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
        );
        PRINT 'Users table created';
    END
    ELSE
        PRINT 'Users table already exists';
"

sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -C -d $DB_NAME -Q "
    -- Create Products table
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
    BEGIN
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
        PRINT 'Products table created';
    END
    ELSE
        PRINT 'Products table already exists';
"

sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -C -d $DB_NAME -Q "
    -- Create Orders table
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
    BEGIN
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
        PRINT 'Orders table created';
    END
    ELSE
        PRINT 'Orders table already exists';
"

sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -C -d $DB_NAME -Q "
    -- Create OrderItems table
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderItems')
    BEGIN
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
        PRINT 'OrderItems table created';
    END
    ELSE
        PRINT 'OrderItems table already exists';
"

sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -C -d $DB_NAME -Q "
    -- Create Carts table
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Carts')
    BEGIN
        CREATE TABLE Carts (
            CartId INT PRIMARY KEY IDENTITY(1,1),
            UserId INT NOT NULL UNIQUE,
            CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
            UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
            CONSTRAINT FK_Carts_Users FOREIGN KEY (UserId)
                REFERENCES Users(UserId)
        );
        PRINT 'Carts table created';
    END
    ELSE
        PRINT 'Carts table already exists';
"

sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -C -d $DB_NAME -Q "
    -- Create CartItems table
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CartItems')
    BEGIN
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
        PRINT 'CartItems table created';
    END
    ELSE
        PRINT 'CartItems table already exists';
"

# Step 10: Create initial admin user (matching appsettings.json)
echo "Creating initial admin user..."
sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -C -d $DB_NAME -Q "
    -- Insert admin user if not exists
    IF NOT EXISTS (SELECT * FROM Users WHERE Email = 'admin@crm.com')
    BEGIN
        INSERT INTO Users (Name, Email, PasswordHash, Role, CreatedAt)
        VALUES (
            'System Admin', 
            'admin@crm.com', 
            '\$2a\$11\$placeholder.hash.for.StrongPassword123', 
            'Admin', 
            GETDATE()
        );
        PRINT 'Admin user created - Email: admin@crm.com';
        PRINT 'Note: Update password hash using your application';
    END
    ELSE
        PRINT 'Admin user already exists';
"

# Step 11: Insert sample data
echo "Inserting sample data..."
sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -C -d $DB_NAME -Q "
    -- Insert sample products
    IF NOT EXISTS (SELECT * FROM Products WHERE Name = 'Sample Product 1')
    BEGIN
        INSERT INTO Products (Name, Description, Category, Price, Stock, CreatedAt, UpdatedAt)
        VALUES 
            ('Sample Product 1', 'A great product for testing', 'Electronics', 99.99, 50, GETDATE(), GETDATE()),
            ('Sample Product 2', 'Another awesome product', 'Clothing', 49.99, 25, GETDATE(), GETDATE()),
            ('Sample Product 3', 'Premium quality item', 'Home & Garden', 149.99, 10, GETDATE(), GETDATE());
        PRINT 'Sample products created';
    END
    ELSE
        PRINT 'Sample products already exist';
"

# Step 12: Verify the setup
echo "Verifying the setup..."
sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -C -d $DB_NAME -Q "
    PRINT 'Database Tables:';
    SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';
    
    PRINT 'Users Count:';
    SELECT COUNT(*) as UserCount FROM Users;
    
    PRINT 'Products Count:';
    SELECT COUNT(*) as ProductCount FROM Products;
"

if [ $? -ne 0 ]; then
    echo "Error: Failed to verify the setup. Check the database connection."
    exit 1
fi

echo "Setup verified successfully!"

# Step 13: Display connection details
echo "========================================="
echo "CRM Database Setup Complete!"
echo "========================================="
echo "Connection details:"
echo "  Host: localhost"
echo "  Port: $PORT"
echo "  Database: $DB_NAME"
echo "  Username: SA"
echo "  Password: $SA_PASSWORD"
echo ""
echo "Connection String:"
echo "  Server=localhost,$PORT;Database=$DB_NAME;User Id=SA;Password=$SA_PASSWORD;TrustServerCertificate=True;"
echo ""
echo "Admin User:"
echo "  Email: admin@crm.com"
echo "  Password: StrongPassword123 (update hash via application)"
echo ""
echo "Container Management:"
echo "  Stop: docker stop $CONTAINER_NAME"
echo "  Start: docker start $CONTAINER_NAME"
echo "  Logs: docker logs $CONTAINER_NAME"
echo "========================================="