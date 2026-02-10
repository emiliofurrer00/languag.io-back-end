# Multi-stage Dockerfile for building and running the API
# DOTNET_VERSION can be overridden at build time (defaults to 10.0)
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Copy everything and restore/publish the API project
COPY . .
RUN dotnet restore "src/Languag.io.Api/Languag.io.Api.csproj"
RUN dotnet publish "src/Languag.io.Api/Languag.io.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app

# Listen on port 80 inside the container
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

# Copy published app from build stage
COPY --from=build /app/publish .

# Do not hardcode connection strings here. Provide them at runtime using environment variables.
# ASP.NET Core configuration maps the environment variable name
# ConnectionStrings__DefaultConnection -> configuration["ConnectionStrings:DefaultConnection"]

ENTRYPOINT ["dotnet", "Languag.io.Api.dll"]
