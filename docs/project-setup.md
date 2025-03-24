# SQL Server
- curl https://packages.microsoft.com/keys/microsoft.asc | sudo tee /etc/apt/trusted.gpg.d/microsoft.asc
- sudo add-apt-repository "$(wget -qO- https://packages.microsoft.com/config/ubuntu/20.04/mssql-server-2022.list)"
- sudo apt-get update
- sudo apt-get install -y mssql-server
- sudo /opt/mssql/bin/mssql-conf setup
- systemctl status mssql-server --no-pager

# Dotnet
- dotnet tool install --global dotnet-ef

# Create Dotnet project
- dotnet new webapi -o CRM

# Build/Run
> from the CRM/ directory
- dotnet build
- dotnet run

# Set up test
- dotnet new xunit -o CRM.Tests
- cd CRM.Tests
- dotnet add reference ../CRM/CRM.csproj
- dotnet add package xunit
- dotnet add package Microsoft.NET.Test.Sdk

# Run tests
> from CRM.Tests
- dotnet test