using Wordrise.Modules.Learning.Domain.Lessons;

namespace Wordrise.Modules.Learning.Infrastructure.Seeds;

/// <summary>
/// The first lesson, so a fresh database has something to teach. The lesson id is fixed so a
/// reseeded database keeps it; word ids come from the words themselves.
/// </summary>
internal static class SampleLessons
{
    public static readonly Guid GroceryLessonId = Guid.Parse("0b1f6a52-3c4e-4d0a-9d5f-1a2b3c4d5e01");

    public static IReadOnlyList<Lesson> All =>
    [
        Lesson.Create(
            GroceryLessonId,
            number: 1,
            track: LessonTrack.Main,
            grammarStep: 5,
            targetLanguage: LessonLanguages.English,
            nativeLanguage: LessonLanguages.Arabic,
            title: "في البقالة",
            scenario: "grocery",
            grammarPoint: "can / can't",
            new LessonContent
            {
                Words =
                [
                    Word("apple", "تفاحة", "آبِل", "I want an apple.", "أريد تفاحة."),
                    Word("milk", "حليب", "مِلك", "The milk is cold.", "الحليب بارد."),
                    Word("rice", "أرز", "رايس", "I buy rice every week.", "أشتري الأرز كل أسبوع."),
                ],
                Dialogue =
                [
                    new DialogueLine { Speaker = "الكاشير", Target = "Hello! Can I help you?", Native = "مرحباً! هل أستطيع مساعدتك؟" },
                    new DialogueLine { Speaker = "أنت", IsLearner = true, Target = "Yes. Milk and bread, please.", Native = "نعم. حليب وخبز من فضلك." },
                    new DialogueLine { Speaker = "الكاشير", Target = "That is twelve riyals.", Native = "المبلغ اثنا عشر ريالاً." },
                ],
                ChatPrompt = "الكاشير ينتظر. ماذا تقول وأنت تعطيه المال؟",
                ChatOptions =
                [
                    new AnswerOption { Text = "Here you go.", IsCorrect = true },
                    new AnswerOption { Text = "Good night." },
                ],
                Grammar = new GrammarExplanation
                {
                    Title = "can",
                    Meaning = "يستطيع",
                    Explanation = "نستخدمها لنقول إننا نقدر على فعل شيء.",
                    Pattern = "الشخص + can + الفعل",
                    Examples =
                    [
                        new GrammarExample { Target = "I can read.", Native = "أستطيع القراءة." },
                        new GrammarExample { Target = "She can cook.", Native = "هي تستطيع الطبخ." },
                        new GrammarExample { Target = "I can't drive.", Native = "لا أستطيع القيادة." },
                        new GrammarExample { Target = "Can you help me?", Native = "هل تستطيع مساعدتي؟" },
                    ],
                    Note = "الفعل بعد can لا يتغيّر: نقول She can swim وليس swims.",
                },
                ListeningSentence = "I can buy milk.",
                ListeningOptions =
                [
                    new AnswerOption { Text = "bread" },
                    new AnswerOption { Text = "milk", IsCorrect = true },
                    new AnswerOption { Text = "rice" },
                    new AnswerOption { Text = "water" },
                ],
            }),
    ];

    private static LessonWord Word(string target, string native, string pronunciation, string example, string exampleNative) =>
        new()
        {
            Id = WordIdentity.For(LessonLanguages.English, target),
            Target = target,
            Native = native,
            Pronunciation = pronunciation,
            Example = example,
            ExampleNative = exampleNative,
        };
}
