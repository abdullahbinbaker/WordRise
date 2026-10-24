using Wordrise.Modules.ContentAuthoring.Domain;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.ContentAuthoring.Domain.Tests;

public sealed class LessonBriefTests
{
    [Fact]
    public void A_brief_keeps_the_managers_words_with_tidy_spacing()
    {
        var brief = LessonBrief.Create("  at   the airport ", 5, 6);

        Assert.Equal("at the airport", brief.Scenario);
        Assert.Equal(5, brief.GrammarStep);
        Assert.Equal(6, brief.WordCount);
    }

    [Fact]
    public void The_word_count_defaults_when_not_given()
    {
        Assert.Equal(LessonBrief.DefaultWords, LessonBrief.Create("hotel", 3, null).WordCount);
    }

    [Fact]
    public void A_lesson_may_use_its_own_grammar_and_every_earlier_point()
    {
        var brief = LessonBrief.Create("restaurant", 5, 5);

        Assert.Equal("can / can't", brief.Grammar.Title);
        Assert.Equal(5, brief.AllowedGrammar.Count);
        Assert.Equal(GrammarCurriculum.Steps[0].Title, brief.AllowedGrammar[0]);
        Assert.DoesNotContain("Present simple", brief.AllowedGrammar);
    }

    [Theory]
    [InlineData(1, "A1")]
    [InlineData(10, "A1")]
    [InlineData(11, "A2")]
    [InlineData(22, "A2")]
    [InlineData(23, "B1")]
    [InlineData(34, "B1")]
    public void The_level_follows_the_grammar_step(int step, string level)
    {
        Assert.Equal(level, LessonBrief.Create("hotel", step, 5).Level);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a")]
    public void A_scenario_that_says_nothing_is_refused(string? scenario)
    {
        var error = Assert.Throws<BusinessRuleException>(() => LessonBrief.Create(scenario, 4, 5));

        Assert.Equal(AuthoringErrors.BriefInvalid, error.Code);
    }

    [Fact]
    public void A_very_long_scenario_is_refused()
    {
        Assert.Throws<BusinessRuleException>(() => LessonBrief.Create(new string('x', LessonBrief.MaxScenarioLength + 1), 4, 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(35)]
    [InlineData(-1)]
    public void A_grammar_step_outside_the_curriculum_is_refused(int step)
    {
        Assert.Throws<BusinessRuleException>(() => LessonBrief.Create("hotel", step, 5));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(11)]
    [InlineData(0)]
    public void A_word_count_outside_the_limits_is_refused(int words)
    {
        Assert.Throws<BusinessRuleException>(() => LessonBrief.Create("hotel", 4, words));
    }
}
