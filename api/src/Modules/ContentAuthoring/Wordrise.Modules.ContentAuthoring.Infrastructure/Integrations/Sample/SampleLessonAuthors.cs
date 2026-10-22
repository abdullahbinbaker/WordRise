using Wordrise.Modules.ContentAuthoring.Application.Abstractions;
using Wordrise.Modules.ContentAuthoring.Domain;

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Integrations.Sample;

/// <summary>
/// A stand-in writer for development: it builds a plain but well-formed lesson from a small bank
/// of everyday words, with no AI and no cost. The lesson is labelled as a sample so nobody
/// publishes it by mistake. Replaced by the Claude writer in production.
/// </summary>
internal sealed class SampleLessonWriter : ILessonWriter
{
    // English, Arabic meaning, pronunciation in Arabic letters.
    private static readonly (string English, string Arabic, string Pronunciation)[] Bank =
    [
        ("apple", "تفاحة", "آبِل"),
        ("milk", "حليب", "مِلك"),
        ("rice", "أرز", "رايس"),
        ("bread", "خبز", "بْرِد"),
        ("water", "ماء", "وُوتِر"),
        ("cheese", "جبن", "تشِيز"),
        ("egg", "بيضة", "إِيغ"),
        ("tea", "شاي", "تي"),
        ("sugar", "سكر", "شُوغِر"),
        ("salt", "ملح", "سُولت"),
        ("fish", "سمك", "فِش"),
        ("chicken", "دجاج", "تشِكِن"),
        ("juice", "عصير", "جُوس"),
        ("coffee", "قهوة", "كُوفِي"),
        ("banana", "موز", "بَنانا"),
        ("orange", "برتقال", "أُورِنج"),
        ("tomato", "طماطم", "تُمَيتُو"),
        ("potato", "بطاطس", "بَتَيتُو"),
        ("onion", "بصل", "أَنيَن"),
        ("meat", "لحم", "مِيت"),
    ];

    public Task<DraftContent> WriteAsync(AuthoringContext context, CancellationToken cancellationToken) =>
        Task.FromResult(Build(context));

    public Task<DraftContent> ReviseAsync(
        AuthoringContext context,
        DraftContent previous,
        ReviewReport review,
        CancellationToken cancellationToken) =>
        Task.FromResult(Build(context));

    private static DraftContent Build(AuthoringContext context)
    {
        var brief = context.Brief;

        // Prefer words learners have not met; fall back to the whole bank if it runs out.
        var known = context.KnownWords.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var fresh = Bank.Where(w => !known.Contains(w.English)).ToList();
        var picked = (fresh.Count >= brief.WordCount ? fresh : [.. Bank]).Take(brief.WordCount).ToList();

        var correct = picked[0].English;
        var distractors = Bank.Where(w => w.English != correct).Take(3).Select(w => w.English);

        return new DraftContent
        {
            Title = $"[تجريبي] {brief.Scenario}",
            Words =
            [
                .. picked.Select(w => new DraftWord
                {
                    English = w.English,
                    Arabic = w.Arabic,
                    Pronunciation = w.Pronunciation,
                    Example = $"I like {w.English}.",
                    ExampleArabic = $"أحب {w.Arabic}.",
                }),
            ],
            Dialogue =
            [
                new DraftDialogueLine { Speaker = "البائع", English = "Hello! Can I help you?", Arabic = "مرحباً! هل أستطيع مساعدتك؟" },
                .. picked.Chunk(2).Select(pair => new DraftDialogueLine
                {
                    Speaker = "أنت",
                    IsLearner = true,
                    English = $"Can I have {string.Join(" and ", pair.Select(w => w.English))}, please?",
                    Arabic = $"هل أستطيع الحصول على {string.Join(" و", pair.Select(w => w.Arabic))} من فضلك؟",
                }),
            ],
            ChatPrompt = "البائع ينتظر. ماذا تقول وأنت تعطيه المال؟",
            ChatOptions =
            [
                new DraftAnswerOption { Text = "Here you go.", IsCorrect = true },
                new DraftAnswerOption { Text = "Good night." },
            ],
            Grammar = new DraftGrammar
            {
                Title = brief.Grammar.Title,
                Meaning = "قاعدة تجريبية",
                Explanation = "هذا شرح تجريبي. سيكتب الذكاء الاصطناعي الشرح الحقيقي بعد ربط المفتاح.",
                Pattern = "الشخص + الفعل",
                Examples =
                [
                    new DraftGrammarExample { English = "I can read.", Arabic = "أستطيع القراءة." },
                    new DraftGrammarExample { English = "She can cook.", Arabic = "هي تستطيع الطبخ." },
                    new DraftGrammarExample { English = "I can't drive.", Arabic = "لا أستطيع القيادة." },
                ],
                Note = "ملاحظة تجريبية.",
            },
            ListeningSentence = $"I can buy {correct}.",
            ListeningOptions =
            [
                new DraftAnswerOption { Text = correct, IsCorrect = true },
                .. distractors.Select(d => new DraftAnswerOption { Text = d }),
            ],
        };
    }
}

/// <summary>A stand-in reviewer for development: approves every lesson that is well formed.</summary>
internal sealed class SampleLessonReviewer : ILessonReviewer
{
    public Task<ReviewReport> ReviewAsync(AuthoringContext context, DraftContent content, CancellationToken cancellationToken) =>
        Task.FromResult(ReviewReport.FromAi(
            "Sample reviewer: no AI was used, so this approval means nothing. Read the lesson yourself.",
            Enum.GetValues<ReviewCriterion>().Select(c => new CriterionResult
            {
                Criterion = c,
                Passed = true,
                Comment = "Not actually reviewed (sample provider).",
            }),
            []));
}
