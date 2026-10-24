namespace Wordrise.Modules.ContentAuthoring.Domain;

/// <param name="Number">Position in the course: step 4 may use the grammar of steps 1 to 4 only.</param>
/// <param name="Level">CEFR level the lesson is written for.</param>
public sealed record GrammarPoint(int Number, string Title, string Level);

/// <summary>
/// The grammar sequence, A1 to B1. Levels were checked against the British Council / EAQUALS Core
/// Inventory for General English (2011), an indicative reference, not the last word: the order inside a
/// level follows what lets a learner speak soonest, and the whole list should be reviewed by a
/// qualified English teacher before many lessons are written. New steps may be inserted while no lesson
/// is published; once lessons exist they refer to steps by number, so change the order with care.
///
/// This list is about grammar. Learners who do not yet know the Latin alphabet need a separate
/// "foundations" track first (letters, sounds, numbers, greetings); see the README.
/// </summary>
public static class GrammarCurriculum
{
    public static IReadOnlyList<GrammarPoint> Steps { get; } =
    [
        // A1: the first ten steps.
        new(1, "to be with every subject pronoun (I am, you are, he / she / it is, we / they are)", "A1"),
        new(2, "Possessive adjectives and possessive 's (my, your, his, her, our, their; Ali's)", "A1"),
        new(3, "a / an, this / that, plurals", "A1"),
        new(4, "There is / There are, Where is...?", "A1"),
        new(5, "can / can't", "A1"),
        new(6, "would like", "A1"),
        new(7, "Present simple (with adverbs of frequency: always, usually, never)", "A1"),
        new(8, "do / does questions", "A1"),
        new(9, "Present continuous", "A1"),
        new(10, "Imperatives and prepositions of place and time (in, on, at)", "A1"),

        // A2.
        new(11, "Past simple (including was / were)", "A2"),
        new(12, "going to (future)", "A2"),
        new(13, "Countable and uncountable nouns: some / any, how much / how many", "A2"),
        new(14, "Comparatives and superlatives", "A2"),
        new(15, "will (offers, decisions, predictions)", "A2"),
        new(16, "have to / must / should", "A2"),
        new(17, "Past continuous with while / when", "A2"),
        new(18, "Gerunds and infinitives (like doing, want to do)", "A2"),
        new(19, "Zero and first conditional (if + present, will)", "A2"),
        new(20, "Present continuous for future arrangements", "A2"),
        new(21, "Present perfect: experience (ever, never, just, already, yet)", "A2"),
        new(22, "Common phrasal verbs", "A2"),

        // B1: the ceiling of the first version of the course.
        new(23, "Present perfect with for / since, and versus the past simple", "B1"),
        new(24, "Second conditional (if + would)", "B1"),
        new(25, "Connecting words (because, so, although, however)", "B1"),
        new(26, "Modals of probability and deduction (might, may, must, can't)", "B1"),
        new(27, "Past perfect", "B1"),
        new(28, "Passive voice (present and past simple)", "B1"),
        new(29, "Reported speech (simple)", "B1"),
        new(30, "too / enough", "B1"),
        new(31, "Question tags", "B1"),
        new(32, "Present perfect continuous", "B1"),

        // Placed at the end of B1 and not confirmed: the Core Inventory does not list "used to" below B2
        // and puts relative clauses at B2. Check them (English Grammar Profile, a teacher) before writing lessons.
        new(33, "used to", "B1"),
        new(34, "Relative clauses (who, which, that)", "B1"),
    ];

    public static GrammarPoint? Find(int number) => Steps.FirstOrDefault(s => s.Number == number);

    /// <summary>The grammar a lesson at this step may use: its own point and every earlier one.</summary>
    public static IReadOnlyList<GrammarPoint> UpTo(int number) => [.. Steps.Where(s => s.Number <= number)];
}
