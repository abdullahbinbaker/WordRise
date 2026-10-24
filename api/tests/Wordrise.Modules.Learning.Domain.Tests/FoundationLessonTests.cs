using Wordrise.Modules.Learning.Domain;
using Wordrise.Modules.Learning.Domain.Learners;
using Wordrise.Modules.Learning.Domain.Lessons;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Domain.Tests;

public sealed class FoundationLessonTests
{
    private static LessonWord Letter(char name = 'A', string ipa = "æ") => new()
    {
        Id = WordIdentity.For("en", name.ToString()),
        Target = name.ToString(),
        Symbol = $"{name}{char.ToLowerInvariant(name)}",
        IsLetter = true,
        Ipa = ipa,
        Native = "إيه",
    };

    private static FoundationExercise Exercise(params bool[] correct) => new()
    {
        Prompt = "استمع ثم اختر",
        AudioText = "A",
        AudioStyle = SpokenStyle.Letter,
        Options = [.. correct.Select((isCorrect, i) => new AnswerOption { Text = $"option {i}", IsCorrect = isCorrect })],
    };

    private static LessonContent Valid() => new()
    {
        Words = [Letter('A'), Letter('B', "b")],
        Exercises = [Exercise(true, false, false)],
    };

    private static Lesson Create(LessonContent content, int? grammarStep = null) =>
        Lesson.Create(Guid.NewGuid(), 1, LessonTrack.Foundations, grammarStep, "en", "ar", "الأبجدية", string.Empty, string.Empty, content);

    [Fact]
    public void A_foundations_lesson_needs_no_dialogue_grammar_or_scenario()
    {
        var lesson = Create(Valid());

        Assert.Equal(LessonTrack.Foundations, lesson.Track);
        Assert.Empty(lesson.Content.Dialogue);
    }

    public static TheoryData<string, Func<LessonContent>> BrokenLessons() => new()
    {
        { "no items", () => new LessonContent { Exercises = [Exercise(true, false)] } },
        { "no exercises", () => new LessonContent { Words = [Letter()] } },
        { "the same item twice", () => new LessonContent { Words = [Letter(), Letter()], Exercises = [Exercise(true, false)] } },
        { "a letter with no sound", () => new LessonContent { Words = [Letter('A', string.Empty)], Exercises = [Exercise(true, false)] } },
        { "a letter that is two characters", () => new LessonContent { Words = [new LessonWord { Id = Guid.NewGuid(), Target = "AB", IsLetter = true, Ipa = "æ" }], Exercises = [Exercise(true, false)] } },
        { "an exercise with two correct answers", () => new LessonContent { Words = [Letter()], Exercises = [Exercise(true, true)] } },
        { "an exercise with no correct answer", () => new LessonContent { Words = [Letter()], Exercises = [Exercise(false, false)] } },
        { "an exercise with one option", () => new LessonContent { Words = [Letter()], Exercises = [Exercise(true)] } },
    };

    [Theory]
    [MemberData(nameof(BrokenLessons))]
    public void A_foundations_lesson_that_would_break_a_screen_is_refused(string reason, Func<LessonContent> content)
    {
        var error = Assert.Throws<BusinessRuleException>(() => Create(content()));

        Assert.Equal(LearningErrors.LessonInvalid, error.Code);
        Assert.False(string.IsNullOrWhiteSpace(reason));
    }

    [Fact]
    public void An_exercise_that_plays_a_sound_must_say_how_the_sound_is_written()
    {
        var exercise = new FoundationExercise
        {
            Prompt = "استمع",
            AudioText = "a",
            AudioStyle = SpokenStyle.Sound,
            Options = [new AnswerOption { Text = "A", IsCorrect = true }, new AnswerOption { Text = "O" }],
        };

        Assert.Throws<BusinessRuleException>(() => Create(new LessonContent { Words = [Letter()], Exercises = [exercise] }));
    }

    [Fact]
    public void An_exercise_must_show_or_play_something()
    {
        var exercise = new FoundationExercise
        {
            Prompt = "اختر",
            Options = [new AnswerOption { Text = "A", IsCorrect = true }, new AnswerOption { Text = "O" }],
        };

        Assert.Throws<BusinessRuleException>(() => Create(new LessonContent { Words = [Letter()], Exercises = [exercise] }));
    }

    [Fact]
    public void A_foundations_lesson_does_not_belong_to_a_grammar_step()
    {
        Assert.Throws<BusinessRuleException>(() => Create(Valid(), grammarStep: 3));
    }

    [Fact]
    public void A_main_lesson_may_not_start_its_grammar_step_below_one()
    {
        var content = new LessonContent
        {
            Words = [new LessonWord { Id = WordIdentity.For("en", "milk"), Target = "milk", Native = "حليب" }],
            Dialogue = [new DialogueLine { Speaker = "A", Target = "Hi", Native = "مرحبا" }],
            ChatOptions = [new AnswerOption { Text = "Yes", IsCorrect = true }, new AnswerOption { Text = "No" }],
            ListeningOptions = [new AnswerOption { Text = "milk", IsCorrect = true }, new AnswerOption { Text = "no" }],
        };

        Assert.Throws<BusinessRuleException>(
            () => Lesson.Create(Guid.NewGuid(), 1, LessonTrack.Main, 0, "en", "ar", "t", "s", "g", content));
    }
}

public sealed class OnboardingTests
{
    [Fact]
    public void A_new_learner_is_assumed_to_know_the_letters_and_to_start_at_the_beginning()
    {
        var learner = Learner.Start(Guid.NewGuid());

        Assert.True(learner.KnowsLetters);
        Assert.Equal(1, learner.StartStep);
        Assert.False(learner.OnboardingDone);
    }

    [Fact]
    public void The_learner_choice_is_saved()
    {
        var learner = Learner.Start(Guid.NewGuid());

        learner.ChooseStart(knowsLetters: false, startStep: 11);

        Assert.False(learner.KnowsLetters);
        Assert.Equal(11, learner.StartStep);
        Assert.True(learner.OnboardingDone);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(99)]
    public void Only_the_offered_starting_points_are_accepted(int step)
    {
        var learner = Learner.Start(Guid.NewGuid());

        var error = Assert.Throws<BusinessRuleException>(() => learner.ChooseStart(true, step));

        Assert.Equal(LearningErrors.InvalidStartingPoint, error.Code);
    }

    [Fact]
    public void The_starting_points_begin_at_step_one_and_only_go_forward()
    {
        Assert.Equal(1, StartingPoints.All[0].Step);
        Assert.Equal(StartingPoints.All.Select(p => p.Step).Order(), StartingPoints.All.Select(p => p.Step));
        Assert.All(StartingPoints.All, p => Assert.False(string.IsNullOrWhiteSpace(p.Title)));
    }
}
