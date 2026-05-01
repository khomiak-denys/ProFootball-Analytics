namespace ProFootball.Domain.Entities;

public sealed class MatchEvent
{
    private MatchEvent()
    {
    }

    public MatchEvent(
        long id,
        int matchId,
        short minute,
        string eventType,
        int teamId,
        int playerId,
        int? assistPlayerId,
        string? payloadJson,
        DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        Id = id;
        MatchId = matchId;
        Minute = minute;
        EventType = eventType.Trim();
        TeamId = teamId;
        PlayerId = playerId;
        AssistPlayerId = assistPlayerId;
        PayloadJson = payloadJson?.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public long Id { get; private set; }

    public int MatchId { get; private set; }

    public short Minute { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public int TeamId { get; private set; }

    public int PlayerId { get; private set; }

    public int? AssistPlayerId { get; private set; }

    public string? PayloadJson { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
}

