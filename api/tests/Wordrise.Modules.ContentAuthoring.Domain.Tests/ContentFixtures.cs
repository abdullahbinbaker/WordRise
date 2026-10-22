using Wordrise.Modules.ContentAuthoring.Domain;

namespace Wordrise.Modules.ContentAuthoring.TestSupport;

/// <summary>Ready-made briefs and lessons for tests. Shared with the API tests as a linked file.</summary>
public static class ContentFixtures
{
    private static readonly string[] Pool =
        ["apple", "milk", "rice", "bread", "water", "cheese", "egg", "tea", "sugar", "salt"];

    public static LessonBrief Brief(int step = 4, int words = 3, string scenario = "grocery store") =>
        LessonBrief.Create(scenario, step, words);

    /// <summary>A lesson that passes every structural check for the brief.</summary>
    public static DraftContent ValidContent(LessonBrief brief)
    {
        var english = Pool.Take(brief.WordCount).ToList();

        // Two new words per learner line, so a long lesson still fits the dialogue length limit.
        List<DraftDialogueLine> dialogue =
        [
            new() { Speaker = "الكاشير", English = "Hello! Can I help you?", Arabic = "مرحباً! هل أستطيع مساعدتك؟" },
            .. english.Chunk(2).Select(pair => new DraftDialogueLine
            {
                Speaker = "أنت",
                IsLearner = true,
                English = $"Can I have {string.Join(" and ", pair)}, please?",
                Arabic = "هل أستطيع الحصول عليها من فضلك؟",
            }),
        ];

        var correct = english[0];
        var distractors = Pool.Where(w => w != correct).Take(2);

        return new DraftContent
        {
            Title = "في البقالة",
            Words =
            [
                .. english.Select(w => new DraftWord
                {
                    English = w,
                    Arabic = "معنى",
                    Pronunciation = "نُطق",
                    Example = $"I like {w}.",
                    ExampleArabic = "أحب هذا.",
                }),
            ],
            Dialogue = dialogue,
            ChatPrompt = "ماذا تقول وأنت تعطيه المال؟",
            ChatOptions =
            [
                new DraftAnswerOption { Text = "Here you go.", IsCorrect = true },
                new DraftAnswerOption { Text = "Good night." },
            ],
            Grammar = new DraftGrammar
            {
                Title = "can",
                Meaning = "يستطيع",
                Explanation = "نستخدمها لنقول إننا نقدر على فعل شيء.",
                Pattern = "الشخص + can + الفعل",
                Examples =
                [
                    new DraftGrammarExample { English = "I can read.", Arabic = "أستطيع القراءة." },
                    new DraftGrammarExample { English = "She can cook.", Arabic = "هي تستطيع الطبخ." },
                    new DraftGrammarExample { English = "I can't drive.", Arabic = "لا أستطيع القيادة." },
                ],
                Note = "الفعل بعد can لا يتغيّر.",
            },
            ListeningSentence = $"I can buy {correct}.",
            ListeningOptions =
            [
                new DraftAnswerOption { Text = correct, IsCorrect = true },
                .. distractors.Select(d => new DraftAnswerOption { Text = d }),
            ],
        };
    }

    public static ReviewReport Passing() => ReviewReport.FromAi(
        "Good.",
        Enum.GetValues<ReviewCriterion>().Select(c => new CriterionResult { Criterion = c, Passed = true }),
        []);

    public static ReviewReport Failing(string problem = "The dialogue is unnatural.") => ReviewReport.FromAi(
        "Needs work.",
        Enum.GetValues<ReviewCriterion>().Select(c => new CriterionResult
        {
            Criterion = c,
            Passed = c != ReviewCriterion.Realism,
            Comment = c == ReviewCriterion.Realism ? problem : string.Empty,
        }),
        [new ReviewIssue { Severity = IssueSeverity.Blocking, Where = "dialogue line 2", Problem = problem, Suggestion = "Say it the way a cashier would." }]);
}
