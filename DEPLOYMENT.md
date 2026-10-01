# Deployment Guide

This guide covers two methods to deploy the LMS application:

1.  **Docker (Recommended)**: Containerized deployment for easy management.
2.  **Manual Linux VPS**: Traditional setup on Ubuntu with Nginx.

---

## Prerequisites

- **Domain Name** (optional but recommended)
- **VPS/Server**: Ubuntu 22.04 or later (e.g., DigitalOcean, AWS EC2, Linode).
- **Docker** (for Method 1) OR **.NET 8 SDK & MySQL** (for Method 2).

---

## Method 1: Docker (Recommended)

This method wraps the application and database into containers, ensuring they run the same way everywhere.

### 1. Create `Dockerfile`

Create a file named `Dockerfile` in the project root:

```dockerfile
# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LMS.csproj", "./"]
RUN dotnet restore "LMS.csproj"
COPY . .
RUN dotnet publish "LMS.csproj" -c Release -o /app/publish

# Serve Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "LMS.dll"]
```

### 2. Create `docker-compose.yml`

Create a file named `docker-compose.yml` in the project root:

```yaml
version: "3.8"

services:
  lms-app:
    build: .
    ports:
      - "80:8080"
    environment:
      - ConnectionStrings__DefaultConnection=server=lms-db;port=3306;database=lms-db;user=root;password=your_secure_password;
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      - lms-db
    restart: always

  lms-db:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: your_secure_password
      MYSQL_DATABASE: lms-db
    volumes:
      - db_data:/var/lib/mysql
    restart: always

volumes:
  db_data:
```

### 3. Run Deployment

Run the following command on your server:

```bash
docker-compose up -d --build
```

Your app will be live on port 80.

---

## Method 2: Manual Linux VPS (Ubuntu + Nginx)

This method runs the app directly on the server OS behind an Nginx reverse proxy.

### 1. Install Dependencies

```bash
# Update packages
sudo apt update && sudo apt upgrade -y

# Install .NET 8 Runtime
sudo apt-get install -y dotnet-sdk-8.0

# Install MySQL Server
sudo apt install mysql-server
sudo mysql_secure_installation
```

### 2. Publish the App

Run this locally and copy files to server, or clone and build on server:

```bash
git clone https://github.com/sameer9860/LMS.git
cd LMS
dotnet publish -c Release -o /var/www/lms
```

### 3. Configure Nginx

Install Nginx:

```bash
sudo apt install nginx
```

Create a new config file `/etc/nginx/sites-available/lms`:

```nginx
server {
    listen 80;
    server_name your_domain.com_or_IP;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Enable the site:

```bash
sudo ln -s /etc/nginx/sites-available/lms /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

### 4. Create Systemd Service

Create `/etc/systemd/system/lms.service`:

```ini
[Unit]
Description=LMS .NET Web App

[Service]
WorkingDirectory=/var/www/lms
ExecStart=/usr/bin/dotnet /var/www/lms/LMS.dll
Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=dotnet-example
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ConnectionStrings__DefaultConnection=server=localhost;port=3306;database=lms-db;user=root;password=your_db_password;

[Install]
WantedBy=multi-user.target
```

Start the service:

```bash
sudo systemctl enable lms.service
sudo systemctl start lms.service
```

Your app should now be running!
