FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

# Create non-root user for security
RUN groupadd -r copilot && useradd --no-log-init -r -g copilot copilot

# Copy published application
COPY ./publish .

# Set ownership and permissions
RUN chown -R copilot:copilot /app
USER copilot

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/api/github/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CopilotAgent.dll"]