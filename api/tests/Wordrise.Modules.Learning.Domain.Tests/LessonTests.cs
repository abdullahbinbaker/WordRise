using Wordrise.Modules.Learning.Domain;
using Wordrise.Modules.Learning.Domain.Lessons;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Domain.Tests;

public sealed class LessonTests
{
    private static LessonWord Word(string english) =>
        new() { Id = WordIdentity.For(english), English = english, Arabic = "معنى" };

    private static LessonContent ValidContent() => new()
    {
        Words = [Word("apple"), Word("milk")],
        Dialogue = [new DialogueLine { Speaker = "A", English = "Hi", Arabic = "مرحبا" }],
        ChatOptions = [new AnswerOption { Text = "Yes", IsCorrect = true }, new AnswerOption { Text = "No" }],
        ListeningOptions = [new AnswerOption { Text = "apple", IsCorrect = true }, new AnswerOption { Text = "milk" }],
    };

    private static Lesson Create(LessonContent content, string title = "في البقالة") =>
        Lesson.Create(Guid.NewGuid(), 1, title, "grocery", "can / can't", content);

    [Fact]
    public void A_complete_lesson_is_created()
    {
        var lesson = Create(ValidContent(), title: "  في البقالة  ");

        Assert.Equal("في البقالة", lesson.Title);
        Assert.Equal(2, lesson.Content.Words.Count);
    }

    [Fact]
    public void A_word_has_the_same_id_wherever_it_is_taught()
    {
        Assert.Equal(WordIdentity.For("Milk"), WordIdentity.For("  milk "));
        Assert.Equal(WordIdentity.For("thank you"), WordIdentity.For("Thank   YOU"));
        Assert.NotEqual(WordIdentity.For("milk"), WordIdentity.For("rice"));
    }

    public static TheoryData<string, Func<LessonContent>> BrokenLessons() => new()
    {
        { "no words", () => new LessonContent { Words = [], Dialogue = ValidContent().Dialogue, ChatOptions = ValidContent().ChatOptions, ListeningOptions = ValidContent().ListeningOptions } },
        { "a repeated word", () => new LessonContent { Words = [Word("milk"), Word("Milk")], Dialogue = ValidContent().Dialogue, ChatOptions = ValidContent().ChatOptions, ListeningOptions = ValidContent().ListeningOptions } },
        { "no dialogue", () => new LessonContent { Words = ValidContent().Words, Dialogue = [], ChatOptions = ValidContent().ChatOptions, ListeningOptions = ValidContent().ListeningOptions } },
        { "no correct chat answer", () => new LessonContent { Words = ValidContent().Words, Dialogue = ValidContent().Dialogue, ChatOptions = [new AnswerOption { Text = "a" }, new AnswerOption { Text = "b" }], ListeningOptions = ValidContent().ListeningOptions } },
        { "two correct listening answers", () => new LessonContent { Words = ValidContent().Words, Dialogue = ValidContent().Dialogue, ChatOptions = ValidContent().ChatOptions, ListeningOptions = [new AnswerOption { Text = "a", IsCorrect = true }, new AnswerOption { Text = "b", IsCorrect = true }] } },
        { "a single listening option", () => new LessonContent { Words = ValidContent().Words, Dialogue = ValidContent().Dialogue, ChatOptions = ValidContent().ChatOptions, ListeningOptions = [new AnswerOption { Text = "a", IsCorrect = true }] } },
    };

    [Theory]
    [MemberData(nameof(BrokenLessons))]
    public void A_lesson_a_learner_could_not_complete_is_refused(string reason, Func<LessonContent> content)
    {
        var error = Assert.Throws<BusinessRuleException>(() => Create(content()));

        Assert.True(error.Code == LearningErrors.LessonInvalid, reason);
    }

    [Fact]
    public void A_lesson_without_a_title_is_refused()
    {
        var error = Assert.Throws<BusinessRuleException>(() => Create(ValidContent(), title: " "));

        Assert.Equal(LearningErrors.LessonInvalid, error.Code);
    }
}
