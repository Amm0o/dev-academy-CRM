#!/bin/bash

# Database connection settings
SERVER="localhost,1433"
USER="sa"
PASSWORD="YourStrong@Passw0rd123!"
DATABASE="CRM"

# Parse command line arguments
REGENERATE=false
FORCE=false
TABLES_ONLY=false
REGISTER_USER=false
SHOW_SCHEMA=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --regenerate)
            REGENERATE=true
            shift
            ;;
        --force)
            FORCE=true
            shift
            ;;
        --tables-only)
            TABLES_ONLY=true
            shift
            ;;
        --register-user)
            REGISTER_USER=true
            shift
            ;;
        --show-schema)
            SHOW_SCHEMA=true
            shift
            ;;
        --help)
            show_help
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Function to show help
show_help() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --regenerate      Drop and recreate the database"
    echo "  --register-user   Register a new user with BCrypt password hashing"
    echo "  --show-schema     Show database schema"
    echo "  --help           Show this help message"
    echo ""
    echo "If no options are provided, the script will connect and show basic database info."
}

# Function to test database connection
test_connection() {
    echo "Testing database connection..."
    
    sqlcmd -S "$SERVER" -U "$USER" -P "$PASSWORD" -C -Q "SELECT @@VERSION" > /dev/null 2>&1
    
    if [ $? -eq 0 ]; then
        echo "✓ Successfully connected to SQL Server"
    else
        echo "✗ Failed to connect to SQL Server"
        echo "Make sure SQL Server is running and credentials are correct"
        exit 1
    fi
}

# Function to show database schema
show_schema() {
    echo "=== DATABASE SCHEMA ==="
    
    sqlcmd -S "$SERVER" -U "$USER" -P "$PASSWORD" -C <<EOF
-- Check if database exists
IF DB_ID('$DATABASE') IS NULL
BEGIN
    PRINT 'Database $DATABASE does not exist.';
END
ELSE
BEGIN
    USE $DATABASE;
    
    PRINT 'Tables in database $DATABASE:';
    SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';
    
    PRINT '';
    PRINT 'Users table schema (if exists):';
    IF OBJECT_ID('Users', 'U') IS NOT NULL
    BEGIN
        SELECT 
            COLUMN_NAME,
            DATA_TYPE,
            IS_NULLABLE,
            COLUMN_DEFAULT,
            CHARACTER_MAXIMUM_LENGTH
        FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'Users'
        ORDER BY ORDINAL_POSITION;
    END
    ELSE
    BEGIN
        PRINT 'Users table does not exist.';
    END
    
    PRINT '';
    PRINT 'Sample Users data (first 5 rows):';
    IF OBJECT_ID('Users', 'U') IS NOT NULL
    BEGIN
        SELECT TOP 5 UserId, Name, Email, Role, UserCreateTime 
        FROM Users;
    END
END
GO
EOF
}

# Function to drop database tables in dependency order
drop_tables() {
    echo "Dropping tables in dependency order..."
    
    sqlcmd -S "$SERVER" -U "$USER" -P "$PASSWORD" -C <<EOF
USE $DATABASE;
GO

-- Drop tables in reverse dependency order to handle foreign keys
PRINT 'Dropping OrderItems table...';
IF OBJECT_ID('OrderItems', 'U') IS NOT NULL
    DROP TABLE OrderItems;

PRINT 'Dropping Orders table...';
IF OBJECT_ID('Orders', 'U') IS NOT NULL
    DROP TABLE Orders;

PRINT 'Dropping CartItems table...';
IF OBJECT_ID('CartItems', 'U') IS NOT NULL
    DROP TABLE CartItems;

PRINT 'Dropping Carts table...';
IF OBJECT_ID('Carts', 'U') IS NOT NULL
    DROP TABLE Carts;

PRINT 'Dropping Products table...';
IF OBJECT_ID('Products', 'U') IS NOT NULL
    DROP TABLE Products;

PRINT 'Dropping Users table...';
IF OBJECT_ID('Users', 'U') IS NOT NULL
    DROP TABLE Users;

PRINT 'All tables dropped successfully.';
GO
EOF
}

# Function to regenerate database
regenerate_database() {
    echo "=== REGENERATING DATABASE ==="
    
    # First check if database exists
    sqlcmd -S "$SERVER" -U "$USER" -P "$PASSWORD" -C <<EOF
IF DB_ID('$DATABASE') IS NOT NULL
BEGIN
    PRINT 'Database $DATABASE exists. Dropping tables...';
END
ELSE
BEGIN
    PRINT 'Database $DATABASE does not exist. Creating...';
    CREATE DATABASE $DATABASE;
    PRINT 'Database $DATABASE created successfully.';
END
GO
EOF

    # Drop existing tables if they exist
    drop_tables
    
    # Run the main database setup script
    echo "Running database setup script..."
    bash "$(dirname "$0")/db_setup.sh"
    
    if [ $? -eq 0 ]; then
        echo "✓ Database regenerated successfully!"
    else
        echo "✗ Error regenerating database"
        exit 1
    fi
}

# Function to generate BCrypt hash
generate_bcrypt_hash() {
    local password="$1"
    
    # Check if python3 is available
    if ! command -v python3 &> /dev/null; then
        echo "Error: python3 is required for BCrypt password hashing"
        echo "Please install python3 and bcrypt library: pip3 install bcrypt"
        exit 1
    fi
    
    # Generate BCrypt hash using Python
    local hash=$(python3 << EOF
import bcrypt
import sys

try:
    password = '$password'.encode('utf-8')
    salt = bcrypt.gensalt()
    hashed = bcrypt.hashpw(password, salt)
    print(hashed.decode('utf-8'))
except ImportError:
    print("ERROR: bcrypt library not installed. Run: pip3 install bcrypt")
    sys.exit(1)
except Exception as e:
    print(f"ERROR: {e}")
    sys.exit(1)
EOF
)

    if [[ "$hash" == ERROR:* ]]; then
        echo "$hash"
        exit 1
    fi
    
    echo "$hash"
}

# Function to register user with proper BCrypt hashing
register_user() {
    echo "=== REGISTERING NEW USER (BCrypt) ==="
    
    # Check dependencies first
    if ! command -v python3 &> /dev/null; then
        echo "Error: python3 is required for BCrypt password hashing"
        echo ""
        echo "Installation instructions:"
        echo "  Ubuntu/Debian: sudo apt install python3 python3-pip"
        echo "  Arch Linux: sudo pacman -S python python-pip"
        echo ""
        echo "Then install bcrypt: pip3 install bcrypt"
        exit 1
    fi
    
    # Test if bcrypt is available
    python3 -c "import bcrypt" 2>/dev/null
    if [ $? -ne 0 ]; then
        echo "Error: bcrypt library is not installed"
        echo "Please install it with: pip3 install bcrypt"
        exit 1
    fi
    
    # Prompt for user details
    echo "Enter user details:"
    read -p "Name: " user_name
    read -p "Email: " user_email
    read -s -p "Password: " user_password
    echo
    read -p "Role (Regular/Admin) [Regular]: " user_role
    
    # Default role if empty
    if [ -z "$user_role" ]; then
        user_role="Regular"
    fi
    
    # Validate inputs
    if [ -z "$user_name" ] || [ -z "$user_email" ] || [ -z "$user_password" ]; then
        echo "Error: Name, email, and password are required!"
        exit 1
    fi
    
    # Validate role
    if [[ "$user_role" != "Regular" && "$user_role" != "Admin" ]]; then
        echo "Error: Role must be 'Regular' or 'Admin'"
        exit 1
    fi
    
    echo "Generating BCrypt hash for password..."
    
    # Generate BCrypt hash
    password_hash=$(generate_bcrypt_hash "$user_password")
    
    if [ $? -ne 0 ]; then
        echo "Error generating password hash"
        exit 1
    fi
    
    echo "✓ Password hash generated successfully"
    echo "Registering user with email: $user_email"
    
    # Escape single quotes in data for SQL
    user_name_escaped=$(echo "$user_name" | sed "s/'/''/g")
    user_email_escaped=$(echo "$user_email" | sed "s/'/''/g")
    password_hash_escaped=$(echo "$password_hash" | sed "s/'/''/g")
    user_role_escaped=$(echo "$user_role" | sed "s/'/''/g")
    
    sqlcmd -S "$SERVER" -U "$USER" -P "$PASSWORD" -C <<EOF
USE $DATABASE;
GO

-- Check if Users table exists, if not create it with proper schema
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
BEGIN
    PRINT 'Users table does not exist. Creating table...';
    CREATE TABLE Users (
        UserId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Email NVARCHAR(255) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        Role NVARCHAR(50) NOT NULL DEFAULT 'Regular',
        UserCreateTime DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UserUpdateTime DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    PRINT 'Users table created successfully with PasswordHash column';
END
ELSE
BEGIN
    -- Check if the required columns exist, if not add them
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'PasswordHash')
    BEGIN
        -- Check if there's an old Password column and rename it
        IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Password')
        BEGIN
            EXEC sp_rename 'Users.Password', 'PasswordHash', 'COLUMN';
            PRINT 'Renamed Password column to PasswordHash';
        END
        ELSE
        BEGIN
            ALTER TABLE Users ADD PasswordHash NVARCHAR(255);
            PRINT 'Added PasswordHash column to Users table';
        END
    END
    
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Role')
    BEGIN
        ALTER TABLE Users ADD Role NVARCHAR(50) DEFAULT 'Regular';
        PRINT 'Added Role column to Users table';
    END
    
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'UserCreateTime')
    BEGIN
        ALTER TABLE Users ADD UserCreateTime DATETIME2 DEFAULT GETUTCDATE();
        PRINT 'Added UserCreateTime column to Users table';
    END
    
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'UserUpdateTime')
    BEGIN
        ALTER TABLE Users ADD UserUpdateTime DATETIME2 DEFAULT GETUTCDATE();
        PRINT 'Added UserUpdateTime column to Users table';
    END
    
    -- Ensure NOT NULL constraints are set properly
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'PasswordHash' AND IS_NULLABLE = 'YES')
    BEGIN
        -- Update any NULL values before adding NOT NULL constraint
        UPDATE Users SET PasswordHash = 'temp_hash' WHERE PasswordHash IS NULL;
        ALTER TABLE Users ALTER COLUMN PasswordHash NVARCHAR(255) NOT NULL;
        PRINT 'Set PasswordHash column to NOT NULL';
    END
END
GO

-- Check if email already exists
IF EXISTS (SELECT 1 FROM Users WHERE Email = '$user_email_escaped')
BEGIN
    PRINT 'Error: User with email $user_email already exists';
    SELECT 'DUPLICATE_EMAIL' AS Status;
END
ELSE
BEGIN
    -- Insert the new user with BCrypt-hashed password
    INSERT INTO Users (Name, Email, PasswordHash, Role, UserCreateTime, UserUpdateTime)
    VALUES (
        '$user_name_escaped',
        '$user_email_escaped', 
        '$password_hash_escaped',
        '$user_role_escaped',
        GETUTCDATE(),
        GETUTCDATE()
    );
    
    PRINT 'User registered successfully with BCrypt-hashed password!';
    SELECT 'SUCCESS' AS Status, SCOPE_IDENTITY() AS UserId;
    
    -- Show the created user (without password)
    SELECT UserId, Name, Email, Role, UserCreateTime, UserUpdateTime 
    FROM Users 
    WHERE Email = '$user_email_escaped';
END
GO
EOF

    if [ $? -eq 0 ]; then
        echo "✓ User registration completed successfully!"
        echo ""
        echo "User '$user_name' with email '$user_email' has been registered."
        echo "Password has been securely hashed using BCrypt."
        echo ""
        echo "You can now test the login with:"
        echo "  curl -k -X POST http://localhost:5205/api/auth/login \\"
        echo "    -H 'Content-Type: application/json' \\"
        echo "    -d '{\"email\":\"$user_email\",\"password\":\"$user_password\"}'"
    else
        echo "✗ Error: Failed to register user."
        exit 1
    fi
}

# Function to show basic database info
show_basic_info() {
    echo "=== BASIC DATABASE INFO ==="
    
    sqlcmd -S "$SERVER" -U "$USER" -P "$PASSWORD" -C <<EOF
-- Check if database exists
IF DB_ID('$DATABASE') IS NULL
BEGIN
    PRINT 'Database $DATABASE does not exist. Run with --regenerate to create it.';
END
ELSE
BEGIN
    USE $DATABASE;
    PRINT 'Database: $DATABASE';
    PRINT 'Server: $SERVER';
    PRINT '';
    
    -- Count tables
    DECLARE @TableCount INT;
    SELECT @TableCount = COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';
    PRINT 'Number of tables: ' + CAST(@TableCount AS VARCHAR);
    
    -- Count users if Users table exists
    IF OBJECT_ID('Users', 'U') IS NOT NULL
    BEGIN
        DECLARE @UserCount INT;
        SELECT @UserCount = COUNT(*) FROM Users;
        PRINT 'Number of users: ' + CAST(@UserCount AS VARCHAR);
    END
    ELSE
    BEGIN
        PRINT 'Users table does not exist.';
    END
END
GO
EOF
}

# Main script logic
main() {
    # Test connection first
    test_connection
    
    # Handle different modes
    if [ "$REGENERATE" = true ]; then
        regenerate_database
    elif [ "$REGISTER_USER" = true ]; then
        register_user
    elif [ "$SHOW_SCHEMA" = true ]; then
        show_schema
    else
        show_basic_info
    fi
}

# Run main function with all arguments
main "$@"