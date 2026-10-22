using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;

namespace Wordrise.Modules.Learning.Tests;

public sealed class LearningApiTests(WordriseApiFactory factory) : IClassFixture<WordriseApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private sealed record GoalResponse(int WordsPerDay, int[] AllowedValues);

    private sealed record NextLesson(Guid Id, int Number, string Title, int NewWordsCount);

    private sealed record TodayResponse(
        int DailyGoal,
        int Streak,
        bool ReviewsEnabled,
        int DueReviewCount,
        int LearnedWordsCount,
        bool CompletedLessonToday,
        bool NewLessonsPaused,
        int? ReviewBacklogLimit,
        NextLesson? NextLesson);

    private sealed record LessonWord(Guid Id, string English, string Arabic);

    private sealed record LessonResponse(Guid Id, string Title, LessonWord[] Words, object[] Dialogue);

    private sealed record CompleteResponse(bool AlreadyCompleted, int WordsAdded, int Streak);

    private sealed record RateResponse(Guid WordId, int IntervalDays, string NextReviewOn, string Status);

    private sealed record MyWord(Guid WordId, string English, string Status);

    private static Task<T?> Get<T>(HttpClient client, string url) => client.GetFromJsonAsync<T>(url, Json);

    private async Task<(HttpClient Client, NextLesson Lesson)> LearnerWithFirstLessonAsync()
    {
        var client = await factory.CreateSignedInClientAsync();
        var today = await Get<TodayResponse>(client, "/api/v1/learning/today");
        return (client, today!.NextLesson!);
    }

    [Fact]
    public async Task Learning_requires_sign_in()
    {
        var response = await factory.CreateHttpsClient().GetAsync("/api/v1/learning/today");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_new_learner_gets_the_first_lesson_and_the_default_goal()
    {
        var client = await factory.CreateSignedInClientAsync();

        var today = await Get<TodayResponse>(client, "/api/v1/learning/today");

        Assert.Equal(10, today!.DailyGoal);
        Assert.Equal(0, today.Streak);
        Assert.Equal(0, today.DueReviewCount);
        Assert.False(today.CompletedLessonToday);
        Assert.False(today.NewLessonsPaused);
        Assert.Equal(30, today.ReviewBacklogLimit);
        Assert.Equal(1, today.NextLesson!.Number);
        Assert.Equal(3, today.NextLesson.NewWordsCount);
    }

    /// <summary>A signed-in client on an app started with extra configuration.</summary>
    private async Task<HttpClient> ConfiguredClientAsync(params (string Key, string Value)[] settings)
    {
        var configured = factory.WithWebHostBuilder(builder =>
        {
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        });
        var client = configured.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await SignInAsync(client);
        return client;
    }

    [Fact]
    public async Task The_review_backlog_limit_comes_from_configuration()
    {
        var client = await ConfiguredClientAsync(("Learning:Reviews:Backlog:Limit", "45"));

        var today = await Get<TodayResponse>(client, "/api/v1/learning/today");

        Assert.Equal(45, today!.ReviewBacklogLimit);
        Assert.False(today.NewLessonsPaused);
    }

    [Fact]
    public async Task With_the_backlog_rule_off_there_is_no_limit()
    {
        var client = await ConfiguredClientAsync(("Learning:Reviews:Backlog:Enabled", "false"));

        var today = await Get<TodayResponse>(client, "/api/v1/learning/today");

        Assert.True(today!.ReviewsEnabled);
        Assert.Null(today.ReviewBacklogLimit);
        Assert.False(today.NewLessonsPaused);
    }

    [Fact]
    public async Task With_reviews_off_nothing_is_due_and_rating_is_refused()
    {
        var client = await ConfiguredClientAsync(("Learning:Reviews:Enabled", "false"));
        var next = (await Get<TodayResponse>(client, "/api/v1/learning/today"))!.NextLesson!;
        await client.PostAsync($"/api/v1/learning/lessons/{next.Id}/complete", null);
        var word = (await Get<MyWord[]>(client, "/api/v1/learning/words"))![0];

        var today = await Get<TodayResponse>(client, "/api/v1/learning/today");
        var due = await Get<object[]>(client, "/api/v1/learning/reviews/due");
        var rate = await client.PostAsJsonAsync($"/api/v1/learning/reviews/{word.WordId}", new { rating = "good" });

        Assert.False(today!.ReviewsEnabled);
        Assert.Equal(0, today.DueReviewCount);
        Assert.Null(today.ReviewBacklogLimit);
        Assert.Equal(3, today.LearnedWordsCount);
        Assert.Empty(due!);
        Assert.Equal(HttpStatusCode.Conflict, rate.StatusCode);
        Assert.Contains("learning.reviews_disabled", await rate.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("Learning:Reviews:Backlog:Limit", "0")]
    [InlineData("Learning:Reviews:MaxPerSession", "0")]
    [InlineData("Learning:Reviews:MaxPerSession", "500")]
    public void An_out_of_range_setting_stops_the_app_at_startup(string key, string value)
    {
        using var misconfigured = factory.WithWebHostBuilder(builder => builder.UseSetting(key, value));

        var error = Assert.ThrowsAny<Exception>(() => misconfigured.CreateClient());

        Assert.Contains(key, error.ToString());
    }

    private static async Task SignInAsync(HttpClient client)
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        (await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = WordriseApiFactory.Password }))
            .EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = WordriseApiFactory.Password });
        var token = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString());
    }

    [Fact]
    public async Task The_daily_goal_accepts_offered_values_and_rejects_others()
    {
        var client = await factory.CreateSignedInClientAsync();

        var accepted = await client.PutAsJsonAsync("/api/v1/learning/goal", new { wordsPerDay = 15 });
        var rejected = await client.PutAsJsonAsync("/api/v1/learning/goal", new { wordsPerDay = 7 });
        var goal = await Get<GoalResponse>(client, "/api/v1/learning/goal");

        Assert.Equal(HttpStatusCode.NoContent, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Contains("learning.daily_goal_invalid", await rejected.Content.ReadAsStringAsync());
        Assert.Equal(15, goal!.WordsPerDay);
        Assert.Equal([5, 10, 15], goal.AllowedValues);
    }

    [Fact]
    public async Task A_lesson_returns_its_words_and_dialogue()
    {
        var (client, next) = await LearnerWithFirstLessonAsync();

        var lesson = await Get<LessonResponse>(client, $"/api/v1/learning/lessons/{next.Id}");

        Assert.Equal("في البقالة", lesson!.Title);
        Assert.Equal(["apple", "milk", "rice"], lesson.Words.Select(w => w.English));
        Assert.Equal(3, lesson.Dialogue.Length);
    }

    [Fact]
    public async Task Unknown_lesson_is_not_found()
    {
        var client = await factory.CreateSignedInClientAsync();

        var response = await client.GetAsync($"/api/v1/learning/lessons/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Completing_a_lesson_adds_its_words_once_and_starts_the_streak()
    {
        var (client, next) = await LearnerWithFirstLessonAsync();

        var first = await client.PostAsync($"/api/v1/learning/lessons/{next.Id}/complete", null);
        var firstBody = await first.Content.ReadFromJsonAsync<CompleteResponse>(Json);
        var second = await client.PostAsync($"/api/v1/learning/lessons/{next.Id}/complete", null);
        var secondBody = await second.Content.ReadFromJsonAsync<CompleteResponse>(Json);
        var today = await Get<TodayResponse>(client, "/api/v1/learning/today");
        var words = await Get<MyWord[]>(client, "/api/v1/learning/words");

        Assert.Equal(new CompleteResponse(false, 3, 1), firstBody);
        Assert.Equal(new CompleteResponse(true, 0, 1), secondBody);
        Assert.True(today!.CompletedLessonToday);
        Assert.Equal(1, today.Streak);
        Assert.Equal(3, today.LearnedWordsCount);
        Assert.Null(today.NextLesson);
        Assert.Equal(3, words!.Length);
        Assert.All(words, w => Assert.Equal("new", w.Status));
    }

    [Fact]
    public async Task A_learner_who_already_set_a_goal_can_complete_a_lesson()
    {
        // Regression: the learner already exists in the database before the first completion.
        var (client, next) = await LearnerWithFirstLessonAsync();
        await client.PutAsJsonAsync("/api/v1/learning/goal", new { wordsPerDay = 15 });

        var response = await client.PostAsync($"/api/v1/learning/lessons/{next.Id}/complete", null);
        var body = await response.Content.ReadFromJsonAsync<CompleteResponse>(Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new CompleteResponse(false, 3, 1), body);
    }

    [Fact]
    public async Task Rating_a_word_schedules_its_next_review()
    {
        var (client, next) = await LearnerWithFirstLessonAsync();
        await client.PostAsync($"/api/v1/learning/lessons/{next.Id}/complete", null);
        var word = (await Get<MyWord[]>(client, "/api/v1/learning/words"))![0];

        var response = await client.PostAsJsonAsync($"/api/v1/learning/reviews/{word.WordId}", new { rating = "good" });
        var rated = await response.Content.ReadFromJsonAsync<RateResponse>(Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, rated!.IntervalDays);
        Assert.Equal("learning", rated.Status);
    }

    [Fact]
    public async Task Rating_a_word_the_learner_never_met_is_not_found()
    {
        var client = await factory.CreateSignedInClientAsync();

        var response = await client.PostAsJsonAsync($"/api/v1/learning/reviews/{Guid.NewGuid()}", new { rating = "easy" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Nothing_is_due_on_the_day_words_are_introduced()
    {
        var (client, next) = await LearnerWithFirstLessonAsync();
        await client.PostAsync($"/api/v1/learning/lessons/{next.Id}/complete", null);

        var due = await Get<object[]>(client, "/api/v1/learning/reviews/due");
        var dueWords = await Get<MyWord[]>(client, "/api/v1/learning/words?filter=due");

        Assert.Empty(due!);
        Assert.Empty(dueWords!);
    }
}
