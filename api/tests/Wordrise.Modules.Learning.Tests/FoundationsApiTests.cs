using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wordrise.Modules.Learning.Tests;

/// <summary>The foundations course, and what a learner tells us at sign-up.</summary>
public sealed class FoundationsApiTests(WordriseApiFactory factory) : IClassFixture<WordriseApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private sealed record Onboarding(bool KnowsLetters, int StartStep, bool Done, StartingPoint[] StartingPoints);

    private sealed record StartingPoint(int Step, string Title, string Example);

    private sealed record NextLesson(Guid Id, int Number, string Title, string Track);

    private sealed record Today(
        NextLesson? NextLesson,
        bool NoLessonsAtYourLevelYet,
        bool CompletedLessonToday,
        bool OnboardingDone,
        bool NewLessonsPaused,
        int LearnedWordsCount);

    private sealed record Word(string Target, string Symbol, bool IsLetter);

    private sealed record Option(string Text, bool IsCorrect);

    private sealed record Exercise(string Prompt, string Stimulus, string AudioText, string AudioStyle, Option[] Options);

    private sealed record Lesson(string Track, int? GrammarStep, Word[] Words, Exercise[] Exercises);

    private sealed record Completion(bool AlreadyCompleted, int WordsAdded);

    private static Task<Today?> TodayAsync(HttpClient client) => client.GetFromJsonAsync<Today>("/api/v1/learning/today", Json);

    private static Task<HttpResponseMessage> Choose(HttpClient client, bool knowsLetters, int startStep) =>
        client.PutAsJsonAsync("/api/v1/learning/onboarding", new { knowsLetters, startStep });

    [Fact]
    public async Task A_new_learner_has_the_defaults_and_the_starting_points_to_choose_from()
    {
        var client = await factory.CreateSignedInClientAsync();

        var onboarding = await client.GetFromJsonAsync<Onboarding>("/api/v1/learning/onboarding", Json);

        Assert.True(onboarding!.KnowsLetters);
        Assert.Equal(1, onboarding.StartStep);
        Assert.False(onboarding.Done);
        Assert.Equal(1, onboarding.StartingPoints[0].Step);
        Assert.True(onboarding.StartingPoints.Length >= 5);
    }

    [Fact]
    public async Task The_choice_is_saved_and_can_be_changed()
    {
        var client = await factory.CreateSignedInClientAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await Choose(client, knowsLetters: false, startStep: 11)).StatusCode);
        var saved = await client.GetFromJsonAsync<Onboarding>("/api/v1/learning/onboarding", Json);
        Assert.False(saved!.KnowsLetters);
        Assert.Equal(11, saved.StartStep);
        Assert.True(saved.Done);

        Assert.Equal(HttpStatusCode.NoContent, (await Choose(client, knowsLetters: true, startStep: 1)).StatusCode);
        Assert.True((await client.GetFromJsonAsync<Onboarding>("/api/v1/learning/onboarding", Json))!.KnowsLetters);
    }

    [Fact]
    public async Task A_starting_point_that_was_not_offered_is_refused()
    {
        var client = await factory.CreateSignedInClientAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await Choose(client, true, 2)).StatusCode);
    }

    [Fact]
    public async Task Signing_in_is_required()
    {
        var anonymous = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/learning/onboarding")).StatusCode);
    }

    [Fact]
    public async Task A_learner_who_knows_the_letters_goes_straight_to_the_main_course()
    {
        var client = await factory.CreateSignedInClientAsync();
        await Choose(client, knowsLetters: true, startStep: 1);

        var today = await TodayAsync(client);

        Assert.Equal("main", today!.NextLesson!.Track);
        Assert.True(today.OnboardingDone);
    }

    [Fact]
    public async Task A_learner_who_does_not_know_the_letters_starts_with_the_foundations_in_order()
    {
        var client = await factory.CreateSignedInClientAsync();
        await Choose(client, knowsLetters: false, startStep: 1);

        var first = (await TodayAsync(client))!.NextLesson!;

        Assert.Equal("foundations", first.Track);
        Assert.Equal(1, first.Number);
    }

    [Fact]
    public async Task The_course_starts_with_the_letters_then_numbers_then_greetings()
    {
        var client = await factory.CreateSignedInClientAsync();
        await Choose(client, knowsLetters: false, startStep: 1);

        var order = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            var next = (await TodayAsync(client))!.NextLesson!;
            order.Add(next.Title);
            await client.PostAsync($"/api/v1/learning/lessons/{next.Id}/complete", null);
        }

        Assert.Equal(["الأبجدية الإنجليزية", "الأرقام من 0 إلى 20", "التحيات"], order);
    }

    [Fact]
    public async Task Foundations_lessons_are_not_held_to_one_a_day()
    {
        var client = await factory.CreateSignedInClientAsync();
        await Choose(client, knowsLetters: false, startStep: 1);
        var first = (await TodayAsync(client))!.NextLesson!;

        await client.PostAsync($"/api/v1/learning/lessons/{first.Id}/complete", null);
        var today = (await TodayAsync(client))!;

        Assert.False(today.CompletedLessonToday);
        Assert.NotEqual(first.Id, today.NextLesson!.Id);
        Assert.Equal("foundations", today.NextLesson.Track);
    }

    [Fact]
    public async Task A_foundations_lesson_carries_its_items_and_exercises_and_plays_letters_by_style()
    {
        var client = await factory.CreateSignedInClientAsync();
        await Choose(client, knowsLetters: false, startStep: 1);

        // The alphabet is the very first thing a beginner sees.
        var alphabetId = (await TodayAsync(client))!.NextLesson!.Id;

        var lesson = await client.GetFromJsonAsync<Lesson>($"/api/v1/learning/lessons/{alphabetId}", Json);

        Assert.Equal("foundations", lesson!.Track);
        Assert.Null(lesson.GrammarStep);
        Assert.Equal(26, lesson.Words.Length);
        Assert.All(lesson.Words, w => Assert.True(w.IsLetter));
        Assert.Equal("Aa", lesson.Words[0].Symbol);
        Assert.Contains(lesson.Exercises, e => e.AudioStyle == "letter");
        Assert.Contains(lesson.Exercises, e => e.AudioStyle == "sound");
        Assert.All(lesson.Exercises, e => Assert.Equal(1, e.Options.Count(o => o.IsCorrect)));
    }

    [Fact]
    public async Task Finishing_the_foundations_leads_to_the_main_course_and_adds_no_review_words()
    {
        var client = await factory.CreateSignedInClientAsync();
        await Choose(client, knowsLetters: false, startStep: 1);

        var seen = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            var next = (await TodayAsync(client))!.NextLesson;
            if (next is null || next.Track == "main")
            {
                break;
            }

            seen.Add(next.Title);
            var response = await client.PostAsync($"/api/v1/learning/lessons/{next.Id}/complete", null);
            var completion = await response.Content.ReadFromJsonAsync<Completion>(Json);
            Assert.Equal(0, completion!.WordsAdded);
        }

        var today = (await TodayAsync(client))!;
        Assert.Equal(5, seen.Count);
        Assert.Equal("main", today.NextLesson!.Track);
        Assert.Equal(0, today.LearnedWordsCount);
    }

    [Fact]
    public async Task A_learner_who_starts_further_on_skips_lessons_before_that_step_and_is_told_when_none_exist_yet()
    {
        var client = await factory.CreateSignedInClientAsync();
        // The only published lesson teaches step 5, so a learner starting at step 11 has nothing yet.
        await Choose(client, knowsLetters: true, startStep: 11);

        var today = await TodayAsync(client);

        Assert.Null(today!.NextLesson);
        Assert.True(today.NoLessonsAtYourLevelYet);
    }
}
