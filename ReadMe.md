# Guide : Orchestrer des Services Multi-Repos avec .NET Aspire

Ce guide explique comment ajouter des services provenant de différents dépôts Git à l'orchestration Aspire.

## Option 1 : Conteneurs Docker (✅ Recommandé)

### Avantages
- ✅ Indépendance totale entre repos
- ✅ Chaque équipe peut déployer indépendamment
- ✅ Pas de dépendances de compilation
- ✅ CI/CD simple
- ✅ Fonctionne en local et en production

### Configuration

#### 1. Créer un Dockerfile dans chaque service

```dockerfile
# Dans votre repo CatalogService
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["CatalogService.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CatalogService.dll"]
```

#### 2. Builder et publier l'image

```bash
cd /path/to/CatalogService
docker build -t catalog-api:latest .
# Ou publier sur un registre
docker tag catalog-api:latest your-registry.azurecr.io/catalog-api:latest
docker push your-registry.azurecr.io/catalog-api:latest
```

#### 3. Référencer dans AppHost

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Service distant en conteneur
var catalogService = builder.AddContainer("catalog", "catalog-api", "latest")
    .WithHttpEndpoint(port: 5001, targetPort: 8080, name: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithEnvironment("ConnectionStrings__Database", "your-connection-string");

// Gateway local
builder.AddProject<Projects.Miccore_Clean_Gateway>("Gateway")
    .WithReference(catalogService);
```

#### 4. Configurer dans Gateway

**appsettings.json :**
```json
{
  "Services": {
    "catalog": {
      "DownstreamPath": "http://catalog"
    }
  }
}
```

**ocelot.json :**
```json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/{everything}",
      "UpstreamPathTemplate": "/catalog/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
      "ServiceName": "catalog"
    }
  ]
}
```

---

## Option 2 : Projets Externes (développement local)

### Avantages
- ✅ Débogage direct dans Visual Studio
- ✅ Hot reload fonctionne
- ✅ Pas besoin de Docker

### Inconvénients
- ❌ Nécessite checkout local de tous les repos
- ❌ Structure de dossiers rigide
- ❌ Dépendances de compilation

### Configuration

#### 1. Structure de dossiers

```
/Projects/
├── Aspire/                    # Ce repo
│   └── Miccore.Clean.AppHost/
├── CatalogService/            # Repo séparé
│   └── src/
│       └── CatalogService.csproj
└── OrdersService/             # Repo séparé
    └── src/
        └── OrdersService.csproj
```

#### 2. Référencer dans AppHost

```csharp
var catalogService = builder.AddProject("catalog", 
    "../../CatalogService/src/CatalogService.csproj");

var ordersService = builder.AddProject("orders",
    "../../OrdersService/src/OrdersService.csproj");

builder.AddProject<Projects.Miccore_Clean_Gateway>("Gateway")
    .WithReference(catalogService)
    .WithReference(ordersService);
```

---

## Option 3 : Binaires Publiés

### Avantages
- ✅ Pas besoin de Docker
- ✅ Pas de dépendances de compilation

### Inconvénients
- ❌ Nécessite publication manuelle
- ❌ Pas de débogage direct
- ❌ Pas de hot reload

### Configuration

#### 1. Publier les services

```bash
cd /path/to/CatalogService
dotnet publish -c Release -o ../publish/catalog
```

#### 2. Référencer dans AppHost

```csharp
var catalogService = builder.AddExecutable("catalog",
    "../../publish/catalog/CatalogService",
    "../../publish/catalog")
    .WithHttpEndpoint(port: 5001)
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:5001");
```

---

## Option 4 : Docker Compose (Legacy)

Pour des projets existants utilisant déjà Docker Compose :

```csharp
builder.AddDockerCompose("external-services", "../docker-compose.yml");
```

---

## Recommandations par Scénario

| Scénario | Solution Recommandée |
|----------|---------------------|
| **Production** | Conteneurs Docker (Option 1) |
| **Développement local (1 développeur)** | Projets Externes (Option 2) |
| **Équipes distribuées** | Conteneurs Docker (Option 1) |
| **CI/CD** | Conteneurs Docker (Option 1) |
| **Débogage intensif** | Projets Externes (Option 2) |

---

## Configuration Gateway pour Services Externes

Peu importe l'option choisie, la configuration de la Gateway reste identique :

### 1. appsettings.json
```json
{
  "Services": {
    "catalog": { "DownstreamPath": "http://catalog" },
    "orders": { "DownstreamPath": "http://orders" },
    "identity": { "DownstreamPath": "http://identity" }
  }
}
```

### 2. ocelot.json
```json
{
  "Routes": [
    {
      "ServiceName": "catalog",
      "UpstreamPathTemplate": "/catalog/{everything}",
      "DownstreamPathTemplate": "/api/{everything}"
    }
  ],
  "GlobalConfiguration": {
    "ServiceDiscoveryProvider": {
      "Type": "AppConfiguration"
    }
  }
}
```

### 3. AppHost
```csharp
builder.AddProject<Projects.Miccore_Clean_Gateway>("Gateway")
    .WithReference(catalogService)
    .WithReference(ordersService)
    .WithReference(identityService);
```

---

## Exemple Complet : Service Catalog en Conteneur

### 1. Dans le repo CatalogService

**Dockerfile :**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 8080
COPY ./publish .
ENTRYPOINT ["dotnet", "CatalogService.dll"]
```

**Build :**
```bash
dotnet publish -c Release -o ./publish
docker build -t catalog-api:dev .
```

### 2. Dans ce repo (AppHost)

**Program.cs :**
```csharp
var catalogService = builder.AddContainer("catalog", "catalog-api", "dev")
    .WithHttpEndpoint(port: 5001, targetPort: 8080, name: "http");

builder.AddProject<Projects.Miccore_Clean_Gateway>("Gateway")
    .WithReference(catalogService);
```

**appsettings.json :**
```json
{
  "Services": {
    "catalog": {
      "DownstreamPath": "http://catalog"
    }
  }
}
```

**ocelot.json :**
```json
{
  "Routes": [{
    "ServiceName": "catalog",
    "UpstreamPathTemplate": "/catalog/{everything}",
    "DownstreamPathTemplate": "/api/{everything}",
    "UpstreamHttpMethod": ["Get", "Post", "Put", "Delete"]
  }]
}
```

### 3. Lancer l'orchestration

```bash
cd Miccore.Clean.AppHost
dotnet run
```

Aspire va :
1. Démarrer le conteneur `catalog-api:dev` sur le port 5001
2. Démarrer la Gateway
3. Configurer le service discovery pour résoudre `http://catalog` → `http://localhost:5001`
4. La Gateway pourra router `/catalog/*` vers le service Catalog

---

## Troubleshooting

### Le service ne se connecte pas
- Vérifiez que le nom du service correspond partout (`catalog` dans cet exemple)
- Vérifiez les ports exposés : `WithHttpEndpoint(targetPort: 8080)`
- Vérifiez que `ASPNETCORE_URLS` est configuré dans le conteneur

### Service discovery ne fonctionne pas
- Assurez-vous d'utiliser `.WithReference()` dans le Gateway
- Vérifiez la section `Services` dans `appsettings.json`
- Le nom doit correspondre à celui utilisé dans `AddContainer("nom", ...)`

### Hot reload ne fonctionne pas (conteneurs)
- Normal, utilisez Option 2 (projets externes) pour le débogage intensif
- Ou configurez un volume Docker pour le hot reload
