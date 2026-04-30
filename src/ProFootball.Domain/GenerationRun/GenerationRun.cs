namespace ProFootball.Domain.Entities;

public sealed class GenerationRun
{
    private GenerationRun()
    {
    }

    public GenerationRun(
        long id,
        string generatorVersion,
        string seed,
        string profile,
        string mode,
        DateTime startedAtUtc,
        DateTime? finishedAtUtc,
        string status,
        string? message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(generatorVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(seed);
        ArgumentException.ThrowIfNullOrWhiteSpace(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(mode);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        Id = id;
        GeneratorVersion = generatorVersion.Trim();
        Seed = seed.Trim();
        Profile = profile.Trim();
        Mode = mode.Trim();
        StartedAtUtc = startedAtUtc;
        FinishedAtUtc = finishedAtUtc;
        Status = status.Trim();
        Message = message?.Trim();
    }

    public long Id { get; private set; }

    public string GeneratorVersion { get; private set; } = string.Empty;

    public string Seed { get; private set; } = string.Empty;

    public string Profile { get; private set; } = string.Empty;

    public string Mode { get; private set; } = string.Empty;

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? FinishedAtUtc { get; private set; }

    public string Status { get; private set; } = string.Empty;

    public string? Message { get; private set; }
}

