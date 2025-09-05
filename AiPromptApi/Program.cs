using Grafana.OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using AiPromptApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<GraphClientFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();

var issuer = builder.Configuration["Issuer"];

if (issuer == null)
{
    throw new Exception();
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256],
            
            IssuerSigningKeyResolver = (_, _, kid, _) => 
                GetSigningKeysFromJwks(kid, issuer).GetAwaiter().GetResult()
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

return;

async Task<IEnumerable<SecurityKey>> GetSigningKeysFromJwks(string? kid, string issuer)
{
    var jwksUrl = $"{issuer}/api/auth/jwks";
    
    using var httpClient = new HttpClient();
    var response = await httpClient.GetStringAsync(jwksUrl);
    
    var jwks = new JsonWebKeySet(response);

    if (string.IsNullOrEmpty(kid))
    {
        return jwks.Keys;
    }
        
    var key = jwks.Keys.FirstOrDefault(k => k.Kid == kid);

    if (key == null)
    {
        return jwks.Keys;
    }
            
    return [key];
}