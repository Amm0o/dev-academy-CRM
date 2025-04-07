# Installing Docker
- sudo apt update && sudo apt upgrade -y
- sudo apt install -y apt-transport-https ca-certificates curl software-properties-common
- curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
- sudo apt update
- sudo apt install -y docker-ce docker-ce-cli containerd.io
- sudo systemctl start docker
- sudo systemctl enable docker
- sudo usermod -aG docker $USER

# Testing installation
- docker run hello-world
- sudo docker run hello-world

# Installing SQL Container
## Pull the Image
- sudo docker pull mcr.microsoft.com/mssql/server:2022-latest

## Create a persistent volume since this is a CRM
- sudo docker volume create mssql-data

## Run the container
```sh
sudo docker run -d \
  --name sql_server_crm \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=My$ecureP@ssw0rd2025" \
  -e "MSSQL_PID=Express" \
  -p 1433:1433 \
  -v mssql-data:/var/opt/mssql \
  mcr.microsoft.com/mssql/server:2022-latest
```

## Check that container is running 
- sudo docker ps

# Securing the container 
- The SA password is already strong. Avoid using it in production scripts—consider a secrets manager (e.g., Azure Key Vault) when deploying.
- Create application specific users
```sh
sudo docker exec -it sql_server_crm /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U SA -P "My$ecureP@ssw0rd2025" \
  -Q 'CREATE LOGIN crm_user WITH PASSWORD = "CrM$ecure2025!@#"; CREATE USER crm_user FOR LOGIN crm_user; ALTER ROLE db_owner ADD MEMBER crm_user;' \
  -C
```
> If the above throws an error use the bellow command to access the containers shell:
> ```sudo docker exec -it sql_server_crm /bin/bash```
> Then look for the CMD path:
> ```find / -name sqlcmd 2>/dev/null ```
> Then rerun the above command.

## Check if user created logged in
```sh
sudo docker exec -it sql_server_crm /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U SA -P "My$ecureP@ssw0rd2025" \
  -Q "SELECT name, type_desc FROM sys.server_principals WHERE name = 'crm_user';" \
  -C
```

## Limit the IPs that can access the SQL DB !!!!!!!!!!!!!!!!!!!!!!!!!!!!

## Enable Encryption
> Generate a self-signed certificate (or use a CA-issued one for production)

```sh
openssl req -x509 -nodes -newkey rsa:2048 -subj "/CN=crm-sql" -keyout mssql.key -out mssql.crt -days 365
```

Now Copy the certificate into the container

```sh
sudo docker cp mssql.key sql_server_crm:/var/opt/mssql/ && sudo docker cp mssql.crt sql_server_crm:/var/opt/mssql/
```

## Configure SQL server to use the certificates
```sh
sudo docker exec -it sql_server_crm /opt/mssql/bin/mssql-conf set network.tlscert /var/opt/mssql/mssql.crt
sudo docker exec -it sql_server_crm /opt/mssql/bin/mssql-conf set network.tlskey /var/opt/mssql/mssql.key
sudo docker exec -it sql_server_crm /opt/mssql/bin/mssql-conf set network.tlsprotocols 1.2
sudo docker restart sql_server_crm
```

## Configure regular backups
> Schedule regular backups to a mounted volume or external storage
```sh
sudo docker exec -it sql_server_crm /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "My$ecureP@ssw0rd2025" -Q -N -C"BACKUP DATABASE master TO DISK='/var/opt/mssql/backup/master.bak'"
```
> -C is bypassing certitificate
## Limit Resource Usage:

    Cap CPU and memory to prevent the container from overwhelming WSL:
    bash

        docker update sql_server_crm --cpus="2" --memory="4g"
    Regular Updates:
        Periodically pull the latest image (docker pull mcr.microsoft.com/mssql/server:2022-latest) and recreate the container to apply security patches.

# Test and Prepare for CRM Integration

Test Connectivity: From Windows, use SQL Server Management Studio (SSMS) or sqlcmd:
```bash
sqlcmd -S localhost -U SA -P "My$ecureP@ssw0rd2025"
```
- Create a test database:

```sql 
CREATE DATABASE CRM_DB; 
GO
```

# CRM Integration

Use the connection string in your CRM app: 
```C#
Server=localhost,1433;Database=CRM_DB;User Id=crm_user;Password=CrM$ecure2025;Encrypt=True;TrustServerCertificate=True (self-signed cert requires TrustServerCertificate).
For production deployment, replace localhost with the server’s IP or hostname and secure the cert properly.
```

# Monitor Logs:
```bash
docker logs sql_server_crm
```


# Testing connection to DB:
```sh
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
curl https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/prod.list | sudo tee /etc/apt/sources.list.d/msprod.list
sudo apt update
sudo apt install -y mssql-tools18 unixodbc-dev
```

- sqlcmd -S localhost -U SA -P "My$ecureP@ssw0rd2025"

> If above fails use this to ignore SSL (for now)
- sqlcmd -S localhost -U SA -P "My$ecureP@ssw0rd2025" -N -C

- Now test running a command

```SQL
3> SELECT @@VERSION;
4> GO
```


# Start the container
- Check status
docker ps -a
- Start it
docker start sql_server_crm
- Checkif running
docker ps




--- 


# Connecting to SQL Server in C#

This guide shows how to connect to your SQL Server Docker container (`sql_server_crm`) from a C# application using `Microsoft.Data.SqlClient`.

## Prerequisites
- **NuGet Package**: Install `Microsoft.Data.SqlClient`.
  ```bash
  dotnet add package Microsoft.Data.SqlClient
  ```
- **Container Running**: Ensure `sql_server_crm` is started.
  ```bash
  docker start sql_server_crm
  ```

## Connection Details
- **Server**: `localhost,1433` (Docker maps port 1433)
- **User**: `SA`
- **Password**: `My$ecureP@ssw0rd2025`
- **Database**: `master` (or your CRM database)

## C# Code Example
```csharp
using Microsoft.Data.SqlClient;
using System;

class Program
{
    static void Main()
    {
        // Connection string
        string connectionString = "Server=localhost,1433;Database=master;User Id=SA;Password=My$ecureP@ssw0rd2025;Encrypt=True;TrustServerCertificate=True;";

        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Connected successfully!");

                // Query: Get SQL Server version
                string sql = "SELECT @@VERSION;";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    string version = (string)command.ExecuteScalar();
                    Console.WriteLine($"SQL Server Version: {version}");
                }
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"SQL Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
```

## Explanation
- **Connection String**:
  - `Encrypt=True`: Matches your SSL setup.
  - `TrustServerCertificate=True`: Bypasses self-signed cert (remove with a CA-signed cert in production).
- **Using Blocks**: Ensures resources are disposed of properly.
- **Query**: `@@VERSION` returns the SQL Server version as a basic test.

## Running the Code
1. Save as `Program.cs` in a .NET project.
2. Build and run:
   ```bash
   dotnet run
   ```
3. Expected output:
   ```
   Connected successfully!
   SQL Server Version: Microsoft SQL Server 2022 (RTM-CU12) - 16.0.XXXX.X ...
   ```

## For Your CRM
- Replace `Database=master` with your CRM database (e.g., `Database=CRM_DB`).
- Store the password securely (e.g., in `appsettings.json` or a secrets manager).

## Troubleshooting
- **Container Down?**: Check with `docker ps`.
- **Connection Fails?**: Verify port 1433 is mapped and the SA password is correct.
```

This MD file provides a concise, reusable reference for connecting to your DB in C#. Let me know if you’d like adjustments!


