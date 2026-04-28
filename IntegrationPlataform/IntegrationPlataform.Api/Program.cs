using Archon.Api.DependencyInjection;
using Archon.Api.MultiTenancy;
using Archon.Infrastructure.DependencyInjection;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Infrastructure.DependencyInjection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
bool hasIdentityManagementConfiguration = HasIdentityManagementConfiguration(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("IntegrationPlataformCors", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddAuthorization();
builder.Services.AddArchonApi(builder.Configuration, typeof(IntegrationPlataformResource));
builder.Services.AddIntegrationPlataformInfrastructure(builder.Configuration);
builder.Services.AddServicesFromAssembly(typeof(Program).Assembly);

if (hasIdentityManagementConfiguration)
{
    builder.Services.AddArchonAuthentication(builder.Configuration);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("IntegrationPlataformCors");
app.UseArchonApi();

if (hasIdentityManagementConfiguration)
{
    app.UseAuthentication();
}

app.UseAuthorization();

if (hasIdentityManagementConfiguration)
{
    app.UseSessionValidation();
}

app.MapControllers();

if (hasIdentityManagementConfiguration)
{
    await app.UseArchonAccessSyncAsync();
}

app.Run();

static bool HasIdentityManagementConfiguration(IConfiguration configuration)
{
    return !string.IsNullOrWhiteSpace(configuration["IdentityManagement:Authority"]);
}
