# Use the official .NET runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
# NOTE: This may fail in GitHub Actions due to firewall restrictions blocking NuGet.org
# Use the deploy-azure-container.yml workflow approach for production deployments
COPY ["src/CopilotAgent/CopilotAgent.csproj", "src/CopilotAgent/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
RUN dotnet restore "src/CopilotAgent/CopilotAgent.csproj"

# Copy all source code and build
COPY . .
WORKDIR "/src/src/CopilotAgent"
RUN dotnet build "CopilotAgent.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "CopilotAgent.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage - runtime image
FROM base AS final
WORKDIR /app

# Create non-root user for security
RUN groupadd -r copilot && useradd --no-log-init -r -g copilot copilot

# Copy published application
COPY --from=publish /app/publish .

# Set ownership and permissions
RUN chown -R copilot:copilot /app
USER copilot

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost/api/github/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CopilotAgent.dll"]