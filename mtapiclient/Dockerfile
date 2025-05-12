# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
# Copy csproj and restore as distinct layers
COPY ["mtapiclient.csproj", "./"]
RUN dotnet restore "mtapiclient.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "mtapiclient.csproj" -c Release -o /app/publish /p:UseAppHost=false

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
EXPOSE 5202

# Start the app
ENTRYPOINT ["dotnet", "mtapiclient.dll", "--urls", "http://*:5202", "--environment=Development"]
#ENTRYPOINT [ "tail", "-f", "/dev/null" ]