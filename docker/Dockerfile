FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy solution and projects
COPY ["EasyLog/EasyLog.csproj", "EasyLog/"]
COPY ["EasySave.Models/EasySave.Models.csproj", "EasySave.Models/"]

# Restore
RUN dotnet restore "EasyLog/EasyLog.csproj"

# Copy remaining source
COPY . .

# Build and publish
RUN dotnet build "EasyLog/EasyLog.csproj" -c Release -o /app/build
RUN dotnet publish "EasyLog/EasyLog.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Create volume for logs
RUN mkdir -p /app/logs
VOLUME ["/app/logs"]

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "EasyLog.dll"]
