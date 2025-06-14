FROM node:18-alpine AS base

# Install system dependencies
RUN apk add --no-cache \
    curl \
    git \
    bash \
    jq \
    python3 \
    py3-pip

# Create non-root user
RUN addgroup -g 1001 reynolds && \
    adduser -D -s /bin/bash -u 1001 -G reynolds reynolds

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci --only=production && npm cache clean --force

# Copy application code
COPY . .

# Set ownership
RUN chown -R reynolds:reynolds /app

# Switch to non-root user
USER reynolds

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Expose ports
EXPOSE 8080 8443

# Start the Reynolds orchestrator
CMD ["node", "src/index.js"]