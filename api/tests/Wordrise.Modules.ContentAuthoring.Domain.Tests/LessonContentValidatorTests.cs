using Wordrise.Modules.ContentAuthoring.Domain;
using Wordrise.Modules.ContentAuthoring.TestSupport;

namespace Wordrise.Modules.ContentAuthoring.Domain.Tests;

public sealed class LessonContentValidatorTests
{
    private static readonly LessonBrief Brief = ContentFixtures.Brief(words: 3);

    private static IReadOnlyList<ContentProblem> Validate(DraftContent content) =>
        LessonContentValidator.Validate(Brief, content);

    private static DraftContent With(
        Func<DraftContent, DraftContent> change) => change(ContentFixtures.ValidContent(Brief));

    /// <summary>A copy of the lesson with one part replaced.</summary>
    private static DraftContent Copy(
        DraftContent c,
        string? title = null,
        List<DraftWord>? words = null,
        List<DraftDialogueLine>? dialogue = null,
        List<DraftAnswerOption>? chatOptions = null,
        DraftGrammar? grammar = null,
        string? listeningSentence = null,
        List<DraftAnswerOption>? listeningOptions = null) => new()
    {
        Title = title ?? c.Title,
        Words = words ?? c.Words,
        Dialogue = dialogue ?? c.Dialogue,
        ChatPrompt = c.ChatPrompt,
        ChatOptions = chatOptions ?? c.ChatOptions,
        Grammar = grammar ?? c.Grammar,
        ListeningSentence = listeningSentence ?? c.ListeningSentence,
        ListeningOptions = listeningOptions ?? c.ListeningOptions,
    };

    [Fact]
    public void A_complete_lesson_has_no_problems()
    {
        Assert.Empty(Validate(ContentFixtures.ValidContent(Brief)));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void A_complete_lesson_passes_for_any_allowed_word_count(int words)
    {
        var brief = ContentFixtures.Brief(words: words);

        Assert.Empty(LessonContentValidator.Validate(brief, ContentFixtures.ValidContent(brief)));
    }

    [Fact]
    public void The_lesson_must_teach_exactly_the_requested_number_of_words()
    {
        var content = With(c => Copy(c, words: [.. c.Words.Take(2)]));

        Assert.Contains(Validate(content), p => p.Where == "words");
    }

    [Fact]
    public void Every_new_word_must_be_used_in_the_dialogue()
    {
        var content = With(c => Copy(c, dialogue: [.. c.Dialogue.Where(d => !d.English.Contains("rice"))
            .Select(d => d.English.Contains("milk")
                ? new DraftDialogueLine { Speaker = d.Speaker, IsLearner = d.IsLearner, English = "Can I have apple and milk, please?", Arabic = d.Arabic }
                : d)]));

        var problem = Assert.Single(Validate(content), p => p.Where == "word: rice");
        Assert.Contains("dialogue", problem.Message);
    }

    [Fact]
    public void A_word_in_the_dialogue_may_carry_an_ending()
    {
        var content = With(c => Copy(c, dialogue:
        [
            new DraftDialogueLine { Speaker = "A", English = "Hello!", Arabic = "مرحبا" },
            new DraftDialogueLine { Speaker = "B", IsLearner = true, English = "I want apples, milk and rice.", Arabic = "أريد" },
            new DraftDialogueLine { Speaker = "A", English = "Sure.", Arabic = "حسناً" },
        ]));

        Assert.Empty(Validate(content));
    }

    [Fact]
    public void English_where_arabic_belongs_is_caught()
    {
        var words = ContentFixtures.ValidContent(Brief).Words;
        words[0] = new DraftWord { English = "apple", Arabic = "apple", Pronunciation = "نُطق", Example = "I like apple.", ExampleArabic = "أحب" };

        var problem = Assert.Single(Validate(With(c => Copy(c, words: words))));

        Assert.Equal("word: apple (Arabic meaning)", problem.Where);
        Assert.Contains("Arabic", problem.Message);
    }

    [Fact]
    public void Arabic_where_english_belongs_is_caught()
    {
        var content = With(c => Copy(c, listeningSentence: "سأشتري تفاحاً"));

        Assert.Contains(Validate(content), p => p.Where == "listening sentence" && p.Message.Contains("English"));
    }

    [Fact]
    public void An_empty_title_is_caught()
    {
        Assert.Contains(Validate(With(c => Copy(c, title: " "))), p => p.Where == "title");
    }

    [Fact]
    public void The_same_word_listed_twice_is_caught()
    {
        var words = ContentFixtures.ValidContent(Brief).Words;
        words[2] = words[0];

        Assert.Contains(Validate(With(c => Copy(c, words: words))), p => p.Message.Contains("twice"));
    }

    [Fact]
    public void The_dialogue_needs_the_learner_to_speak()
    {
        var content = With(c => Copy(c, dialogue: [.. c.Dialogue.Select(d => new DraftDialogueLine { Speaker = d.Speaker, English = d.English, Arabic = d.Arabic })]));

        Assert.Contains(Validate(content), p => p.Where == "dialogue" && p.Message.Contains("learner never speaks"));
    }

    [Fact]
    public void The_dialogue_needs_someone_to_talk_to()
    {
        var content = With(c => Copy(c, dialogue: [.. c.Dialogue.Select(d => new DraftDialogueLine { Speaker = d.Speaker, IsLearner = true, English = d.English, Arabic = d.Arabic })]));

        Assert.Contains(Validate(content), p => p.Message.Contains("someone to talk to"));
    }

    [Fact]
    public void A_dialogue_that_is_too_short_is_caught()
    {
        var content = With(c => Copy(c, dialogue: [.. c.Dialogue.Take(1)]));

        Assert.Contains(Validate(content), p => p.Where == "dialogue" && p.Message.Contains("lines"));
    }

    [Fact]
    public void Two_correct_answers_are_caught()
    {
        var content = With(c => Copy(c, chatOptions:
        [
            new DraftAnswerOption { Text = "Here you go.", IsCorrect = true },
            new DraftAnswerOption { Text = "Thank you.", IsCorrect = true },
        ]));

        Assert.Contains(Validate(content), p => p.Where == "dialogue question" && p.Message.Contains("exactly one correct"));
    }

    [Fact]
    public void No_correct_answer_is_caught()
    {
        var content = With(c => Copy(c, chatOptions: [new DraftAnswerOption { Text = "a" }, new DraftAnswerOption { Text = "b" }]));

        Assert.Contains(Validate(content), p => p.Message.Contains("exactly one correct"));
    }

    [Fact]
    public void A_listening_answer_that_is_not_in_the_sentence_is_caught()
    {
        var content = With(c => Copy(c, listeningSentence: "I can buy fish."));

        Assert.Contains(Validate(content), p => p.Where == "listening exercise" && p.Message.Contains("not in the sentence"));
    }

    [Fact]
    public void A_listening_exercise_needs_enough_options()
    {
        var content = With(c => Copy(c, listeningOptions: [.. c.ListeningOptions.Take(2)]));

        Assert.Contains(Validate(content), p => p.Where == "listening exercise" && p.Message.Contains("answer options"));
    }

    [Fact]
    public void Repeated_options_are_caught()
    {
        var content = With(c => Copy(c, chatOptions:
        [
            new DraftAnswerOption { Text = "Here you go.", IsCorrect = true },
            new DraftAnswerOption { Text = "here you go." },
        ]));

        Assert.Contains(Validate(content), p => p.Message.Contains("twice"));
    }

    [Fact]
    public void The_grammar_point_needs_examples()
    {
        var grammar = ContentFixtures.ValidContent(Brief).Grammar;
        var thin = new DraftGrammar
        {
            Title = grammar.Title,
            Meaning = grammar.Meaning,
            Explanation = grammar.Explanation,
            Pattern = grammar.Pattern,
            Examples = [.. grammar.Examples.Take(1)],
        };

        Assert.Contains(Validate(With(c => Copy(c, grammar: thin))), p => p.Where == "grammar" && p.Message.Contains("examples"));
    }

    [Fact]
    public void Several_problems_are_all_reported_at_once()
    {
        var content = With(c => Copy(c, title: "", listeningSentence: "I can buy fish.", words: [.. c.Words.Take(2)]));

        Assert.True(Validate(content).Count >= 3);
    }
}
