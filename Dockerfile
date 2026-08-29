# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy solution and project files
COPY *.slnx .
COPY src/NotesAndFileBackend.Api/*.csproj src/NotesAndFileBackend.Api/
COPY src/NotesAndFileBackend.Application/*.csproj src/NotesAndFileBackend.Application/
COPY src/NotesAndFileBackend.Contracts/*.csproj src/NotesAndFileBackend.Contracts/
COPY src/NotesAndFileBackend.Domain/*.csproj src/NotesAndFileBackend.Domain/
COPY src/NotesAndFileBackend.Infrastructure/*.csproj src/NotesAndFileBackend.Infrastructure/
RUN dotnet restore

# Copy all source code and build
COPY src/ src/
WORKDIR /source/src/NotesAndFileBackend.Api
RUN dotnet publish -c Release -o /app

# Serve stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "NotesAndFileBackend.Api.dll"]
