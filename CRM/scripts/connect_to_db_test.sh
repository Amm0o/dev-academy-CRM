#!/bin/bash

# Variables (edit as needed)
SERVER="localhost,1433"
USER="SA"
PASSWORD="YourStrong@Passw0rd123!"
DATABASE="CRM"

# Run test queries using sqlcmd
sqlcmd -S "$SERVER" -U "$USER" -P "$PASSWORD" -C <<EOF
-- Check existing databases
SELECT name FROM sys.databases;
GO

-- Switch to your database
USE $DATABASE;
GO

-- Check tables in the database
SELECT table_name FROM information_schema.tables WHERE table_type = 'BASE TABLE';
GO

-- To see all data in a table (e.g., Customers)
SELECT * FROM Customers;
GO

-- To see all data in a table (e.g., Users)
SELECT * FROM Users;
GO
EOF

# Check exit status
if [ $? -eq 0 ]; then
  echo "Database connection and queries executed successfully."
else
  echo "Error: Could not connect or run queries."
  exit 1
fi