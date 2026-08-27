using System;
using Newtonsoft.Json;

/// <summary>
/// Abstract base for all training session types.
/// Holds identity, timestamps and duration. Subclasses add sport-specific data.
/// </summary>
public abstract class TrainingSession
{
    /// <summary>Unique identifier for this session (GUID string).</summary>
    public string sessionId;

    /// <summary>Session start time, formatted as "yyyy-MM-dd HH:mm".</summary>
    public string date;

    /// <summary>Session end time, set by <see cref="Finish"/>. Null while the session is still active.</summary>
    public string endTime;

    /// <summary>Total session duration in seconds, set by <see cref="Finish"/>.</summary>
    public int durationSeconds;

    /// <summary>Identifies which subtype this session is (used for polymorphic JSON deserialization).</summary>
    public SessionType sessionType;

    [JsonIgnore]
    private DateTime _startDateTime;

    /// <param name="type">The concrete session type for this instance.</param>
    protected TrainingSession(SessionType type)
    {
        _startDateTime = DateTime.Now;
        sessionId      = Guid.NewGuid().ToString();
        date           = _startDateTime.ToString("yyyy-MM-dd HH:mm");
        sessionType    = type;
    }

    /// <summary>
    /// Marks the session as finished by recording <see cref="endTime"/> and <see cref="durationSeconds"/>.
    /// Call this once before the final save (e.g. in OnApplicationQuit).
    /// </summary>
    public void Finish()
    {
        endTime         = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        durationSeconds = (int)(DateTime.Now - _startDateTime).TotalSeconds;
    }
}
