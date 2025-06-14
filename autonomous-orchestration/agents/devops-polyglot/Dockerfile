# Simplified DevOps Agent for Testing
FROM node:18-alpine

# Install basic dependencies
RUN apk add --no-cache \
    curl \
    git \
    bash \
    jq \
    wget \
    docker-cli

# Create non-root user
RUN addgroup -g 1001 devops && \
    adduser -D -u 1001 -G devops devops

WORKDIR /app

# Copy package files and install dependencies
COPY package*.json ./
RUN npm ci --only=production && npm cache clean --force

# Copy application code
COPY . .

# Create necessary directories
RUN mkdir -p /app/{knowledge,context,memory,workspace,cache,tools,secrets} && \
    chown -R devops:devops /app

USER devops

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

EXPOSE 8080

ENV NODE_ENV=production
ENV AGENT_TYPE=devops-polyglot
ENV LOG_LEVEL=info
ENV PORT=8080

CMD ["node", "DevOpsAgent.js"]