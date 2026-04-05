using Archon.Core.Http;
using Archon.Core.Templating;
using IntegrationPlataform.Application.Models;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;
using Jint;
using Microsoft.Extensions.Localization;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace IntegrationPlataform.Infrastructure.Services.ExecutionEngine
{
    public sealed class StepExecutorService : IStepExecutorService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IStringLocalizer<IntegrationPlataformResource> Localizer;

        public StepExecutorService(IHttpClientFactory httpClientFactory, IStringLocalizer<IntegrationPlataformResource> localizer)
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
                HttpClient client = httpClientFactory.CreateClient();
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

                engine.SetValue("variables", context.StepVariables);
                engine.SetValue("payload", context.PayloadData);
                engine.SetValue("attributes", context.ConnectorAttributes);
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
            string interpolatedScript = string.Empty;

            try
            {
                interpolatedScript = TemplateInterpolator.Interpolate(databaseScript.Script, context.StepVariables, context.PayloadData, context.ConnectorAttributes);
                string connectionString = BuildConnectionString(connection);
                IDatabaseExecutor executor = ResolveDatabaseExecutor(connection.Type);
                object result = await executor.ExecuteQueryAsync(connectionString, interpolatedScript, cancellationToken: cancellationToken);

                stopwatch.Stop();

                return new PipelineStepExecutionResult
                {
                    Success = true,
                    RequestInfo = interpolatedScript,
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
                    RequestInfo = interpolatedScript,
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
                        builder.AppendLine(JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions { WriteIndented = true }));
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
            return connection.Type switch
            {
                DatabaseType.PostgreSql => $"Host={connection.Host};Port={connection.Port};Database={connection.Database};Username={connection.Username};Password={connection.Password}",
                DatabaseType.SqlServer => $"Server={connection.Host},{connection.Port};Database={connection.Database};User Id={connection.Username};Password={connection.Password};TrustServerCertificate=True",
                _ => throw new NotSupportedException(Localizer["step.database.type.unsupported", connection.Type])
            };
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
