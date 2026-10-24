using Wordrise.Modules.ContentAuthoring.Domain;

namespace Wordrise.Modules.ContentAuthoring.Domain.Tests;

public sealed class GrammarCurriculumTests
{
    private static readonly string[] LevelOrder = ["A1", "A2", "B1", "B2"];

    [Fact]
    public void Steps_are_numbered_from_one_without_gaps()
    {
        Assert.Equal(Enumerable.Range(1, GrammarCurriculum.Steps.Count), GrammarCurriculum.Steps.Select(s => s.Number));
    }

    [Fact]
    public void The_course_never_goes_back_to_an_easier_level()
    {
        var levels = GrammarCurriculum.Steps.Select(s => Array.IndexOf(LevelOrder, s.Level)).ToList();

        Assert.DoesNotContain(-1, levels);
        Assert.Equal(levels.Order(), levels);
    }

    [Fact]
    public void The_first_version_of_the_course_reaches_B1_and_stops_there()
    {
        Assert.Equal("A1", GrammarCurriculum.Steps[0].Level);
        Assert.Equal("B1", GrammarCurriculum.Steps[^1].Level);
    }

    [Fact]
    public void The_first_step_teaches_every_subject_pronoun_not_just_three()
    {
        var title = GrammarCurriculum.Steps[0].Title;

        foreach (var form in new[] { "I am", "you are", "he", "she", "it", "we", "they" })
        {
            Assert.Contains(form, title);
        }
    }

    [Fact]
    public void Possessives_come_early_because_introductions_need_them()
    {
        var possessives = GrammarCurriculum.Steps.Single(s => s.Title.StartsWith("Possessive", StringComparison.Ordinal));

        Assert.True(possessives.Number <= 3);
        Assert.Equal("A1", possessives.Level);
    }

    [Fact]
    public void Titles_are_unique()
    {
        Assert.Equal(GrammarCurriculum.Steps.Count, GrammarCurriculum.Steps.Select(s => s.Title).Distinct().Count());
    }

    [Fact]
    public void A_lesson_may_use_its_own_grammar_and_every_earlier_one()
    {
        Assert.Equal(21, GrammarCurriculum.UpTo(21).Count);
        Assert.Equal("Present perfect: experience (ever, never, just, already, yet)", GrammarCurriculum.UpTo(21)[^1].Title);
    }
}
