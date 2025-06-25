# -----------------------------
# STAGE 1: Runtime base (ASP.NET)
# -----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# -----------------------------
# STAGE 2: Build
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copier les fichiers .csproj
COPY Sourcing.Messaging/Sourcing.Messaging.API.csproj Sourcing.Messaging/
COPY Sourcing.Messaging.BLL/Sourcing.Messaging.BLL.csproj Sourcing.Messaging.BLL/
COPY Sourcing.Messaging.DAL/Sourcing.Messaging.DAL.csproj Sourcing.Messaging.DAL/

# Restore
RUN dotnet restore Sourcing.Messaging/Sourcing.Messaging.API.csproj

# Copier tout le reste
COPY . .

# Positionner dans le bon dossier
WORKDIR /src/Sourcing.Messaging

# Build
RUN dotnet build Sourcing.Messaging.API.csproj -c $BUILD_CONFIGURATION -o /app/build

# -----------------------------
# STAGE 3: Publish
# -----------------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish Sourcing.Messaging.API.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# -----------------------------
# STAGE 4: Runtime
# -----------------------------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Sourcing.Messaging.API.dll"]
