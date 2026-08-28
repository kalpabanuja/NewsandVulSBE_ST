# Notes & File Backend - Ubuntu VPS Installation Guide

This guide will walk you through deploying the `NotesAndFileBackend` service to a new Ubuntu VPS using Docker and Docker Compose. This deployment assumes you want to run PostgreSQL, MinIO (Object Storage), and the .NET Backend API containerized on the same server, separate from your other existing services.

## Prerequisites

1. **Ubuntu VPS**: A clean Ubuntu 20.04 or 22.04 installation.
2. **SSH Access**: You should be logged into your VPS via SSH as a non-root user with `sudo` privileges.
3. **Domain Name (Optional but Recommended)**: A domain pointing to your VPS IP for secure HTTPS access.

---

## Step 1: Install Docker & Docker Compose

Run the following commands to install the latest versions of Docker and Docker Compose:

```bash
# Update package lists
sudo apt update && sudo apt upgrade -y

# Install dependencies
sudo apt install -y apt-transport-https ca-certificates curl software-properties-common

# Add Docker's official GPG key
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg

# Add Docker repository
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Install Docker
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Allow your user to run Docker commands without sudo
sudo usermod -aG docker ${USER}
```
*(You may need to log out and log back in for the group change to take effect).*

---

## Step 2: Prepare the Application Files

On your VPS, create a directory for the backend service:

```bash
mkdir -p ~/apps/NotesAndFileBackend
cd ~/apps/NotesAndFileBackend
```

You need to transfer your project files from your local machine to this directory. You can use `scp`, `rsync`, or Git if your code is in a repository.

Make sure the following files and folders are present on the VPS:
- `src/` (Containing your .NET source code)
- `NotesAndFileBackend.slnx`
- `docker-compose.yml` (We will create a specific production version of this next)

---

## Step 3: Create the Production `docker-compose.yml`

Create a new file named `docker-compose.yml` in the `~/apps/NotesAndFileBackend` directory:

```yaml
version: '3.8'

services:
  api:
    build: 
      context: .
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=backend_db;Username=prod_user;Password=SuperSecurePassword!
      - JwtSettings__Secret=YOUR_VERY_SECURE_LONG_RANDOM_SECRET_KEY_HERE_12345!
      - Storage__ServiceUrl=http://minio:9000
      - Storage__AccessKey=admin
      - Storage__SecretKey=SuperSecureMinioPassword!
    ports:
      - "5001:8080" # Maps the container's internal port to port 5001 on the host
    depends_on:
      - postgres
      - minio
    restart: unless-stopped

  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_USER: prod_user
      POSTGRES_PASSWORD: SuperSecurePassword!
      POSTGRES_DB: backend_db
    # We do NOT expose ports here to keep the DB internal and prevent conflicts with your other Postgres instance
    volumes:
      - postgres_data_prod:/var/lib/postgresql/data
    restart: unless-stopped

  minio:
    image: minio/minio:latest
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: admin
      MINIO_ROOT_PASSWORD: SuperSecureMinioPassword!
    ports:
      - "9000:9000"
      - "9001:9001" # MinIO Admin Console
    volumes:
      - minio_data_prod:/data
    restart: unless-stopped

volumes:
  postgres_data_prod:
  minio_data_prod:
```

> **Note:** Change the passwords and JWT secret in the file above to secure random strings before running!

---

## Step 4: Create the Dockerfile

In the root of your project (`~/apps/NotesAndFileBackend`), create a `Dockerfile`:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy solution and project files
COPY *.slnx .
COPY src/NotesAndFileBackend.Api/*.csproj src/NotesAndFileBackend.Api/
COPY src/NotesAndFileBackend.Core/*.csproj src/NotesAndFileBackend.Core/
COPY src/NotesAndFileBackend.Infrastructure/*.csproj src/NotesAndFileBackend.Infrastructure/
RUN dotnet restore

# Copy all source code and build
COPY src/ src/
WORKDIR /source/src/NotesAndFileBackend.Api
RUN dotnet publish -c Release -o /app

# Serve stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "NotesAndFileBackend.Api.dll"]
```

---

## Step 5: Start the Services

Run the following command to build the API image and start all services in the background:

```bash
sudo docker compose up -d --build
```

To verify everything is running:
```bash
sudo docker compose logs -f api
```
*Look for a log line indicating "DEFAULT ADMIN USER CREATED" along with the generated secure password. **Copy this password!***

---

## Step 6: Setup Nginx Reverse Proxy (Optional but Recommended)

To expose your API securely on port 80/443 without conflicting with other apps, use Nginx.

```bash
sudo apt install -y nginx
```

Create a new Nginx configuration:
```bash
sudo nano /etc/nginx/sites-available/notes-api
```

Add this configuration (replace `api.yourdomain.com` with your actual domain):
```nginx
server {
    listen 80;
    server_name api.yourdomain.com;

    location / {
        proxy_pass http://localhost:5001;
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

Enable the site and restart Nginx:
```bash
sudo ln -s /etc/nginx/sites-available/notes-api /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

### Access the Admin Dashboard
You can now access your built-in dashboard by navigating to:
`http://api.yourdomain.com/` (or `http://YOUR_VPS_IP:5001/`).

Sign in using `admin@notesandfile.local` and the password you copied from the logs in Step 5.
