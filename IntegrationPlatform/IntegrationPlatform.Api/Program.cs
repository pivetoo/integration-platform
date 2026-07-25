using Archon.Api.DependencyInjection;
using Archon.Api.MultiTenancy;
using Archon.Infrastructure.DependencyInjection;
using IntegrationPlatform.Api.BackgroundJobs;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Infrastructure.DependencyInjection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("IntegrationPlatformCors", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddAuthorization();
builder.Services.AddArchonApi(builder.Configuration, typeof(IntegrationPlatformResource));
builder.Services.AddIntegrationPlatformInfrastructure(builder.Configuration);
builder.Services.AddServicesFromAssembly(typeof(Program).Assembly);

#region Background Jobs
builder.Services.AddHostedService<QueueWorkerService>();
builder.Services.AddHostedService<PipelineRoutineSchedulerService>();
builder.Services.AddHostedService<CallbackDeliveryJob>();
#endregion

builder.Services.AddArchonAuthentication(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("IntegrationPlatformCors");
app.UseArchonApi();

app.UseAuthentication();
// Depois da autenticacao de proposito: o tenant sai de claim ja validada.
app.UseArchonTenantResolution();
app.UseAuthorization();
// UseSessionValidation() removido: nao existe implementacao de ISessionValidator no ecossistema,
// entao a chamada rodava sem validar nada e dava falsa impressao de barrar sessao revogada.
// Reativar junto com a implementacao (o IdM tem a tabela loginsessions, falta o endpoint).
app.MapControllers();
await app.UseArchonAccessSyncAsync();
app.Run();
