#!/bin/bash

echo "Cleaning up existing microservice processes..."
# Use PowerShell to kill processes on specific ports
powershell -Command "
    \$ports = 7007, 7205, 7216, 7005, 7002
    foreach (\$port in \$ports) {
        \$connection = Get-NetTCPConnection -LocalPort \$port -State Listen -ErrorAction SilentlyContinue
        if (\$connection) {
            Write-Host \"Cleaning port \$port (PID: \$(\$connection.OwningProcess))\"
            Stop-Process -Id \$connection.OwningProcess -Force -ErrorAction SilentlyContinue
        }
    }
    Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue 2>\$null
"

echo "Starting all microservices..."



# Start User & Auth Service in the background
cd UserAndAuthorizationManagementMicroService
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls=http://localhost:7007 &
cd ..

# Start Sending Emails Service in the background
cd SendingEmailsMicroService
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls=http://localhost:7005 &
cd ..

# Start Payment Service in the background
cd PaymentMicroService
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls=http://localhost:7205 &
cd ..

# Start Reservations Service in the background
cd ReservationsMicroService
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls=http://localhost:7216 &
cd ..

# Start Room Service in the background
cd RoomMicroService
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls=http://localhost:7002 &
cd ..

# Wait 5 seconds for the services to start
sleep 5

echo "Opening services in browser..."
start http://localhost:7007/swagger/index.html
start http://localhost:7005/swagger/index.html
start http://localhost:7205/swagger/index.html
start http://localhost:7216/swagger/index.html
start http://localhost:7002/swagger/index.html

echo "All services started!"


# Keep the script running (optional)
echo "Press Ctrl+C to stop all services"
wait