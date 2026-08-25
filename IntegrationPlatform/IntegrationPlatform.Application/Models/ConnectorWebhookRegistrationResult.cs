namespace IntegrationPlatform.Application.Models
{
    // Resultado de ConnectorService.RegisterWebhook: Supported=false quando a integracao nao tem
    // pipeline "{identifier}-registrar-webhook" (registro manual, ex.: ClickSign/ZapSign/D4Sign hoje).
    public sealed record ConnectorWebhookRegistrationResult(bool Supported, bool Success, string Message);
}
