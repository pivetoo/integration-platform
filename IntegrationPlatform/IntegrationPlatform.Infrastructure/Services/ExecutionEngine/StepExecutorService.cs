using Archon.Core.Http;
using Archon.Core.Templating;
using IntegrationPlatform.Application.Models;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Security;
using Jint;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using MimeKit;
using Npgsql;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace IntegrationPlatform.Infrastructure.Services.ExecutionEngine
{
    public sealed class StepExecutorService : IStepExecutorService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public StepExecutorService(IHttpClientFactory httpClientFactory, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.httpClientFactory = httpClientFactory;
            Localizer = localizer;
        }

        public Task<PipelineStepExecutionResult> Execute(PipelineStep step, PipelineExecutionContext context, CancellationToken cancellationToken = default)
        {
            return step.Type switch
            {
                PipelineStepType.HttpRequest => ExecuteHttpRequest(step, context, cancellationToken),
                PipelineStepType.JavaScriptFunction => Task.FromResult(ExecuteJavaScript(step, context)),
                PipelineStepType.ExecuteScript => ExecuteScript(step, context, cancellationToken),
                PipelineStepType.SmtpSend => ExecuteSmtpSend(step, context, cancellationToken),
                _ => Task.FromResult(new PipelineStepExecutionResult
                {
                    Success = false,
                    Error = Localizer["step.type.unknown", step.Type].Value
                })
            };
        }

        private async Task<PipelineStepExecutionResult> ExecuteHttpRequest(PipelineStep step, PipelineExecutionContext context, CancellationToken cancellationToken)
        {
            ApiCall? apiCall = step.ApiCall;
            if (apiCall is null)
            {
                return new PipelineStepExecutionResult
                {
                    Success = false,
                    Error = Localizer["step.apiCall.notConfigured"]
                };
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                string url = TemplateInterpolator.Interpolate(apiCall.Url, context.StepVariables, context.PayloadData, context.ConnectorAttributes);

                if (!await OutboundUrlGuard.IsAllowedAsync(url, cancellationToken))
                {
                    stopwatch.Stop();
                    return new PipelineStepExecutionResult
                    {
                        Success = false,
                        DurationInMilliseconds = stopwatch.ElapsedMilliseconds,
                        Error = Localizer["step.http.urlBlocked", url]
                    };
                }

                HttpMethod method = ResolveHttpMethod(apiCall.Method);
                using HttpRequestMessage request = new(method, url);

                ApplyHeaders(request, apiCall, context);

                if (method != HttpMethod.Get && method != HttpMethod.Delete)
                {
                    string body = TemplateInterpolator.Interpolate(apiCall.BodyTemplate, context.StepVariables, context.PayloadData, context.ConnectorAttributes);
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        request.Content = CreateRequestContent(request, body);
                    }
                }

                string requestInfo = await SerializeRequest(request);
                HttpClient client = httpClientFactory.CreateClient("outbound");
                client.Timeout = TimeSpan.FromSeconds(60);
                using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

                string responseBody;
                string? contentType = response.Content.Headers.ContentType?.MediaType;

                if (BinaryHttpResponseUtils.IsBinaryContentType(contentType))
                {
                    byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    bool isPdf = BinaryHttpResponseUtils.IsPdfContent(bytes, contentType);
                    string fileName = BinaryHttpResponseUtils.GetResponseFileName(response, contentType, isPdf);

                    responseBody = JsonSerializer.Serialize(new Dictionary<string, object?>
                    {
                        ["isBinary"] = true,
                        ["mimeType"] = isPdf ? "application/pdf" : contentType ?? "application/octet-stream",
                        ["fileName"] = fileName,
                        ["size"] = bytes.LongLength,
                        ["base64"] = Convert.ToBase64String(bytes)
                    });
                }
                else
                {
                    responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                }

                stopwatch.Stop();

                return new PipelineStepExecutionResult
                {
                    Success = response.IsSuccessStatusCode,
                    RequestInfo = requestInfo,
                    ResponseBody = responseBody,
                    StatusCode = (int)response.StatusCode,
                    DurationInMilliseconds = stopwatch.ElapsedMilliseconds,
                    Error = response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}: {responseBody}"
                };
            }
            catch (Exception exception)
            {
                stopwatch.Stop();

                return new PipelineStepExecutionResult
                {
                    Success = false,
                    DurationInMilliseconds = stopwatch.ElapsedMilliseconds,
                    Error = exception.Message
                };
            }
        }

        private PipelineStepExecutionResult ExecuteJavaScript(PipelineStep step, PipelineExecutionContext context)
        {
            JavaScriptFunction? javaScriptFunction = step.JavaScriptFunction;
            if (javaScriptFunction is null || string.IsNullOrWhiteSpace(javaScriptFunction.Code))
            {
                return new PipelineStepExecutionResult
                {
                    Success = false,
                    Error = Localizer["step.javaScript.notConfigured"]
                };
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                Engine engine = new(cfg => cfg
                    .TimeoutInterval(TimeSpan.FromSeconds(30))
                    .LimitMemory(50_000_000)
                    .LimitRecursion(100));

                Dictionary<string, string> scriptAttributes = context.ConnectorAttributes
                    .Where(pair => !context.SensitiveAttributeFields.Contains(pair.Key))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                engine.SetValue("variables", context.StepVariables);
                engine.SetValue("payload", context.PayloadData);
                engine.SetValue("attributes", scriptAttributes);
                engine.SetValue("result", new Dictionary<string, object?> { ["value"] = null });

                engine.Execute(javaScriptFunction.Code);

                object? resultValue = null;
                Jint.Native.JsValue jsResult = engine.GetValue("result");
                if (jsResult.IsObject())
                {
                    Jint.Native.JsValue jsValue = jsResult.AsObject().Get("value");
                    if (!jsValue.IsUndefined() && !jsValue.IsNull())
                    {
                        resultValue = jsValue.ToObject();
                    }
                }

                stopwatch.Stop();

                return new PipelineStepExecutionResult
                {
                    Success = true,
                    DurationInMilliseconds = stopwatch.ElapsedMilliseconds,
                    ExtractedResult = resultValue,
                    ResponseBody = resultValue is not null ? JsonSerializer.Serialize(resultValue) : null
                };
            }
            catch (Exception exception)
            {
                stopwatch.Stop();

                return new PipelineStepExecutionResult
                {
                    Success = false,
                    DurationInMilliseconds = stopwatch.ElapsedMilliseconds,
                    Error = exception.Message
                };
            }
        }

        private async Task<PipelineStepExecutionResult> ExecuteSmtpSend(PipelineStep step, PipelineExecutionContext context, CancellationToken cancellationToken)
        {
            string host = context.ConnectorAttributes.GetValueOrDefault("host") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(host))
            {
                return new PipelineStepExecutionResult
                {
                    Success = false,
                    Error = Localizer["step.smtp.host.required"]
                };
            }

            string to = TemplateInterpolator.Interpolate("{{ to }}", context.StepVariables, context.PayloadData, context.ConnectorAttributes);
            if (string.IsNullOrWhiteSpace(to))
            {
                return new PipelineStepExecutionResult
                {
                    Success = false,
                    Error = Localizer["step.smtp.recipient.required"]
                };
            }

            string portRaw    = context.ConnectorAttributes.GetValueOrDefault("port")       ?? "587";
            string username   = context.ConnectorAttributes.GetValueOrDefault("username")   ?? string.Empty;
            string password   = context.ConnectorAttributes.GetValueOrDefault("password")   ?? string.Empty;
            string fromEmail  = context.ConnectorAttributes.GetValueOrDefault("from_email") ?? string.Empty;
            string fromName   = context.ConnectorAttributes.GetValueOrDefault("from_name")  ?? string.Empty;
            string enableSslRaw = context.ConnectorAttributes.GetValueOrDefault("enable_ssl") ?? "true";

            int port = int.TryParse(portRaw, out int p) ? p : 587;
            bool enableSsl = !string.Equals(enableSslRaw, "false", StringComparison.OrdinalIgnoreCase) && enableSslRaw != "0";

            string subject  = TemplateInterpolator.Interpolate("{{ subject }}",   context.StepVariables, context.PayloadData, context.ConnectorAttributes);
            string htmlBody = TemplateInterpolator.Interpolate("{{ html_body }}", context.StepVariables, context.PayloadData, context.ConnectorAttributes);
            string textBody = TemplateInterpolator.Interpolate("{{ text_body }}", context.StepVariables, context.PayloadData, context.ConnectorAttributes);

            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                MimeMessage message = new();
                message.From.Add(string.IsNullOrWhiteSpace(fromName)
                    ? new MailboxAddress(fromEmail, fromEmail)
                    : new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress(to, to));
                message.Subject = subject;

                BodyBuilder bodyBuilder = new();
                if (!string.IsNullOrWhiteSpace(htmlBody))
                {
                    bodyBuilder.HtmlBody = htmlBody;
                }
                if (!string.IsNullOrWhiteSpace(textBody))
                {
                    bodyBuilder.TextBody = textBody;
                }
                message.Body = bodyBuilder.ToMessageBody();

                SecureSocketOptions socketOptions = enableSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;

                using SmtpClient client = new();
                client.Timeout = (int)TimeSpan.FromSeconds(60).TotalMilliseconds;
                await client.ConnectAsync(host, port, socketOptions, cancellationToken);

                if (!string.IsNullOrWhiteSpace(username))
                {
                    await client.AuthenticateAsync(username, password, cancellationToken);
                }

                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                stopwatch.Stop();

                return new PipelineStepExecutionResult
                {
                    Success = true,
                    RequestInfo = $"SMTP {host}:{port} | From: {fromEmail} | To: {to} | Subject: {subject}",
                    DurationInMilliseconds = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception exception)
            {
                stopwatch.Stop();

                return new PipelineStepExecutionResult
                {
                    Success = false,
                    DurationInMilliseconds = stopwatch.ElapsedMilliseconds,
                    Error = exception.Message
                };
            }
        }

        private Task<PipelineStepExecutionResult> ExecuteScript(PipelineStep step, PipelineExecutionContext context, CancellationToken cancellationToken)
        {
            return ExecuteScriptInternal(step, context, cancellationToken);
        }

        private async Task<PipelineStepExecutionResult> ExecuteScriptInternal(PipelineStep step, PipelineExecutionContext context, CancellationToken cancellationToken)
        {
            DatabaseScript? databaseScript = step.DatabaseScript;
            if (databaseScript is null)
            {
                return new PipelineStepExecutionResult
                {
                    Success = false,
                    Error = Localizer["step.databaseScript.notConfigured"]
                };
            }

            DatabaseConnection connection = databaseScript.DatabaseConnection;
            if (string.IsNullOrWhiteSpace(databaseScript.Script))
            {
                return new PipelineStepExecutionResult
                {
                    Success = false,
                    Error = Localizer["step.databaseScript.notInformed"]
                };
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            string commandText = string.Empty;

            try
            {
                ParameterizedSql parameterized = SqlScriptParameterizer.Build(databaseScript.Script, context.StepVariables, context.PayloadData, context.ConnectorAttributes);
                commandText = parameterized.CommandText;
                string connectionString = BuildConnectionString(connection);
                IDatabaseExecutor executor = ResolveDatabaseExecutor(connection.Type);
                object result = await executor.ExecuteQueryAsync(connectionString, commandText, parameterized.Parameters, cancellationToken: cancellationToken);

                stopwatch.Stop();

                return new PipelineStepExecutionResult
                {
                    Success = true,
                    RequestInfo = commandText,
                    DurationInMilliseconds = stopwatch.ElapsedMilliseconds,
                    ExtractedResult = result,
                    ResponseBody = JsonSerializer.Serialize(result)
                };
            }
            catch (Exception exception)
            {
                stopwatch.Stop();

                return new PipelineStepExecutionResult
                {
                    Success = false,
                    RequestInfo = commandText,
                    DurationInMilliseconds = stopwatch.ElapsedMilliseconds,
                    Error = exception.Message
                };
            }
        }

        private static HttpMethod ResolveHttpMethod(HttpMethodType method)
        {
            return method switch
            {
                HttpMethodType.Get => HttpMethod.Get,
                HttpMethodType.Post => HttpMethod.Post,
                HttpMethodType.Put => HttpMethod.Put,
                HttpMethodType.Patch => HttpMethod.Patch,
                HttpMethodType.Delete => HttpMethod.Delete,
                _ => HttpMethod.Get
            };
        }

        private static HttpContent CreateRequestContent(HttpRequestMessage request, string body)
        {
            string contentType = GetContentType(request);

            if (string.Equals(contentType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            {
                string trimmed = body.Trim();
                if (trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
                {
                    try
                    {
                        Dictionary<string, string>? dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(body);
                        if (dictionary is not null)
                        {
                            return new FormUrlEncodedContent(dictionary);
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return new StringContent(body, Encoding.UTF8, contentType);
        }

        private static string GetContentType(HttpRequestMessage request)
        {
            if (request.Content?.Headers.ContentType is not null)
            {
                return request.Content.Headers.ContentType.MediaType ?? "application/json";
            }

            if (request.Options.TryGetValue(new HttpRequestOptionsKey<string>("Content-Type"), out string? contentType))
            {
                return contentType;
            }

            return "application/json";
        }

        private static void ApplyHeaders(HttpRequestMessage request, ApiCall apiCall, PipelineExecutionContext context)
        {
            string headersJson = TemplateInterpolator.Interpolate(apiCall.HeadersTemplate, context.StepVariables, context.PayloadData, context.ConnectorAttributes);
            if (string.IsNullOrWhiteSpace(headersJson))
            {
                return;
            }

            Dictionary<string, string>? headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
            if (headers is null)
            {
                return;
            }

            HttpRequestOptionsKey<string> contentTypeKey = new("Content-Type");
            foreach ((string key, string value) in headers)
            {
                if (string.Equals(key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    request.Options.Set(contentTypeKey, value);
                }
                else
                {
                    request.Headers.TryAddWithoutValidation(key, value);
                }
            }
        }

        private static async Task<string> SerializeRequest(HttpRequestMessage request)
        {
            try
            {
                string? body = null;
                if (request.Content is not null)
                {
                    body = await request.Content.ReadAsStringAsync();
                }

                StringBuilder builder = new();
                builder.AppendLine($"Method: {request.Method.Method}");
                builder.AppendLine($"URL: {request.RequestUri}");

                if (request.Headers.Any())
                {
                    builder.AppendLine();
                    builder.AppendLine("Headers:");
                    foreach ((string key, IEnumerable<string> values) in request.Headers)
                    {
                        builder.AppendLine($"  {key}: {string.Join(", ", values)}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(body))
                {
                    builder.AppendLine();
                    builder.AppendLine("Body:");

                    try
                    {
                        using JsonDocument jsonDocument = JsonDocument.Parse(body);
                        builder.AppendLine(JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
                    }
                    catch
                    {
                        builder.AppendLine(body);
                    }
                }

                return builder.ToString();
            }
            catch
            {
                return $"{request.Method} {request.RequestUri}";
            }
        }

        private string BuildConnectionString(DatabaseConnection connection)
        {
            switch (connection.Type)
            {
                case DatabaseType.PostgreSql:
                    NpgsqlConnectionStringBuilder postgres = new()
                    {
                        Host = connection.Host,
                        Port = connection.Port,
                        Database = connection.Database,
                        Username = connection.Username,
                        Password = connection.Password
                    };
                    return postgres.ConnectionString;

                case DatabaseType.SqlServer:
                    SqlConnectionStringBuilder sqlServer = new()
                    {
                        DataSource = $"{connection.Host},{connection.Port}",
                        InitialCatalog = connection.Database,
                        UserID = connection.Username,
                        Password = connection.Password,
                        TrustServerCertificate = true
                    };
                    return sqlServer.ConnectionString;

                default:
                    throw new NotSupportedException(Localizer["step.database.type.unsupported", connection.Type]);
            }
        }

        private IDatabaseExecutor ResolveDatabaseExecutor(DatabaseType databaseType)
        {
            return databaseType switch
            {
                DatabaseType.PostgreSql => new PostgreSqlExecutor(Localizer),
                DatabaseType.SqlServer => new SqlServerExecutor(Localizer),
                _ => throw new NotSupportedException(Localizer["step.database.type.unsupported", databaseType])
            };
        }
    }
}
