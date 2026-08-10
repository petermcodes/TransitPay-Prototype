# Use the official .NET 10.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["TransitPay.API/TransitPay.API.csproj", "TransitPay.API/"]
RUN dotnet restore "TransitPay.API/TransitPay.API.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "TransitPay.API/TransitPay.API.csproj" -c Release -o "/app/publish" --no-restore

# Use the official .NET 10.0 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy the published app from the build stage
COPY --from=build /app/publish .

# Expose the port Railway expects
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "TransitPay.API.dll"]