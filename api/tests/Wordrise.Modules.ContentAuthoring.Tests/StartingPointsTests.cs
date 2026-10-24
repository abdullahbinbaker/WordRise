using Wordrise.Modules.ContentAuthoring.Domain;
using Wordrise.Modules.Learning.Domain.Learners;

namespace Wordrise.Modules.ContentAuthoring.Tests;

/// <summary>The learner's starting points are written in the Learning module but name curriculum steps owned here.</summary>
public sealed class StartingPointsTests
{
    [Fact]
    public void Every_starting_point_names_a_step_that_exists_in_the_curriculum()
    {
        foreach (var point in StartingPoints.All)
        {
            Assert.NotNull(GrammarCurriculum.Find(point.Step));
        }
    }

    [Fact]
    public void The_starting_points_cover_the_course_from_the_first_step_to_B1()
    {
        var levels = StartingPoints.All.Select(p => GrammarCurriculum.Find(p.Step)!.Level).ToList();

        Assert.Equal("A1", levels[0]);
        Assert.Contains("A2", levels);
        Assert.Contains("B1", levels);
    }
}
