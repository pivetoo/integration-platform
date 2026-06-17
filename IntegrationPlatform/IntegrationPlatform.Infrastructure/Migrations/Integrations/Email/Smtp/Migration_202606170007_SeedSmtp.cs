using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.Email.Smtp
{
    // Integracao Email: SMTP generico (step nativo SmtpSend, sem apicall/jsfunc).
    // Idempotente e convergente (no-op onde ja existe).
    [Migration(202606170007)]
    public sealed class Migration_202606170007_SeedSmtp : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedIntegration("smtp", "SMTP", "Envio de email via servidor SMTP generico (Gmail, Outlook, Postfix, AWS SES SMTP, etc).", "email", "https://logos.hunter.io/minutemailer.com", supportsWebhook: false);

            SeedAttribute("smtp", "host", "Host SMTP", FieldType.Text, required: true, order: 1, group: "Servidor", description: "Endereco do servidor SMTP.", placeholder: "smtp.example.com");
            SeedAttribute("smtp", "port", "Porta", FieldType.Number, required: true, order: 2, group: "Servidor", description: "Porta TCP. Comuns: 587 (STARTTLS), 465 (SSL), 25.", placeholder: "587");
            SeedAttribute("smtp", "username", "Usuario", FieldType.Text, required: true, order: 3, group: "Autenticação", description: "Usuario de autenticacao SMTP.", placeholder: "usuario@example.com");
            SeedAttribute("smtp", "password", "Senha / App Password", FieldType.Text, required: true, order: 4, group: "Autenticação", sensitive: true, description: "Senha SMTP ou App Password (Gmail/Outlook).");
            SeedAttribute("smtp", "from_email", "Email remetente", FieldType.Text, required: true, order: 5, group: "Remetente", description: "Endereco usado no campo From.", placeholder: "no-reply@empresa.com");
            SeedAttribute("smtp", "from_name", "Nome remetente", FieldType.Text, required: false, order: 6, group: "Remetente", description: "Nome exibido no campo From.", placeholder: "Kanvas");
            SeedAttribute("smtp", "enable_ssl", "Habilitar SSL/TLS", FieldType.Boolean, required: true, order: 7, group: "Servidor", description: "true/false. Use true em portas 587/465.", placeholder: "true");

            BindContract("smtp", "email.send");

            SeedPipeline("smtp", "smtp-send-email", "Enviar email", "Envia e-mail via protocolo SMTP.", isDefault: true, isTestPipeline: false, contractIdentifier: "email.send");
            SeedPipeline("smtp", "smtp-test-connection", "Testar conexao", "Envia e-mail de teste via SMTP para validar credenciais.", isDefault: false, isTestPipeline: true, contractIdentifier: null);

            SeedStep("smtp-send-email", 1, "Enviar e-mail via SMTP", PipelineStepType.SmtpSend, ErrorAction.Stop);
            SeedStep("smtp-test-connection", 1, "Enviar e-mail de teste via SMTP", PipelineStepType.SmtpSend, ErrorAction.Stop);
        }

        public override void Down()
        {
        }
    }
}
