using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.ContentAuthoring.Domain;

public enum DraftStatus
{
    /// <summary>Waiting for the writer.</summary>
    Queued,

    /// <summary>The writer and reviewer are working on it.</summary>
    InProgress,

    /// <summary>The AI reviewer approved it: waiting for a person.</summary>
    AiApproved,

    /// <summary>The AI could not get it right in the allowed attempts: a person must decide.</summary>
    NeedsAttention,

    /// <summary>The run broke (an outage, a bad answer); it can be put back in the queue.</summary>
    Failed,

    /// <summary>A person approved it and learners can take it.</summary>
    Published,
}

/// <summary>One version of the lesson with the verdict on it.</summary>
public sealed class DraftRevision
{
    private DraftRevision()
    {
    }

    /// <summary>1 for the first draft, then one more for each rewrite.</summary>
    public int Number { get; private set; }

    public DraftContent Content { get; private set; } = new();

    public ReviewReport Review { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    internal static DraftRevision Create(int number, DraftContent content, ReviewReport review, DateTime now) =>
        new() { Number = number, Content = content, Review = review, CreatedAtUtc = now };
}

/// <summary>What a person asked the writer to change when sending a lesson back.</summary>
public sealed class FeedbackNote
{
    public Guid By { get; init; }

    public string Text { get; init; } = string.Empty;

    public DateTime AtUtc { get; init; }
}

/// <summary>
/// A lesson on its way from idea to learners. The AI writes and reviews it, looping until the
/// reviewer approves or the attempts run out; a person then reads it and publishes it, or sends
/// it back with notes. Nothing reaches learners without that person.
/// </summary>
public sealed class LessonDraft
{
    private readonly List<DraftRevision> _revisions = [];
    private readonly List<FeedbackNote> _feedback = [];

    private LessonDraft()
    {
    }

    public Guid Id { get; private set; }

    public LessonBrief Brief { get; private set; } = null!;

    public DraftStatus Status { get; private set; }

    /// <summary>Write-and-review attempts in the current run.</summary>
    public int CyclesUsed { get; private set; }

    /// <summary>Why the last run broke; set while <see cref="DraftStatus.Failed"/>.</summary>
    public string? Error { get; private set; }

    public Guid CreatedBy { get; private set; }

    /// <summary>The person who published the lesson.</summary>
    public Guid? PublishedBy { get; private set; }

    /// <summary>The lesson id learners see; the same as the draft id.</summary>
    public Guid? PublishedLessonId { get; private set; }

    public int? PublishedLessonNumber { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyList<DraftRevision> Revisions => _revisions;

    public IReadOnlyList<FeedbackNote> Feedback => _feedback;

    public DraftRevision? LatestRevision => _revisions.Count == 0 ? null : _revisions[^1];

    /// <summary>The lesson as it stands: the latest version written.</summary>
    public DraftContent? CurrentContent => LatestRevision?.Content;

    public static LessonDraft Create(Guid id, LessonBrief brief, Guid createdBy, DateTime now) => new()
    {
        Id = id,
        Brief = brief,
        Status = DraftStatus.Queued,
        CreatedBy = createdBy,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
    };

    /// <summary>The writer picks the draft up.</summary>
    public void StartRun(DateTime now)
    {
        Require(DraftStatus.Queued);
        Status = DraftStatus.InProgress;
        CyclesUsed = 0;
        Error = null;
        Touch(now);
    }

    public void AddRevision(DraftContent content, ReviewReport review, DateTime now)
    {
        Require(DraftStatus.InProgress);
        _revisions.Add(DraftRevision.Create(_revisions.Count + 1, content, review, now));
        CyclesUsed++;
        Touch(now);
    }

    public void ApproveByAi(DateTime now)
    {
        Require(DraftStatus.InProgress);
        if (LatestRevision?.Review.Passed != true)
        {
            throw InvalidState("The latest version did not pass review.");
        }

        Status = DraftStatus.AiApproved;
        Touch(now);
    }

    public void FlagNeedsAttention(DateTime now)
    {
        Require(DraftStatus.InProgress);
        Status = DraftStatus.NeedsAttention;
        Touch(now);
    }

    public void Fail(string error, DateTime now)
    {
        Require(DraftStatus.InProgress);
        Status = DraftStatus.Failed;
        Error = error;
        Touch(now);
    }

    /// <summary>Puts a broken run back in the queue; also used for a run cut short by a restart.</summary>
    public void Requeue(DateTime now)
    {
        Require(DraftStatus.Failed, DraftStatus.InProgress);
        Status = DraftStatus.Queued;
        Error = null;
        Touch(now);
    }

    /// <summary>A person sends the lesson back; the writer must address the note.</summary>
    public void RejectByHuman(string? note, Guid by, DateTime now)
    {
        Require(DraftStatus.AiApproved, DraftStatus.NeedsAttention);
        if (string.IsNullOrWhiteSpace(note))
        {
            throw new BusinessRuleException(AuthoringErrors.FeedbackRequired, "Say what to change; the writer works from your note.");
        }

        _feedback.Add(new FeedbackNote { By = by, Text = note.Trim(), AtUtc = now });
        Status = DraftStatus.Queued;
        Touch(now);
    }

    /// <summary>A person may publish a lesson the AI approved, or one the AI gave up on.</summary>
    public void EnsureCanPublish() => Require(DraftStatus.AiApproved, DraftStatus.NeedsAttention);

    public void MarkPublished(Guid by, Guid lessonId, int lessonNumber, DateTime now)
    {
        EnsureCanPublish();
        Status = DraftStatus.Published;
        PublishedBy = by;
        PublishedLessonId = lessonId;
        PublishedLessonNumber = lessonNumber;
        Touch(now);
    }

    private void Require(params DraftStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw InvalidState($"The draft is {Status}; this needs it to be {string.Join(" or ", allowed)}.");
        }
    }

    private void Touch(DateTime now) => UpdatedAtUtc = now;

    private static BusinessRuleException InvalidState(string message) => new(AuthoringErrors.InvalidState, message);
}
