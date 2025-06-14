FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Presentation/McpServer.Api/McpServer.Api.csproj", "src/Presentation/McpServer.Api/"]
COPY ["src/Application/McpServer.Application/McpServer.Application.csproj", "src/Application/McpServer.Application/"]
COPY ["src/Domain/McpServer.Domain/McpServer.Domain.csproj", "src/Domain/McpServer.Domain/"]
COPY ["src/Infrastructure/McpServer.Infrastructure/McpServer.Infrastructure.csproj", "src/Infrastructure/McpServer.Infrastructure/"]
RUN dotnet restore "src/Presentation/McpServer.Api/McpServer.Api.csproj"
COPY . .
WORKDIR "/src/src/Presentation/McpServer.Api"
RUN dotnet build "McpServer.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "McpServer.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "McpServer.Api.dll"]