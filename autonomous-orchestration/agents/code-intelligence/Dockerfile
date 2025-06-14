# Simplified Code Intelligence Agent for Testing
FROM node:18-alpine

# Install basic dependencies
RUN apk add --no-cache \
    curl \
    git \
    bash \
    jq \
    wget \
    python3 \
    py3-pip

# Create non-root user
RUN addgroup -g 1001 codeagent && \
    adduser -D -u 1001 -G codeagent codeagent

WORKDIR /app

# Copy package files and install dependencies
COPY package*.json ./
RUN npm ci --only=production && npm cache clean --force

# Copy application code
COPY . .

# Create necessary directories
RUN mkdir -p /app/{knowledge,context,memory,workspace,cache,tools,secrets,analysis,reports} && \
    chown -R codeagent:codeagent /app

USER codeagent

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

EXPOSE 8080

ENV NODE_ENV=production
ENV AGENT_TYPE=code-intelligence
ENV LOG_LEVEL=info
ENV PORT=8080

CMD ["node", "CodeAgent.js"]