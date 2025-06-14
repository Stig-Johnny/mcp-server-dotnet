FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install Node.js 20 LTS using Node.js official distribution
RUN apt-get update && apt-get install -y ca-certificates curl gnupg \
    && mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
    && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_20.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list \
    && apt-get update && apt-get install -y nodejs \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy project files for restore
COPY ["src/Presentation/McpServer.Bff/McpServer.Bff.csproj", "src/Presentation/McpServer.Bff/"]
COPY ["src/Application/McpServer.Application/McpServer.Application.csproj", "src/Application/McpServer.Application/"]
COPY ["src/Domain/McpServer.Domain/McpServer.Domain.csproj", "src/Domain/McpServer.Domain/"]
COPY ["src/Infrastructure/McpServer.Infrastructure/McpServer.Infrastructure.csproj", "src/Infrastructure/McpServer.Infrastructure/"]

# Restore .NET dependencies
RUN dotnet restore "src/Presentation/McpServer.Bff/McpServer.Bff.csproj"

# Copy frontend package files for npm install
COPY ["src/Presentation/McpServer.Bff/frontend/package*.json", "src/Presentation/McpServer.Bff/frontend/"]

# Install npm dependencies
WORKDIR "/src/src/Presentation/McpServer.Bff/frontend"
RUN npm ci --production=false

# Copy all source code
WORKDIR /src
COPY . .

# Build React app
WORKDIR "/src/src/Presentation/McpServer.Bff/frontend"
RUN npm run build

# Build .NET application
WORKDIR "/src/src/Presentation/McpServer.Bff"
RUN dotnet build "McpServer.Bff.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "McpServer.Bff.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "McpServer.Bff.dll"]