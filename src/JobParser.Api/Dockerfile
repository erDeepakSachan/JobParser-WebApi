# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/JobParser.Api/JobParser.Api.csproj", "src/JobParser.Api/"]
COPY ["src/JobParser.Application/JobParser.Application.csproj", "src/JobParser.Application/"]
COPY ["src/JobParser.Domain/JobParser.Domain.csproj", "src/JobParser.Domain/"]
COPY ["src/JobParser.Infrastructure/JobParser.Infrastructure.csproj", "src/JobParser.Infrastructure/"]
RUN dotnet restore "./src/JobParser.Api/JobParser.Api.csproj"
COPY . .
WORKDIR "/src/src/JobParser.Api"
RUN dotnet build "./JobParser.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./JobParser.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobParser.Api.dll"]