namespace Wordrise.Modules.Learning.Domain.Learners;

/// <summary>A place in the main course a learner who already knows some English can start from.</summary>
/// <param name="Step">The grammar curriculum step it begins at (see <c>GrammarCurriculum</c> in content authoring).</param>
/// <param name="Title">What the learner can already do before this point, in the learner's language.</param>
/// <param name="Example">A sentence from the topics it starts with.</param>
public sealed record StartingPoint(int Step, string Title, string Example);

/// <summary>
/// The starting points a learner chooses from instead of a placement test (which comes once there are
/// enough lessons). Each step must exist in the grammar curriculum: a test keeps the two in step.
/// </summary>
public static class StartingPoints
{
    public static IReadOnlyList<StartingPoint> All { get; } =
    [
        new(1, "من البداية: أعرّف بنفسي وأتحدث عن الأشخاص", "I am Ali. She is my sister."),
        new(7, "أعرف الجمل البسيطة: أريد الحديث عن حياتي اليومية", "I work in an office. Do you like tea?"),
        new(11, "أحتاج الماضي والمستقبل", "I went to the market. I am going to travel."),
        new(17, "أريد أن أروي ما كان يحدث وأضع شروطاً", "I was walking when it rained. If it rains, I will stay home."),
        new(21, "أريد الحديث عن خبراتي وما أنجزتُه", "I have never been to London."),
        new(24, "لغتي جيدة: أريد المستوى المتوسط", "If I had more time, I would learn another language."),
    ];

    public static bool IsValid(int step) => All.Any(point => point.Step == step);
}
