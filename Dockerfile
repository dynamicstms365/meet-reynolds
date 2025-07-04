# Multi-stage build for security and optimization
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files for dependency resolution - Cache bust for fresh build
COPY src/CopilotAgent.sln ./
COPY src/CopilotAgent/CopilotAgent.csproj ./CopilotAgent/
COPY src/CopilotAgent.Tests/CopilotAgent.Tests.csproj ./CopilotAgent.Tests/
COPY src/Shared/Shared.csproj ./Shared/

# Restore dependencies using solution file
RUN dotnet restore CopilotAgent.sln

# Copy source and build the main project
COPY src/ ./
RUN dotnet build CopilotAgent/CopilotAgent.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish CopilotAgent/CopilotAgent.csproj -c Release -o /app/publish --no-restore

# Final runtime image - Reynolds: .NET 9.0 with Maximum Effort™
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install security updates and required packages
RUN apt-get update && apt-get upgrade -y \
    && apt-get install -y --no-install-recommends \
        curl \
        ca-certificates \
        tzdata \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user with specific UID/GID for security
RUN groupadd -r -g 1000 copilot && useradd --no-log-init -r -g copilot -u 1000 copilot

# Create necessary directories with proper permissions
RUN mkdir -p /app/logs /app/temp /app/data \
    && chown -R copilot:copilot /app

# Copy published application
COPY --from=publish --chown=copilot:copilot /app/publish .

# Set up security-hardened environment - Reynolds: Updated for SEQ integration
ENV ASPNETCORE_URLS=http://+:8000 \
    ASPNETCORE_ENVIRONMENT=Development \
    SEQ_SERVER_URL=http://seq:80 \
    SEQ_API_KEY= \
    REYNOLDS_MAXIMUM_EFFORT=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    COMPlus_EnableDiagnostics=0

# Azure OpenAI Integration Environment Variables
ENV AZURE_OPENAI_ENDPOINT="" \
    AZURE_OPENAI_API_KEY="" \
    AZURE_OPENAI_DEPLOYMENT_NAME="" \
    AZURE_OPENAI_API_VERSION="2024-02-15-preview" \
    AZURE_OPENAI_MAX_TOKENS=4096 \
    AZURE_OPENAI_TEMPERATURE=0.7 \
    AZURE_OPENAI_TIMEOUT=30 \
    AZURE_OPENAI_RETRY_COUNT=3 \
    AZURE_OPENAI_RATE_LIMIT_ENABLED=true

# Security and monitoring settings
ENV SECURITY_HEADERS_ENABLED=true \
    CORS_ENABLED=true \
    RATE_LIMITING_ENABLED=true \
    HEALTH_CHECK_ENABLED=true \
    METRICS_ENABLED=true \
    LOGGING_LEVEL=Information

# Switch to non-root user
USER copilot

# Expose ports - Reynolds: Updated for consistency
EXPOSE 8000
EXPOSE 8443

# Enhanced health check with Azure OpenAI validation - Reynolds: Updated port
HEALTHCHECK --interval=30s --timeout=15s --start-period=90s --retries=5 \
    CMD curl -f http://localhost:8000/health || exit 1

ENTRYPOINT ["dotnet", "CopilotAgent.dll"]