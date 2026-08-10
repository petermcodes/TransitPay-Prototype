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

# Install serve globally
RUN npm install -g serve

# Copy built files from builder stage
COPY --from=builder /app/dist ./dist

# Set working directory
WORKDIR /app

# Expose the port Railway will assign (for documentation)
EXPOSE 80

# Start serve - use Railway's PORT environment variable
CMD ["sh", "-c", "serve -s dist -l ${PORT}"]