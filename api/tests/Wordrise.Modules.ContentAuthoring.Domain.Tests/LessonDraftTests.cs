using Wordrise.Modules.ContentAuthoring.Domain;
using Wordrise.Modules.ContentAuthoring.TestSupport;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.ContentAuthoring.Domain.Tests;

public sealed class LessonDraftTests
{
    private static readonly DateTime Now = new(2026, 10, 22, 8, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Staff = Guid.NewGuid();

    private static LessonDraft Queued() => LessonDraft.Create(Guid.NewGuid(), ContentFixtures.Brief(), Staff, Now);

    private static LessonDraft InProgress()
    {
        var draft = Queued();
        draft.StartRun(Now);
        return draft;
    }

    private static LessonDraft AiApproved()
    {
        var draft = InProgress();
        draft.AddRevision(ContentFixtures.ValidContent(draft.Brief), ContentFixtures.Passing(), Now);
        draft.ApproveByAi(Now);
        return draft;
    }

    private static LessonDraft NeedsAttention()
    {
        var draft = InProgress();
        draft.AddRevision(ContentFixtures.ValidContent(draft.Brief), ContentFixtures.Failing(), Now);
        draft.FlagNeedsAttention(Now);
        return draft;
    }

    private static LessonDraft Failed()
    {
        var draft = InProgress();
        draft.Fail("The service is down.", Now);
        return draft;
    }

    private static LessonDraft Published()
    {
        var draft = AiApproved();
        draft.MarkPublished(Staff, draft.Id, 2, Now);
        return draft;
    }

    private static string CodeOf(Action action) => Assert.Throws<BusinessRuleException>(action).Code;

    [Fact]
    public void A_new_draft_waits_in_the_queue()
    {
        var draft = Queued();

        Assert.Equal(DraftStatus.Queued, draft.Status);
        Assert.Empty(draft.Revisions);
        Assert.Null(draft.CurrentContent);
        Assert.Equal(Staff, draft.CreatedBy);
    }

    [Fact]
    public void Each_attempt_adds_a_numbered_revision_and_counts_a_cycle()
    {
        var draft = InProgress();

        draft.AddRevision(ContentFixtures.ValidContent(draft.Brief), ContentFixtures.Failing(), Now);
        draft.AddRevision(ContentFixtures.ValidContent(draft.Brief), ContentFixtures.Passing(), Now);

        Assert.Equal([1, 2], draft.Revisions.Select(r => r.Number));
        Assert.Equal(2, draft.CyclesUsed);
        Assert.Same(draft.Revisions[1].Content, draft.CurrentContent);
    }

    [Fact]
    public void A_passing_revision_lets_the_ai_approve_the_draft()
    {
        Assert.Equal(DraftStatus.AiApproved, AiApproved().Status);
    }

    [Fact]
    public void The_ai_cannot_approve_a_draft_whose_latest_revision_failed()
    {
        var draft = InProgress();
        draft.AddRevision(ContentFixtures.ValidContent(draft.Brief), ContentFixtures.Failing(), Now);

        Assert.Equal(AuthoringErrors.InvalidState, CodeOf(() => draft.ApproveByAi(Now)));
        Assert.Equal(DraftStatus.InProgress, draft.Status);
    }

    [Fact]
    public void The_ai_cannot_approve_a_draft_without_any_revision()
    {
        var draft = InProgress();

        Assert.Equal(AuthoringErrors.InvalidState, CodeOf(() => draft.ApproveByAi(Now)));
    }

    [Fact]
    public void A_new_run_starts_the_cycle_count_over()
    {
        var draft = NeedsAttention();
        draft.RejectByHuman("Use simpler words.", Staff, Now);

        draft.StartRun(Now);

        Assert.Equal(0, draft.CyclesUsed);
        Assert.Single(draft.Revisions);
    }

    [Fact]
    public void A_broken_run_can_be_put_back_in_the_queue()
    {
        var draft = Failed();

        Assert.Equal(DraftStatus.Failed, draft.Status);
        Assert.Equal("The service is down.", draft.Error);

        draft.Requeue(Now);

        Assert.Equal(DraftStatus.Queued, draft.Status);
        Assert.Null(draft.Error);
    }

    [Fact]
    public void A_run_cut_short_by_a_restart_can_be_put_back_in_the_queue()
    {
        var draft = InProgress();

        draft.Requeue(Now);

        Assert.Equal(DraftStatus.Queued, draft.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_person_sends_the_lesson_back_with_a_note_and_it_returns_to_the_queue(bool aiGaveUp)
    {
        var draft = aiGaveUp ? NeedsAttention() : AiApproved();
        var reviewer = Guid.NewGuid();

        draft.RejectByHuman("  The cashier would not say that.  ", reviewer, Now);

        Assert.Equal(DraftStatus.Queued, draft.Status);
        var note = Assert.Single(draft.Feedback);
        Assert.Equal("The cashier would not say that.", note.Text);
        Assert.Equal(reviewer, note.By);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sending_a_lesson_back_needs_a_note(string? note)
    {
        var draft = AiApproved();

        Assert.Equal(AuthoringErrors.FeedbackRequired, CodeOf(() => draft.RejectByHuman(note, Staff, Now)));
        Assert.Equal(DraftStatus.AiApproved, draft.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_person_can_publish_a_lesson_the_ai_approved_or_gave_up_on(bool aiGaveUp)
    {
        var draft = aiGaveUp ? NeedsAttention() : AiApproved();
        var lessonId = draft.Id;

        draft.MarkPublished(Staff, lessonId, 7, Now);

        Assert.Equal(DraftStatus.Published, draft.Status);
        Assert.Equal(7, draft.PublishedLessonNumber);
        Assert.Equal(lessonId, draft.PublishedLessonId);
        Assert.Equal(Staff, draft.PublishedBy);
    }

    public static TheoryData<string, Func<LessonDraft>> DraftsThatCannotBePublished() => new()
    {
        { "queued", Queued },
        { "in progress", InProgress },
        { "failed", Failed },
        { "published", Published },
    };

    [Theory]
    [MemberData(nameof(DraftsThatCannotBePublished))]
    public void Only_a_lesson_waiting_for_a_person_can_be_published(string state, Func<LessonDraft> build)
    {
        var draft = build();

        var code = CodeOf(() => draft.MarkPublished(Staff, draft.Id, 1, Now));

        Assert.True(code == AuthoringErrors.InvalidState, state);
    }

    public static TheoryData<string, Func<LessonDraft>> DraftsThatCannotBeSentBack() => new()
    {
        { "queued", Queued },
        { "in progress", InProgress },
        { "failed", Failed },
        { "published", Published },
    };

    [Theory]
    [MemberData(nameof(DraftsThatCannotBeSentBack))]
    public void Only_a_lesson_waiting_for_a_person_can_be_sent_back(string state, Func<LessonDraft> build)
    {
        var draft = build();

        var code = CodeOf(() => draft.RejectByHuman("Change it.", Staff, Now));

        Assert.True(code == AuthoringErrors.InvalidState, state);
    }

    [Fact]
    public void A_draft_that_is_not_in_progress_cannot_receive_a_revision()
    {
        var draft = Queued();

        Assert.Equal(
            AuthoringErrors.InvalidState,
            CodeOf(() => draft.AddRevision(ContentFixtures.ValidContent(draft.Brief), ContentFixtures.Passing(), Now)));
    }

    [Fact]
    public void A_draft_that_is_already_running_cannot_be_started_again()
    {
        var draft = InProgress();

        Assert.Equal(AuthoringErrors.InvalidState, CodeOf(() => draft.StartRun(Now)));
    }

    [Fact]
    public void A_published_draft_cannot_be_requeued()
    {
        var draft = Published();

        Assert.Equal(AuthoringErrors.InvalidState, CodeOf(() => draft.Requeue(Now)));
    }

    [Fact]
    public void Every_change_updates_the_last_modified_time()
    {
        var draft = Queued();
        var later = Now.AddMinutes(5);

        draft.StartRun(later);

        Assert.Equal(later, draft.UpdatedAtUtc);
        Assert.Equal(Now, draft.CreatedAtUtc);
    }
}
