FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Presentation/McpServer.Bff/McpServer.Bff.csproj", "src/Presentation/McpServer.Bff/"]
RUN dotnet restore "src/Presentation/McpServer.Bff/McpServer.Bff.csproj"
COPY . .
WORKDIR "/src/src/Presentation/McpServer.Bff"
RUN dotnet build "McpServer.Bff.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "McpServer.Bff.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "McpServer.Bff.dll"]