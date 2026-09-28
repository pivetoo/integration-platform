using IntegrationPlatform.Infrastructure.Services.ExecutionEngine;

namespace IntegrationPlatform.Testing.Infrastructure.ExecutionEngine
{
    [TestFixture]
    public sealed class TransientFailureTests
    {
        [TestCase(null)]
        [TestCase(408)]
        [TestCase(429)]
        [TestCase(500)]
        [TestCase(502)]
        [TestCase(503)]
        [TestCase(504)]
        public void IsTransientStatus_true_for_retryable_statuses(int? statusCode)
        {
            TransientFailure.IsTransientStatus(statusCode).Should().BeTrue();
        }

        [TestCase(200)]
        [TestCase(400)]
        [TestCase(401)]
        [TestCase(403)]
        [TestCase(404)]
        [TestCase(409)]
        [TestCase(422)]
        public void IsTransientStatus_false_for_success_and_client_errors(int statusCode)
        {
            TransientFailure.IsTransientStatus(statusCode).Should().BeFalse();
        }

        [Test]
        public void IsTransientException_true_for_http_request_exception()
        {
            TransientFailure.IsTransientException(new HttpRequestException("network down"), CancellationToken.None).Should().BeTrue();
        }

        [Test]
        public void IsTransientException_true_for_timeout()
        {
            TransientFailure.IsTransientException(new TaskCanceledException("timeout"), CancellationToken.None).Should().BeTrue();
        }

        [Test]
        public void IsTransientException_false_when_caller_cancelled()
        {
            using CancellationTokenSource source = new();
            source.Cancel();

            TransientFailure.IsTransientException(new TaskCanceledException("cancelled"), source.Token).Should().BeFalse();
        }

        [TestCase(typeof(InvalidOperationException))]
        [TestCase(typeof(FormatException))]
        public void IsTransientException_false_for_other_exceptions(Type exceptionType)
        {
            Exception exception = (Exception)Activator.CreateInstance(exceptionType)!;

            TransientFailure.IsTransientException(exception, CancellationToken.None).Should().BeFalse();
        }
    }
}
