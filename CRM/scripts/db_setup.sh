#!/bin/bash
# filepath: /root/repos/dev-academy-CRM/CRM/scripts/db_setup.sh

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
        echo \ 
          "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" \
          > /etc/apt/sources.list.d/docker.list
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

# Step 8: Create the CRM database and tables
echo "Setting up the CRM database and tables..."
sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -C -Q "CREATE DATABASE $DB_NAME"
sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -C -d $DB_NAME -Q "
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
sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -C -d $DB_NAME -Q "SELECT * FROM Customers"
sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -C -d $DB_NAME -Q "SELECT * FROM TestTable"
if [ $? -ne 0 ]; then
    echo "Error: Failed to verify the setup. Check the database connection."
    exit 1
fi
echo "Setup verified successfully!"
# Clean up test table
echo "Cleaning up test table..."
sqlcmd -S localhost,$PORT -U SA -P "$SA_PASSWORD" -C -d $DB_NAME -Q "DROP TABLE TestTable"

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