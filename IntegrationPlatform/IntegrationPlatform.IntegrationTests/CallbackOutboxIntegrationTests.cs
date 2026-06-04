using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.IntegrationTests
{
    [TestFixture]
    public sealed class CallbackOutboxIntegrationTests : IntegrationTestBase
    {
        [Test]
        public async Task GetPendingToDeliver_returns_only_due_pending_ordered_by_next_attempt()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            await InScopeAsync(async serviceProvider =>
            {
                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();

                CallbackDelivery earlier = new(1, 1, "svc", "https://earlier.example.com", null, "{}", now.AddMinutes(-10));
                earlier.SetCreatedAt(now);
                CallbackDelivery later = new(2, 1, "svc", "https://later.example.com", null, "{}", now.AddMinutes(-5));
                later.SetCreatedAt(now);
                CallbackDelivery future = new(3, 1, "svc", "https://future.example.com", null, "{}", now.AddMinutes(30));
                future.SetCreatedAt(now);

                dbContext.AddRange(earlier, later, future);
                await dbContext.SaveChangesAsync();
            });

            await InScopeAsync(async serviceProvider =>
            {
                ICallbackDeliveryService service = serviceProvider.GetRequiredService<ICallbackDeliveryService>();

                IReadOnlyCollection<CallbackDelivery> pending = await service.GetPendingToDeliver(10);

                pending.Should().HaveCount(2);
                pending.Select(item => item.CallbackUrl).Should().Equal("https://earlier.example.com", "https://later.example.com");
            });
        }

        [Test]
        public async Task GetPendingToDeliver_excludes_already_delivered_items()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            await InScopeAsync(async serviceProvider =>
            {
                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();

                CallbackDelivery pending = new(1, 1, "svc", "https://pending.example.com", null, "{}", now.AddMinutes(-1));
                pending.SetCreatedAt(now);

                CallbackDelivery delivered = new(2, 1, "svc", "https://delivered.example.com", null, "{}", now.AddMinutes(-1));
                delivered.SetCreatedAt(now);
                delivered.MarkDelivered(now);

                dbContext.AddRange(pending, delivered);
                await dbContext.SaveChangesAsync();
            });

            await InScopeAsync(async serviceProvider =>
            {
                ICallbackDeliveryService service = serviceProvider.GetRequiredService<ICallbackDeliveryService>();

                IReadOnlyCollection<CallbackDelivery> pending = await service.GetPendingToDeliver(10);

                pending.Should().ContainSingle();
                pending.Single().CallbackUrl.Should().Be("https://pending.example.com");
            });
        }
    }
}
