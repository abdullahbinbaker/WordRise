namespace Wordrise.Modules.ContentAuthoring.Domain;

/// <param name="Number">Position in the course: step 4 may use the grammar of steps 1 to 4 only.</param>
/// <param name="Level">CEFR level the lesson is written for.</param>
public sealed record GrammarPoint(int Number, string Title, string Level);

/// <summary>
/// The beginner grammar sequence agreed in the product plan, ordered by what lets a learner
/// speak soonest (asking and requesting come before the present simple), not by textbook order.
/// </summary>
public static class GrammarCurriculum
{
    public static IReadOnlyList<GrammarPoint> Steps { get; } =
    [
        new(1, "to be (I am, you are, he is)", "A1"),
        new(2, "a / an, this / that, plurals", "A1"),
        new(3, "There is / There are, Where is...?", "A1"),
        new(4, "can / can't", "A1"),
        new(5, "would like", "A1"),
        new(6, "Present simple", "A1"),
        new(7, "do / does questions", "A1"),
        new(8, "Present continuous", "A1"),
        new(9, "Imperatives and prepositions of place", "A1"),
        new(10, "Past simple", "A2"),
        new(11, "going to (future)", "A2"),
    ];

    public static GrammarPoint? Find(int number) => Steps.FirstOrDefault(s => s.Number == number);

    /// <summary>The grammar a lesson at this step may use: its own point and every earlier one.</summary>
    public static IReadOnlyList<GrammarPoint> UpTo(int number) => [.. Steps.Where(s => s.Number <= number)];
}
