var builder = WebApplication.CreateBuilder(args);

// Load configuration files with environment-specific overrides
builder.Configuration.AddGatewayConfiguration(builder.Environment);

// Add Aspire service defaults (OpenTelemetry, Health Checks, Service Discovery)
builder.AddServiceDefaults();
builder.AddGatewayDefaults();

// Add Memory Cache (required by Ocelot AppConfiguration provider)
builder.Services.AddMemoryCache();

// Add Ocelot with AppConfiguration provider for dynamic service discovery
builder.Services.AddOcelotServices();

// Configure Swagger for Ocelot aggregation
builder.Services.AddSwaggerServices(builder.Configuration);

// Configure CORS with Options Pattern
builder.Services.AddCorsPolicy(builder.Environment, builder.Configuration);

var app = builder.Build();

// Map Aspire health check endpoints (/health, /alive)
app.MapDefaultEndpoints();

// Enable CORS
app.UseCorsPolicy();

// Configure Swagger UI
app.UseSwaggerServices();

app.UseHttpsRedirection();

// Ocelot middleware (async pattern)
await app.UseOcelotMiddleware();

app.Run();