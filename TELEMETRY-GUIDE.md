# Guide : Télémétrie Aspire avec Services Conteneurisés

Ce guide explique comment configurer la télémétrie OpenTelemetry dans des services .NET conteneurisés pour qu'ils s'intègrent avec l'orchestration Aspire.

---

## Architecture

```
┌─────────────────────────────────────┐
│        Aspire AppHost               │
│   - Collecteur OTLP                 │
│   - Dashboard (localhost:18888)     │
└────────────┬────────────────────────┘
             │ Injecte automatiquement :
             │ - OTEL_EXPORTER_OTLP_ENDPOINT
             │ - OTEL_SERVICE_NAME
             │
    ┌────────┴────────┬───────────────┐
    │                 │               │
┌───▼──────┐   ┌─────▼────┐   ┌─────▼────┐
│ Gateway  │   │ Catalog  │   │ Orders   │
│Container │   │Container │   │Container │
│+ OTLP    │   │+ OTLP    │   │+ OTLP    │
└──────────┘   └──────────┘   └──────────┘
```

Aspire Dashboard affiche :
- **Traces** (requêtes HTTP, dépendances)
- **Metrics** (CPU, mémoire, requêtes/sec)
- **Logs** (logs structurés)

---

## Option 1 : Partager ServiceDefaults (Recommandé)

### Avantages
- ✅ Configuration centralisée
- ✅ Cohérence entre tous les services
- ✅ Mise à jour simplifiée

### Étapes

#### 1. Publier ServiceDefaults en package NuGet

**Dans le repo Aspire (ce repo) :**

```bash
cd Miccore.Clean.ServiceDefaults
dotnet pack -c Release -o ../packages

# Ou publier sur un registre privé
dotnet nuget push ../packages/Miccore.Clean.ServiceDefaults.1.0.0.nupkg \
    --source https://your-registry/nuget/v3/index.json \
    --api-key YOUR_API_KEY
```

#### 2. Référencer ServiceDefaults dans chaque service

**Dans CatalogService.csproj (autre repo) :**

```xml
<ItemGroup>
  <!-- Référencer votre package ServiceDefaults -->
  <PackageReference Include="Miccore.Clean.ServiceDefaults" Version="1.0.0" />
  
  <!-- Ou référence locale pour développement -->
  <!-- <ProjectReference Include="../../SampleAspire/Miccore.Clean.ServiceDefaults/Miccore.Clean.ServiceDefaults.csproj" /> -->
</ItemGroup>
```

#### 3. Utiliser dans Program.cs

**CatalogService/Program.cs :**

```csharp
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// ✅ Cette ligne configure automatiquement OpenTelemetry, Health Checks, Service Discovery
builder.AddServiceDefaults();

// Votre configuration spécifique
builder.Services.AddControllers();
builder.Services.AddDbContext<CatalogContext>();

var app = builder.Build();

// ✅ Cette ligne expose /health et /alive
app.MapDefaultEndpoints();

app.MapControllers();
app.Run();
```

#### 4. Configuration NuGet locale (développement)

**nuget.config dans le repo du service :**

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="local" value="../SampleAspire/packages" />
  </packageSources>
</configuration>
```

---

## Option 2 : Configuration Manuelle dans Chaque Service

Si vous ne voulez pas partager ServiceDefaults (services complètement indépendants).

### 1. Ajouter les packages NuGet

**Dans CatalogService.csproj :**

```xml
<ItemGroup>
  <!-- OpenTelemetry Core -->
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.9.0" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.9.0" />
  
  <!-- Instrumentation ASP.NET Core -->
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.9.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.9.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.9.0" />
  
  <!-- Service Discovery (optionnel) -->
  <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="9.1.0" />
  <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.1.0" />
</ItemGroup>
```

### 2. Configurer OpenTelemetry manuellement

**CatalogService/Program.cs :**

```csharp
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

// === OpenTelemetry Configuration ===

// Logging
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

// Metrics & Tracing
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(builder.Environment.ApplicationName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    });

// OTLP Exporter (Aspire injecte OTEL_EXPORTER_OTLP_ENDPOINT automatiquement)
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
if (!string.IsNullOrWhiteSpace(otlpEndpoint))
{
    builder.Services.AddOpenTelemetry().UseOtlpExporter();
}

// === Health Checks ===
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

// === Service Discovery (optionnel) ===
builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler();
    http.AddServiceDiscovery();
});

// Votre configuration
builder.Services.AddControllers();

var app = builder.Build();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

app.MapControllers();
app.Run();
```

---

## Configuration dans AppHost

Peu importe l'option choisie, configurez vos conteneurs ainsi :

**Miccore.Clean.AppHost/Program.cs :**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Service conteneurisé avec télémétrie
var catalogService = builder.AddContainer("catalog", "catalog-api", "latest")
    .WithHttpEndpoint(port: 5001, targetPort: 8080, name: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);
    // ✅ Aspire injecte automatiquement OTEL_EXPORTER_OTLP_ENDPOINT

var ordersService = builder.AddContainer("orders", "orders-api", "latest")
    .WithHttpEndpoint(port: 5002, targetPort: 8080, name: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);

// Gateway
builder.AddProject<Projects.Miccore_Clean_Gateway>("Gateway")
    .WithReference(catalogService)
    .WithReference(ordersService);

builder.Build().Run();
```

---

## Vérification

### 1. Lancer Aspire

```bash
cd Miccore.Clean.AppHost
dotnet run
```

### 2. Accéder au Dashboard

Ouvrir dans le navigateur : **http://localhost:18888**

Vous devriez voir :

#### **Onglet Resources**
- ✅ Gateway (running)
- ✅ catalog (running)
- ✅ orders (running)

#### **Onglet Traces**
- ✅ Requêtes HTTP entrantes sur Gateway
- ✅ Appels sortants Gateway → Catalog
- ✅ Traçage distribué avec correlation IDs

#### **Onglet Metrics**
- ✅ `http.server.request.duration` (latence)
- ✅ `http.server.active_requests` (requêtes actives)
- ✅ `process.cpu.usage` (CPU)
- ✅ `process.memory.working_set` (mémoire)

#### **Onglet Logs**
- ✅ Logs structurés de tous les services
- ✅ Corrélés avec les traces (même Request-Id)

---

## Exemple Complet : Service Catalog avec Télémétrie

### Structure du repo CatalogService

```
CatalogService/
├── src/
│   ├── CatalogService.csproj
│   ├── Program.cs
│   ├── Controllers/
│   │   └── ProductsController.cs
│   └── appsettings.json
├── Dockerfile
└── nuget.config (si utilise ServiceDefaults local)
```

### CatalogService.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- Option 1 : Utiliser ServiceDefaults partagé -->
    <PackageReference Include="Miccore.Clean.ServiceDefaults" Version="1.0.0" />
    
    <!-- Votre code métier -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
  </ItemGroup>
</Project>
```

### Program.cs

```csharp
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configure automatiquement OpenTelemetry + Health Checks + Service Discovery
builder.AddServiceDefaults();

// Configuration métier
builder.Services.AddControllers();
builder.Services.AddDbContext<CatalogContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

var app = builder.Build();

// ✅ Expose /health et /alive
app.MapDefaultEndpoints();

app.MapControllers();
app.Run();
```

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/CatalogService.csproj", "./"]
COPY ["nuget.config", "./"]  # Si utilise package local
RUN dotnet restore
COPY src/ .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CatalogService.dll"]
```

### Build et Run

```bash
# Dans le repo CatalogService
dotnet publish -c Release
docker build -t catalog-api:latest .

# Dans le repo Aspire
cd Miccore.Clean.AppHost
dotnet run
```

Le Dashboard Aspire affichera automatiquement toutes les traces, metrics et logs ! 🎉

---

## Troubleshooting

### ❌ Pas de télémétrie dans le Dashboard

**Vérifier :**
1. Le service a bien les packages OpenTelemetry installés
2. `builder.AddServiceDefaults()` ou configuration manuelle est présente
3. La variable `OTEL_EXPORTER_OTLP_ENDPOINT` est injectée (visible dans les logs)

**Debug :**
```bash
# Vérifier les variables d'environnement dans le conteneur
docker exec -it <container_id> env | grep OTEL
```

### ❌ Service Discovery ne fonctionne pas

**Vérifier :**
1. `.WithReference()` est utilisé dans l'AppHost
2. Les services appellent `builder.Services.AddServiceDiscovery()`
3. HttpClient utilise les noms de service Aspire (ex: `http://catalog`)

### ❌ Health Checks non visibles

**Vérifier :**
1. `app.MapDefaultEndpoints()` est appelé
2. Les endpoints `/health` et `/alive` répondent (test direct)

```bash
curl http://localhost:5001/health
```

---

## Résumé des Avantages

| Composant | Ce que vous obtenez |
|-----------|---------------------|
| **Traces** | Traçage distribué des requêtes à travers tous les services |
| **Metrics** | Latence, throughput, CPU, mémoire en temps réel |
| **Logs** | Logs structurés corrélés avec les traces |
| **Service Discovery** | Résolution automatique des noms de services |
| **Health Checks** | Monitoring de santé de chaque service |
| **Dashboard** | Interface unique pour observer tout le système |

La télémétrie Aspire fonctionne **exactement pareil** que vos services soient dans le même repo ou dans des conteneurs séparés ! 🚀
