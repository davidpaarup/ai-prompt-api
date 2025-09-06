using AiPromptApi.Config;
using Grafana.OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using AiPromptApi.Services;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<GraphClientFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IKernelService, KernelService>();

var semanticKernelSettings = builder.Configuration.GetRequiredSection<SemanticKernelSettings>("SemanticKernel");
builder.Services.AddSingleton(semanticKernelSettings);

var azureApplicationSettings = builder.Configuration
        .GetRequiredSection<AzureApplicationSettings>("AzureApplication");

builder.Services.AddSingleton(azureApplicationSettings);

var issuer = builder.Configuration.GetRequiredValue("Issuer");

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = issuer,
            ValidAudience = issuer,
            
            IssuerSigningKeyResolver = (_, _, kid, _) =>
            {
                var jwksUrl = $"{issuer}/api/auth/jwks";
                var httpClient = new HttpClient();
                var response = httpClient.GetStringAsync(jwksUrl).GetAwaiter().GetResult();
                var jwks = new JsonWebKeySet(response);
                return jwks.Keys.Where(k => k.Kid == kid);
            }
        };
        
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://ai-prompt-bice.vercel.app")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

const string serviceName = "ai-prompt-api";

builder.Logging.AddOpenTelemetry(options =>
{
    options
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName))
        .AddOtlpExporter()
        .UseGrafana();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter()
        .UseGrafana())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter()
        .UseGrafana());

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();