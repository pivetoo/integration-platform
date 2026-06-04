using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Testing.Domain.ValueObjects
{
    [TestFixture]
    public sealed class RoutineScheduleTests
    {
        private static DateTimeOffset At(int hour, int minute)
        {
            return new DateTimeOffset(2026, 6, 4, hour, minute, 0, TimeSpan.Zero);
        }

        [Test]
        public void ComputeNextExecution_first_run_uses_now_plus_interval()
        {
            DateTimeOffset next = RoutineSchedule.ComputeNextExecution(null, 5, At(10, 0));

            next.Should().Be(At(10, 5));
        }

        [Test]
        public void ComputeNextExecution_on_schedule_stays_on_the_grid_without_drift()
        {
            // Planejado 10:00, rodou as 10:02 -> proximo deve ser 10:05 (grade), nao 10:07 (drift).
            DateTimeOffset next = RoutineSchedule.ComputeNextExecution(At(10, 0), 5, At(10, 2));

            next.Should().Be(At(10, 5));
        }

        [Test]
        public void ComputeNextExecution_after_downtime_realigns_to_next_future_slot_without_catch_up()
        {
            // Planejado 08:00 a cada 5min, voltou as 08:12 -> proximo slot futuro e 08:15 (uma execucao, sem repor as perdidas).
            DateTimeOffset next = RoutineSchedule.ComputeNextExecution(At(8, 0), 5, At(8, 12));

            next.Should().Be(At(8, 15));
        }

        [Test]
        public void ComputeNextExecution_when_now_is_exactly_on_a_slot_advances_to_the_next()
        {
            DateTimeOffset next = RoutineSchedule.ComputeNextExecution(At(8, 0), 5, At(8, 5));

            next.Should().Be(At(8, 10));
        }

        [Test]
        public void ComputeNextExecution_guards_against_non_positive_interval()
        {
            DateTimeOffset next = RoutineSchedule.ComputeNextExecution(At(8, 0), 0, At(8, 0));

            next.Should().Be(At(8, 1));
        }
    }
}
