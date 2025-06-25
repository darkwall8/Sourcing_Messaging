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

#  Copier uniquement les fichiers .csproj (restauration rapide et stable)
COPY ["Sourcing.Messaging/Sourcing.Messaging.API.csproj", "Sourcing.Messaging/"]
COPY ["Sourcing.Messaging.BLL/Sourcing.Messaging.BLL.csproj", "Sourcing.Messaging.BLL/"]
COPY ["Sourcing.Messaging.DAL/Sourcing.Messaging.DAL.csproj", "Sourcing.Messaging.DAL/"]

#  Restore des dépendances
RUN dotnet restore "Sourcing.Messaging/Sourcing.Messaging.API.csproj"

#  Copier le reste des fichiers source
COPY . .

#  Se positionner dans le dossier du projet principal
WORKDIR "/src/Sourcing.Messaging"

#  Build de l’application
RUN dotnet build "Sourcing.Messaging.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# -----------------------------
# STAGE 3: Publish
# -----------------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Sourcing.Messaging.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# -----------------------------
# STAGE 4: Final image (production-ready)
# -----------------------------
FROM base AS final
WORKDIR /app

#  Copier les fichiers publiés depuis l'étape précédente
COPY --from=publish /app/publish .

#  Définir l'environnement comme "Production" pour désactiver Swagger
ENV ASPNETCORE_ENVIRONMENT=Production

#  Point d’entrée de l’application
ENTRYPOINT ["dotnet", "Sourcing.Messaging.API.dll"]
