# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy solution and project files
COPY *.sln .
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
