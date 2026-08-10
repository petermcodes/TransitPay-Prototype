# Stage 1: Build the application with Node.js 20
FROM node:20-alpine AS builder
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies (uses package-lock.json for deterministic builds)
RUN npm ci

# Copy source code
COPY . .

# Build the application
RUN npm run build

# Stage 2: Serve with serve package (production)
FROM node:20-alpine

# Install curl for healthcheck and serve globally
RUN apk add --no-cache curl && npm install -g serve

# Copy built files from builder stage
COPY --from=builder /app/dist /app/dist

# Set working directory
WORKDIR /app

# Expose port 80
EXPOSE 80

# Healthcheck
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost/ || exit 1

# Start serve
CMD ["serve", "-s", "dist", "-l", "80"]