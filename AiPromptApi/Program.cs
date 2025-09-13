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
builder.Services.AddScoped<GoogleClientFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IApiTokenRepository, ApiTokenRepository>();
builder.Services.AddScoped<IKernelService, KernelService>();
builder.Services.AddScoped<IUserService, UserService>();

var semanticKernelSettings = builder.Configuration.GetRequiredSection<SemanticKernelSettings>("SemanticKernel");
builder.Services.AddSingleton(semanticKernelSettings);

var frontendUrl = builder.Configuration.GetRequiredValue("FrontendUrl");

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        var isDevelopment = builder.Environment.IsDevelopment();
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = frontendUrl,
            ValidAudience = frontendUrl,
            
            ValidateIssuer = !isDevelopment,
            ValidateAudience = !isDevelopment,
            
            IssuerSigningKeyResolver = (_, _, kid, _) =>
            {
                var jwksUrl = $"{frontendUrl}/api/auth/jwks";
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
        policy.WithOrigins("http://localhost:3000", frontendUrl)
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