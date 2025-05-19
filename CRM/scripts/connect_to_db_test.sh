# From your host machine
sqlcmd -S localhost,1433 -U SA -P "YourStrong@Passw0rd123!" -C

-- Check existing databases
SELECT name FROM sys.databases
GO

-- Switch to your database
USE CRM
GO

-- Check tables in the database
SELECT table_name FROM information_schema.tables WHERE table_type = 'BASE TABLE'  
GO

-- To see all data in a table (e.g., Customers)
SELECT * FROM Customers
GO

-- To exit
EXIT