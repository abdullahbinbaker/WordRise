namespace Wordrise.Modules.ContentAuthoring.Domain;

/// <param name="Number">Position in the course: step 4 may use the grammar of steps 1 to 4 only.</param>
/// <param name="Level">CEFR level the lesson is written for.</param>
public sealed record GrammarPoint(int Number, string Title, string Level);

/// <summary>
/// The grammar sequence agreed in the product plan, A1 to B1. The first steps are ordered by what
/// lets a learner speak soonest (asking and requesting come before the present simple), not by
/// textbook order. New steps go at the end: published lessons refer to steps by number.
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

        // The rest of A2: the gaps every learner meets before the present perfect.
        new(12, "Countable and uncountable nouns: some / any, how much / how many", "A2"),
        new(13, "Comparatives and superlatives", "A2"),
        new(14, "will (offers, decisions, predictions)", "A2"),
        new(15, "have to / must / should", "A2"),

        // B1: the ceiling of the first version of the course.
        new(16, "Present perfect: experience (ever, never, just, already, yet)", "B1"),
        new(17, "Present perfect with for / since, and versus the past simple", "B1"),
        new(18, "Past continuous with while / when", "B1"),
        new(19, "First conditional (if + will)", "B1"),
        new(20, "Second conditional (if + would)", "B1"),
        new(21, "used to", "B1"),
        new(22, "Passive voice (present and past simple)", "B1"),
        new(23, "Relative clauses (who, which, that)", "B1"),
        new(24, "Gerunds and infinitives (like doing, want to do)", "B1"),
        new(25, "Reported speech (simple)", "B1"),
    ];

    public static GrammarPoint? Find(int number) => Steps.FirstOrDefault(s => s.Number == number);

    /// <summary>The grammar a lesson at this step may use: its own point and every earlier one.</summary>
    public static IReadOnlyList<GrammarPoint> UpTo(int number) => [.. Steps.Where(s => s.Number <= number)];
}
