namespace GetABursary.Worker.Sources.Http;

/// <summary>Waits for a duration. Injectable so tests never really sleep.</summary>
public interface IDelayer
{
    Task DelayAsync(TimeSpan delay, CancellationToken ct);
}

public sealed class TaskDelayer : IDelayer
{
    public Task DelayAsync(TimeSpan delay, CancellationToken ct) => Task.Delay(delay, ct);
}
