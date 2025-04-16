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

# Step 1: Check if Docker is installed, install if not (Ubuntu/Debian assumed)
if ! [ -x "$(command -v docker)" ]; then
    echo "Docker not found. Installing Docker..."
    sudo apt-get update -y
    sudo apt-get install -y docker.io
    sudo systemctl start docker
    sudo systemctl enable docker
    sudo usermod -aG docker $USER
    echo "Docker installed. You may need to log out and back in."
else
    echo "Docker is already installed."
fi

# Step 2: Check if port 1433 is in use
if sudo netstat -tuln | grep -q ":$PORT "; then
    echo "Port $PORT is already in use. Please free it or choose a different port."
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

# Step 7: Install sqlcmd (if not installed)
if ! [ -x "$(command -v sqlcmd)" ]; then
    echo "Installing sqlcmd..."
    sudo apt-get update -y
    sudo apt-get install -y curl gnupg
    curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
    curl https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/prod.list | sudo tee /etc/apt/sources.list.d/msprod.list
    sudo apt-get update -y
    sudo apt-get install -y mssql-tools unixodbc-dev
    echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> ~/.bashrc
    source ~/.bashrc
else
    echo "sqlcmd is already installed."
fi

# Step 8: Create the CRM database and tables
echo "Setting up the CRM database and tables..."
/opt/mssql-tools/bin/sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -Q "CREATE DATABASE $DB_NAME"
/opt/mssql-tools/bin/sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -d $DB_NAME -Q "
    CREATE TABLE Customers (
        CustomerID INT PRIMARY KEY IDENTITY(1,1),
        FirstName NVARCHAR(50),
        LastName NVARCHAR(50),
        Email NVARCHAR(100),
        Phone NVARCHAR(20),
        CreatedDate DATETIME DEFAULT GETDATE()
    );
    INSERT INTO Customers (FirstName, LastName, Email, Phone) 
    VALUES ('John', 'Doe', 'john.doe@example.com', '123-456-7890');
    
    CREATE TABLE TestTable (
        TestID INT PRIMARY KEY IDENTITY(1,1),
        TestData NVARCHAR(100)
    );
    INSERT INTO TestTable (TestData) 
    VALUES ('Test Entry');
"

# Step 9: Verify the setup
echo "Verifying the setup..."
/opt/mssql-tools/bin/sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -d $DB_NAME -Q "SELECT * FROM Customers"
/opt/mssql-tools/bin/sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -d $DB_NAME -Q "SELECT * FROM TestTable"
if [ $? -ne 0 ]; then
    echo "Error: Failed to verify the setup. Check the database connection."
    exit 1
fi
echo "Setup verified successfully!"
# Clean up test table
echo "Cleaning up test table..."
/opt/mssql-tools/bin/sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -d $DB_NAME -Q "DROP TABLE TestTable"

# Step 10: Display connection details
echo "Setup complete! Your SQL Server is running in a Docker container."
echo "Connection details:"
echo "  Host: localhost"
echo "  Port: $PORT"
echo "  Database: $DB_NAME"
echo "  Username: SA"
echo "  Password: $SA_PASSWORD"
echo "To connect from your CRM app, use: Server=localhost,$PORT;Database=$DB_NAME;User Id=SA;Password=$SA_PASSWORD;"
echo "To stop the container: docker stop $CONTAINER_NAME"
echo "To start it again later: docker start $CONTAINER_NAME"