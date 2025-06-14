FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Copy the pre-built application
COPY ["publish/bff", "./"]

ENTRYPOINT ["dotnet", "McpServer.Bff.dll"]