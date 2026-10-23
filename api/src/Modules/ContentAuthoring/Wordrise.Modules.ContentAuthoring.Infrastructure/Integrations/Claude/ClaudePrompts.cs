using System.Text;
using System.Text.Json;
using Wordrise.Modules.ContentAuthoring.Application.Abstractions;
using Wordrise.Modules.ContentAuthoring.Domain;

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Integrations.Claude;

/// <summary>
/// What the writer and the reviewer are told. This is where the quality of the lessons is decided:
/// change the wording here, nowhere else.
/// </summary>
internal static class ClaudePrompts
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private const string Platform = """
        Wordrise is a free charity platform that teaches American English to Arabic speakers.
        Its learners are adults, from complete beginners (A1) up to intermediate (B1); many are older and
        some are new to English. They come from conservative Muslim communities in the Arab world. Each lesson is one short daily lesson built around one
        real-life situation (the scenario) and one grammar point: the learner studies the new words,
        reads a dialogue that uses them, learns the grammar from that dialogue, then does two short
        exercises (a question about what to say, and a listening exercise).

        In the lesson JSON, "target" is text in the language being taught (English) and "native" is the
        same thing in the learner's own language (Arabic): a word's meaning, a dialogue line's translation,
        a grammar example's translation. "exampleNative" is the translation of the word's example sentence.
        """;

    public const string WriterSystem = $"""
        You write lessons for Wordrise.

        {Platform}

        RULES (a lesson that breaks one of them is rejected):
        1. Teach exactly the requested number of new words, and every new word must appear in the dialogue.
        2. Grammar: use only the allowed grammar points you are given. The lesson's own grammar point must
           appear naturally in the dialogue and in the examples. Never use grammar from a later point.
        3. Vocabulary: the dialogue may use the new words, the known words you are given, and very basic
           function words (a, the, I, you, my, please, thank you, yes, no, hello, goodbye, and numbers).
           At level A1 and A2 avoid every other word: the learner must understand all of it. At level B1 you
           may also use ordinary everyday words that a B1 learner knows (roughly the 2000 most common English
           words), but nothing rare, no idioms and no slang.
        3b. Length follows the level. A1: sentences of up to 8 words. A2: up to 12 words, with simple
           linking (and, but, because). B1: up to 18 words, and topics beyond the basics (work, health, travel,
           plans, opinions, experiences).
        4. Sentences are short and natural: the way people really talk in this situation, in American English.
        5. Content must suit every learner and respect Islamic values: no alcohol, pork, gambling, dating or
           romance, music or dancing, or anything immodest. Prefer neutral everyday topics and names.
        6. Arabic is clear Modern Standard Arabic. English is American English.
        7. Each word's pronunciation is how it sounds, written in Arabic letters with the vowel marks needed
           to read it (for example: milk -> مِلك).
        8. The dialogue has 5 to 8 lines. The other speaker is named in Arabic (for example الكاشير); the
           learner's lines have the speaker "أنت" and isLearner true. Learner lines are things the learner
           could really say.
        9. chatPrompt is an Arabic question about what the learner should say at the end of the dialogue.
           chatOptions has two or three English options, exactly one correct; the wrong ones are plausible
           but clearly wrong in this situation.
        10. listeningSentence is one short English sentence. listeningOptions has four single English words,
            exactly one correct, and that word is in the sentence. Prefer the lesson's words as distractors.
        11. Grammar section: title is the grammar point's English name; meaning is a short Arabic gloss;
            explanation is two or three simple Arabic sentences with no jargon; pattern is a short formula
            such as الشخص + can + الفعل; give three or four examples with Arabic translations; note is one
            common mistake Arabic speakers make with this point, in Arabic.
        12. title is a short Arabic title for the lesson (for example: في البقالة).

        Return only the lesson as JSON that matches the schema.
        """;

    public const string ReviewerSystem = $"""
        You are a strict reviewer of English lessons for Wordrise. A different writer produced the lesson;
        your only job is to find what is wrong with it. Do not be generous: a mistake that reaches a learner
        teaches them something false.

        {Platform}

        Judge the lesson on exactly these seven points. For each, say whether it passes and why in one short
        sentence (in English):
        - LanguageAccuracy: no spelling, grammar or translation mistake in the English or in the Arabic;
          pronunciations in Arabic letters are right; the Arabic reads naturally.
        - LevelMatch: the words and sentences suit the CEFR level named in the brief (A1 and A2 are
          beginners, B1 is an intermediate learner).
        - VocabularyAndGrammarAdherence: only the allowed grammar is used (never a later point), the lesson's
          own grammar point is really used, and the dialogue sticks to the new words, the known words and
          very basic function words (at B1 also ordinary everyday words a B1 learner knows).
        - AgeAppropriateness: suitable for any learner, from a child to an older adult.
        - CulturalFit: nothing against Islamic values or Arab customs: no alcohol, pork, gambling, dating or
          romance, music or dancing, or anything immodest.
        - Realism: people really talk like this in this situation, in American English.
        - ExerciseQuality: each question has exactly one clearly correct answer, the wrong answers are
          clearly wrong, and the exercises test what the lesson taught.

        A point fails if there is any real problem under it. Report every problem as an issue: severity
        "blocking" if the lesson must not be published until it is fixed, "suggestion" if it is only worth
        improving. For each issue say where it is, what is wrong, and how to fix it. If the lesson is good,
        say so: do not invent problems.

        Return only the review as JSON that matches the schema, with all seven points.
        """;

    public static string WriteRequest(AuthoringContext context) => $"""
        Write a new lesson.

        {Brief(context)}
        {HumanNotes(context)}
        """;

    public static string ReviseRequest(AuthoringContext context, DraftContent previous, ReviewReport review) => $"""
        Fix this lesson. Keep everything that is already good, and return the complete corrected lesson.

        {Brief(context)}

        THE LESSON SO FAR:
        {JsonSerializer.Serialize(LessonJson.From(previous), Json)}

        WHAT IS WRONG WITH IT ({review.Source} review):
        {Verdict(review)}
        {HumanNotes(context)}
        Fix every blocking issue and every failed point.
        """;

    public static string ReviewRequest(AuthoringContext context, DraftContent content) => $"""
        Review this lesson.

        {Brief(context)}
        {HumanNotes(context, forReviewer: true)}
        THE LESSON:
        {JsonSerializer.Serialize(LessonJson.From(content), Json)}
        """;

    private static string Brief(AuthoringContext context)
    {
        var brief = context.Brief;
        var text = new StringBuilder();
        text.AppendLine($"Scenario: {brief.Scenario}");
        text.AppendLine($"This lesson's grammar point: {brief.Grammar.Title} (step {brief.GrammarStep} of {GrammarCurriculum.Steps.Count}, level {brief.Level})");
        text.AppendLine("Grammar you may use (this point and the earlier ones, nothing else):");
        foreach (var point in brief.AllowedGrammar)
        {
            text.AppendLine($"  - {point}");
        }

        text.AppendLine($"Number of new words to teach: {brief.WordCount}");
        text.Append("Words learners were already taught in earlier lessons (build on them; do not teach them as new words): ");
        text.Append(context.KnownWords.Count == 0 ? "none yet" : string.Join(", ", context.KnownWords));
        return text.ToString();
    }

    private static string Verdict(ReviewReport review)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(review.Summary))
        {
            text.AppendLine($"Summary: {review.Summary}");
        }

        foreach (var criterion in review.Criteria.Where(c => !c.Passed))
        {
            text.AppendLine($"Failed point {criterion.Criterion}: {criterion.Comment}");
        }

        foreach (var issue in review.Issues)
        {
            text.AppendLine($"[{issue.Severity}] {issue.Where}: {issue.Problem} {(string.IsNullOrWhiteSpace(issue.Suggestion) ? string.Empty : $"Fix: {issue.Suggestion}")}".TrimEnd());
        }

        return text.Length == 0 ? "(no details)" : text.ToString().TrimEnd();
    }

    private static string HumanNotes(AuthoringContext context, bool forReviewer = false)
    {
        if (context.HumanFeedback.Count == 0)
        {
            return string.Empty;
        }

        var heading = forReviewer
            ? "A person sent the lesson back with these notes. Check that the lesson now does what they asked:"
            : "A person sent the lesson back with these notes. Do what they ask, they have the final say:";
        return $"{heading}\n{string.Join('\n', context.HumanFeedback.Select(n => $"  - {n}"))}\n";
    }
}
