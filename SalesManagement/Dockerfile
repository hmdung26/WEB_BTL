# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore dependencies (layer cache theo csproj)
COPY SalesManagement.csproj ./
RUN dotnet restore "SalesManagement.csproj"

# Copy toàn bộ source và publish
COPY . .
RUN dotnet publish "SalesManagement.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:80 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 80

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SalesManagement.dll"]
