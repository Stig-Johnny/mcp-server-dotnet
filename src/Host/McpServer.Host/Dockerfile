FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 8080

# Copy the pre-built application
COPY ["publish/host", "./"]

ENTRYPOINT ["dotnet", "McpServer.Host.dll"]