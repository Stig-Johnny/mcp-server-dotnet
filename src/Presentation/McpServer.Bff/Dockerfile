FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install Node.js for React build
RUN curl -fsSL https://deb.nodesource.com/setup_18.x | bash - \
    && apt-get install -y nodejs

COPY ["src/Presentation/McpServer.Bff/McpServer.Bff.csproj", "src/Presentation/McpServer.Bff/"]
RUN dotnet restore "src/Presentation/McpServer.Bff/McpServer.Bff.csproj"

COPY . .
WORKDIR "/src/src/Presentation/McpServer.Bff"

# Install npm dependencies and build React app
RUN cd frontend && npm ci && npm run build

RUN dotnet build "McpServer.Bff.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "McpServer.Bff.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "McpServer.Bff.dll"]