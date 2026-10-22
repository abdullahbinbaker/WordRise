using Wordrise.Modules.Learning.Domain.Lessons;

namespace Wordrise.Modules.Learning.Infrastructure.Seeds;

/// <summary>
/// The first lesson, so the app works before content authoring exists. Ids are fixed so a
/// reseeded database keeps the same lesson and word ids.
/// </summary>
internal static class SampleLessons
{
    public static readonly Guid GroceryLessonId = Guid.Parse("0b1f6a52-3c4e-4d0a-9d5f-1a2b3c4d5e01");

    public static IReadOnlyList<Lesson> All =>
    [
        Lesson.Create(
            GroceryLessonId,
            number: 1,
            title: "في البقالة",
            scenario: "grocery",
            grammarPoint: "can / can't",
            new LessonContent
            {
                Words =
                [
                    Word("0b1f6a52-3c4e-4d0a-9d5f-1a2b3c4d5e11", "apple", "تفاحة", "آبِل", "I want an apple.", "أريد تفاحة."),
                    Word("0b1f6a52-3c4e-4d0a-9d5f-1a2b3c4d5e12", "milk", "حليب", "مِلك", "The milk is cold.", "الحليب بارد."),
                    Word("0b1f6a52-3c4e-4d0a-9d5f-1a2b3c4d5e13", "rice", "أرز", "رايس", "I buy rice every week.", "أشتري الأرز كل أسبوع."),
                ],
                Dialogue =
                [
                    new DialogueLine { Speaker = "الكاشير", English = "Hello! Can I help you?", Arabic = "مرحباً! هل أستطيع مساعدتك؟" },
                    new DialogueLine { Speaker = "أنت", IsLearner = true, English = "Yes. Milk and bread, please.", Arabic = "نعم. حليب وخبز من فضلك." },
                    new DialogueLine { Speaker = "الكاشير", English = "That is twelve riyals.", Arabic = "المبلغ اثنا عشر ريالاً." },
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
                        new GrammarExample { English = "I can read.", Arabic = "أستطيع القراءة." },
                        new GrammarExample { English = "She can cook.", Arabic = "هي تستطيع الطبخ." },
                        new GrammarExample { English = "I can't drive.", Arabic = "لا أستطيع القيادة." },
                        new GrammarExample { English = "Can you help me?", Arabic = "هل تستطيع مساعدتي؟" },
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

    private static LessonWord Word(string id, string english, string arabic, string pronunciation, string example, string exampleArabic) =>
        new()
        {
            Id = Guid.Parse(id),
            English = english,
            Arabic = arabic,
            Pronunciation = pronunciation,
            Example = example,
            ExampleArabic = exampleArabic,
        };
}
