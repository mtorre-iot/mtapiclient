# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
# Copy csproj and restore as distinct layers
COPY ["gaihcc2dataserver.csproj", "./"]
RUN dotnet restore "gaihcc2dataserver.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "gaihcc2dataserver.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN groupadd -g 1000 appuser && useradd -d /app -g 1000 -M -N -s /sbin/nologin -u 1000 appuser

WORKDIR /app
COPY --from=build --chown=appuser:appuser /app/publish .
COPY --chown=appuser:appuser appsettings.json /app

USER root:root
RUN install -o appuser -g appuser -d -m 0755 /temp

USER appuser:appuser
VOLUME ["/temp"]
# Expose port (adjust if needed)
EXPOSE 5212

# Start the app
ENTRYPOINT ["dotnet", "gaihcc2dataserver.dll", "--urls", "http://*:5212", "--environment=Development"]
#ENTRYPOINT [ "tail", "-f", "/dev/null" ]