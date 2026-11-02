using GetABursary.Core.Common;
using GetABursary.Core.Repositories.Bursaries;

namespace GetABursary.Worker.Pipeline.Jobs;

public interface IClosureJob
{
    /// <summary>Closes published bursaries whose closing date is before today (SAST). Returns how many were closed.</summary>
    Task<int> RunAsync(CancellationToken ct);
}

public sealed class ClosureJob(IBursaryWriteRepository bursaries, IClock clock) : IClosureJob
{
    public Task<int> RunAsync(CancellationToken ct) => bursaries.CloseExpiredAsync(clock.Today, clock.UtcNow, ct);
}
