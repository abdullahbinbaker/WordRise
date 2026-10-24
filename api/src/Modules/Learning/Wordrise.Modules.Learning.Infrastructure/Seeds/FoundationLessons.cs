using Wordrise.Modules.Learning.Domain.Lessons;

namespace Wordrise.Modules.Learning.Infrastructure.Seeds;

/// <summary>
/// The foundations course for learners who do not know the Latin letters yet: greetings, the alphabet,
/// numbers, spelling a name and the first words to read. The content is fixed and written by hand (not by
/// the AI pipeline): a wrong letter sound or a wrong example word would teach a beginner something false
/// that they cannot spot. The Arabic hints and the order are a DRAFT for a teacher to review.
/// </summary>
internal static class FoundationLessons
{
    // Fixed ids, so a reseeded database keeps the same lessons.
    public static readonly Guid GreetingsId = Guid.Parse("7f3e2a10-0000-4000-8000-000000000001");
    public static readonly Guid AlphabetId = Guid.Parse("7f3e2a10-0000-4000-8000-000000000002");
    public static readonly Guid NumbersId = Guid.Parse("7f3e2a10-0000-4000-8000-000000000003");
    public static readonly Guid SpellingId = Guid.Parse("7f3e2a10-0000-4000-8000-000000000004");
    public static readonly Guid ReadingId = Guid.Parse("7f3e2a10-0000-4000-8000-000000000005");

    public static IReadOnlyList<Lesson> All =>
    [
        Foundation(GreetingsId, 1, "التحيات", Greetings()),
        Foundation(AlphabetId, 2, "الأبجدية الإنجليزية", Alphabet()),
        Foundation(NumbersId, 3, "الأرقام من 0 إلى 20", Numbers()),
        Foundation(SpellingId, 4, "اسمك بالحروف", Spelling()),
        Foundation(ReadingId, 5, "اقرأ كلماتك الأولى", Reading()),
    ];

    private static Lesson Foundation(Guid id, int number, string title, LessonContent content) =>
        Lesson.Create(
            id,
            number,
            LessonTrack.Foundations,
            grammarStep: null,
            LessonLanguages.English,
            LessonLanguages.Arabic,
            title,
            scenario: string.Empty,
            grammarPoint: string.Empty,
            content);

    // ---- 1. Greetings: learned by ear first, so no reading is needed ----

    private static LessonContent Greetings() => new()
    {
        Words =
        [
            Item("hello", "مرحباً", "هَلُو", "Hello!", "مرحباً!"),
            Item("goodbye", "مع السلامة", "جُود بَاي", "Goodbye!", "مع السلامة!"),
            Item("thank you", "شكراً", "ثَانْك يُو", "Thank you!", "شكراً!"),
            Item("please", "من فضلك", "بْلِيز", "Water, please.", "ماء، من فضلك."),
            Item("yes", "نعم", "يَس", "Yes, please.", "نعم، من فضلك."),
            Item("no", "لا", "نُو", "No, thank you.", "لا، شكراً."),
            Item("sorry", "آسف", "سُورِي", "Sorry!", "آسف!"),
        ],
        Exercises =
        [
            Hear("thank you", "thank you", "please", "sorry"),
            Hear("goodbye", "hello", "goodbye", "yes"),
            Hear("hello", "no", "goodbye", "hello"),
            Meaning("yes", "نعم", "لا", "شكراً"),
            Meaning("sorry", "من فضلك", "آسف", "مرحباً"),
            Meaning("please", "مع السلامة", "لا", "من فضلك"),
            Hear("no", "no", "yes", "please"),
        ],
    };

    private static FoundationExercise Hear(string played, params string[] options) => new()
    {
        Prompt = "استمع، ثم اختر ما سمعتَه",
        AudioText = played,
        Options = [.. options.Select(option => new AnswerOption { Text = option, IsCorrect = option == played })],
    };

    private static FoundationExercise Meaning(string shown, params string[] options)
    {
        return new FoundationExercise
        {
            Prompt = "ما معنى هذه الكلمة؟",
            Stimulus = shown,
            AudioText = shown,
            Options = [.. options.Select(option => new AnswerOption { Text = option, IsCorrect = option == GreetingMeanings[shown] })],
        };
    }

    private static readonly Dictionary<string, string> GreetingMeanings = new()
    {
        ["yes"] = "نعم",
        ["sorry"] = "آسف",
        ["please"] = "من فضلك",
    };

    // ---- 2. The alphabet, all 26 letters at once ----

    private sealed record Letter(char Name, string ArabicName, string SoundHint, string Ipa, string Example, string ExampleArabic);

    private static readonly Letter[] Letters =
    [
        new('A', "إيه", "أَ قصيرة، كما في apple", "æ", "apple", "تفاحة"),
        new('B', "بي", "بْ", "b", "ball", "كرة"),
        new('C', "سي", "كْ، كما في cat (وأحياناً سين، كما في city)", "k", "cat", "قطة"),
        new('D', "دي", "دْ", "d", "duck", "بطة"),
        new('E', "إي", "إِ قصيرة، كما في egg", "ɛ", "egg", "بيضة"),
        new('F', "إف", "فْ", "f", "fish", "سمكة"),
        new('G', "جي", "صوت g كما في goat، يشبه «ك» لكن بجهر", "ɡ", "goat", "ماعز"),
        new('H', "إيتش", "هْ", "h", "hat", "قبعة"),
        new('I', "آي", "إِ قصيرة، كما في ink", "ɪ", "ink", "حبر"),
        new('J', "جيه", "جْ", "dʒ", "jam", "مربى"),
        new('K', "كيه", "كْ", "k", "kite", "طائرة ورقية"),
        new('L', "إل", "لْ", "l", "lion", "أسد"),
        new('M', "إم", "مْ", "m", "moon", "قمر"),
        new('N', "إن", "نْ", "n", "nose", "أنف"),
        new('O', "أو", "أُو قصيرة مفتوحة، كما في octopus", "ɑ", "octopus", "أخطبوط"),
        new('P', "بي", "بْ خفيفة بنفَس، وليست b", "p", "pen", "قلم"),
        new('Q', "كيو", "كْو، كما في queen", "kw", "queen", "ملكة"),
        new('R', "آر", "رْ", "ɹ", "rain", "مطر"),
        new('S', "إس", "سْ", "s", "sun", "شمس"),
        new('T', "تي", "تْ", "t", "tree", "شجرة"),
        new('U', "يو", "أَ قصيرة، كما في umbrella", "ʌ", "umbrella", "مظلة"),
        new('V', "في", "فْ مجهورة، وليست f", "v", "van", "شاحنة صغيرة"),
        new('W', "دابليو", "وْ", "w", "water", "ماء"),
        new('X', "إكس", "كْس، كما في fox", "ks", "fox", "ثعلب"),
        new('Y', "واي", "يْ، كما في yes", "j", "yes", "نعم"),
        new('Z', "زد", "زْ", "z", "zoo", "حديقة الحيوان"),
    ];

    /// <summary>Letters a beginner mixes up: the wrong answers in the exercises come from here.</summary>
    private static readonly Dictionary<char, char[]> Confusable = new()
    {
        ['A'] = ['O', 'E'], ['B'] = ['D', 'P'], ['C'] = ['E', 'O'], ['D'] = ['B', 'P'], ['E'] = ['C', 'O'],
        ['F'] = ['T', 'E'], ['G'] = ['Q', 'J'], ['H'] = ['N', 'M'], ['I'] = ['L', 'J'], ['J'] = ['G', 'I'],
        ['K'] = ['X', 'H'], ['L'] = ['I', 'J'], ['M'] = ['N', 'W'], ['N'] = ['M', 'H'], ['O'] = ['C', 'A'],
        ['P'] = ['B', 'Q'], ['Q'] = ['P', 'G'], ['R'] = ['N', 'V'], ['S'] = ['Z', 'C'], ['T'] = ['F', 'L'],
        ['U'] = ['V', 'N'], ['V'] = ['W', 'U'], ['W'] = ['V', 'M'], ['X'] = ['K', 'Z'], ['Y'] = ['V', 'J'],
        ['Z'] = ['S', 'X'],
    };

    private static LessonContent Alphabet() => new()
    {
        Words = [.. Letters.Select(letter => new LessonWord
        {
            Id = WordIdentity.For(LessonLanguages.English, letter.Name.ToString()),
            Target = letter.Name.ToString(),
            Symbol = $"{letter.Name}{char.ToLowerInvariant(letter.Name)}",
            IsLetter = true,
            Ipa = letter.Ipa,
            Native = letter.ArabicName,
            Pronunciation = letter.SoundHint,
            Example = letter.Example,
            ExampleNative = letter.ExampleArabic,
        })],
        Exercises =
        [
            .. "AEGIPVWY".Select((name, index) => HearLetterName(name, index)),
            .. "BDFKMPSTVZAEIOU".Select((name, index) => HearLetterSound(name, index)),
            .. "ABDGQRN".Select((name, index) => CapitalToSmall(name, index)),
            .. "BCFLMRSTW".Select((name, index) => LetterToWord(name, index)),
        ],
    };

    private static Letter Of(char name) => Letters.Single(l => l.Name == name);

    /// <summary>The correct answer goes in a different place each time, so the right one is never always first.</summary>
    private static List<AnswerOption> Choices(string correct, IEnumerable<string> wrong, int index)
    {
        List<AnswerOption> options = [.. wrong.Select(text => new AnswerOption { Text = text })];
        options.Insert(index % (options.Count + 1), new AnswerOption { Text = correct, IsCorrect = true });
        return options;
    }

    private static FoundationExercise HearLetterName(char name, int index) => new()
    {
        Prompt = "استمع لاسم الحرف، ثم اختره",
        AudioText = name.ToString(),
        AudioStyle = SpokenStyle.Letter,
        Options = Choices(name.ToString(), Confusable[name].Select(c => c.ToString()), index),
    };

    private static FoundationExercise HearLetterSound(char name, int index) => new()
    {
        Prompt = "استمع لصوت الحرف، ثم اختر الحرف",
        AudioText = char.ToLowerInvariant(name).ToString(),
        AudioStyle = SpokenStyle.Sound,
        AudioIpa = Of(name).Ipa,
        Options = Choices(name.ToString(), Confusable[name].Select(c => c.ToString()), index + 1),
    };

    private static FoundationExercise CapitalToSmall(char name, int index) => new()
    {
        Prompt = "اختر الحرف الصغير المقابل",
        Stimulus = name.ToString(),
        AudioText = name.ToString(),
        AudioStyle = SpokenStyle.Letter,
        Options = Choices(
            char.ToLowerInvariant(name).ToString(),
            Confusable[name].Select(c => char.ToLowerInvariant(c).ToString()),
            index + 2),
    };

    private static FoundationExercise LetterToWord(char name, int index)
    {
        // The wrong words begin with other letters, never with one that sounds the same (C and K) and never
        // with X, whose example word (fox) begins with another letter and would also fit F.
        var wrong = Letters
            .Where(l => l.Name != name && l.Name != 'X' && l.Ipa != Of(name).Ipa && !Confusable[name].Contains(l.Name))
            .Skip(index % 12)
            .Take(2);
        return new FoundationExercise
        {
            Prompt = "أي كلمة تبدأ بهذا الحرف؟",
            Stimulus = name.ToString(),
            AudioText = name.ToString(),
            AudioStyle = SpokenStyle.Letter,
            Options = Choices(Of(name).Example, wrong.Select(l => l.Example), index),
        };
    }

    // ---- 3. Numbers 0 to 20: the digits (as the learner will see them) with their words ----

    private static readonly (string Word, string Arabic, string Hint)[] NumberWords =
    [
        ("zero", "صفر", "زِيرُو"), ("one", "واحد", "وَان"), ("two", "اثنان", "تُو"), ("three", "ثلاثة", "ثْرِي"),
        ("four", "أربعة", "فُور"), ("five", "خمسة", "فَايف"), ("six", "ستة", "سِكس"), ("seven", "سبعة", "سِفِن"),
        ("eight", "ثمانية", "إِيت"), ("nine", "تسعة", "نَايْن"), ("ten", "عشرة", "تِن"),
        ("eleven", "أحد عشر", "إِلِفِن"), ("twelve", "اثنا عشر", "تْوِلف"), ("thirteen", "ثلاثة عشر", "ثِرتِين"),
        ("fourteen", "أربعة عشر", "فُورتِين"), ("fifteen", "خمسة عشر", "فِفتِين"), ("sixteen", "ستة عشر", "سِكستِين"),
        ("seventeen", "سبعة عشر", "سِفِنتِين"), ("eighteen", "ثمانية عشر", "إِيتِين"),
        ("nineteen", "تسعة عشر", "نَاينتِين"), ("twenty", "عشرون", "تْوِنتِي"),
    ];

    private static LessonContent Numbers() => new()
    {
        Words = [.. NumberWords.Select((number, digit) => new LessonWord
        {
            Id = WordIdentity.For(LessonLanguages.English, number.Word),
            Target = number.Word,
            Symbol = digit.ToString(),
            Native = number.Arabic,
            Pronunciation = number.Hint,
        })],
        Exercises =
        [
            HearNumber(3, 5, 8),
            HearNumber(7, 2, 9),
            HearNumber(13, 15, 3),
            HearNumber(16, 6, 12),
            HearNumber(14, 4, 11),
            HearNumber(20, 12, 10),
            HearNumber(19, 9, 18),
            SeeNumber(9, "eight", "five"),
            SeeNumber(12, "two", "eleven"),
            SeeNumber(15, "five", "fifty"),
            SeeNumber(0, "one", "ten"),
            SeeNumber(17, "seven", "seventy"),
        ],
    };

    private static FoundationExercise HearNumber(int heard, int wrong1, int wrong2) => new()
    {
        Prompt = "استمع، ثم اختر الرقم الذي سمعتَه",
        AudioText = NumberWords[heard].Word,
        Options = Choices(heard.ToString(), [wrong1.ToString(), wrong2.ToString()], heard),
    };

    private static FoundationExercise SeeNumber(int shown, string wrong1, string wrong2) => new()
    {
        Prompt = "ما اسم هذا الرقم بالإنجليزية؟",
        Stimulus = shown.ToString(),
        AudioText = NumberWords[shown].Word,
        Options = Choices(NumberWords[shown].Word, [wrong1, wrong2], shown + 1),
    };

    // ---- 4. Spelling a name ----

    private static LessonContent Spelling() => new()
    {
        Words =
        [
            Item("name", "اسم", "نَيم", "My name is Ali.", "اسمي علي."),
            Item("spell", "يتهجّى", "سْبِل", "How do you spell it?", "كيف تتهجّاه؟"),
        ],
        Exercises =
        [
            HearSpelling("ALI", "Ali", "Ila", "Lia"),
            HearSpelling("SAM", "Sam", "Mas", "Sim"),
            HearSpelling("OMAR", "Omar", "Amar", "Omer"),
            HearSpelling("NOOR", "Noor", "Nora", "Roon"),
            HearSpelling("LAYLA", "Layla", "Lilia", "Alyl"),
        ],
    };

    private static FoundationExercise HearSpelling(string spelled, string name, string wrong1, string wrong2) => new()
    {
        Prompt = "استمع لتهجئة الاسم، ثم اختره",
        AudioText = spelled,
        AudioStyle = SpokenStyle.Letter,
        Options = Choices(name, [wrong1, wrong2], spelled.Length),
    };

    // ---- 5. The first words to read: three sounds each, using only letters the learner has met ----

    private static readonly (string Word, string Arabic)[] ReadingWords =
    [
        ("cat", "قطة"), ("dad", "أب"), ("sun", "شمس"), ("pen", "قلم"), ("bed", "سرير"), ("big", "كبير"),
        ("hot", "ساخن"), ("cup", "كوب"), ("bus", "حافلة"), ("red", "أحمر"), ("run", "يركض"), ("sit", "يجلس"),
        ("hat", "قبعة"), ("map", "خريطة"), ("box", "صندوق"), ("net", "شبكة"), ("leg", "ساق"), ("hen", "دجاجة"),
        ("jam", "مربى"), ("fan", "مروحة"),
    ];

    private static LessonContent Reading() => new()
    {
        Words = [.. ReadingWords.Select(word => Item(word.Word, word.Arabic, string.Empty, string.Empty, string.Empty))],
        Exercises =
        [
            .. ReadingWords.Take(10).Select((word, index) => ReadWord(word.Word, word.Arabic, index)),
            HearWord("bus", "bus", "bed", "big"),
            HearWord("hen", "hat", "hen", "net"),
            HearWord("map", "map", "jam", "man"),
            HearWord("sit", "sun", "sit", "set"),
        ],
    };

    private static FoundationExercise ReadWord(string word, string arabic, int index)
    {
        var wrong = ReadingWords.Where(w => w.Word != word).Skip((index * 3) % 15).Take(2).Select(w => w.Arabic);
        return new FoundationExercise
        {
            Prompt = "اقرأ الكلمة، ثم اختر معناها",
            Stimulus = word,
            AudioText = word,
            Options = Choices(arabic, wrong, index),
        };
    }

    private static FoundationExercise HearWord(string played, params string[] options) => new()
    {
        Prompt = "استمع، ثم اختر الكلمة التي سمعتَها",
        AudioText = played,
        Options = [.. options.Select(option => new AnswerOption { Text = option, IsCorrect = option == played })],
    };

    private static LessonWord Item(string target, string native, string pronunciation, string example, string exampleNative) => new()
    {
        Id = WordIdentity.For(LessonLanguages.English, target),
        Target = target,
        Native = native,
        Pronunciation = pronunciation,
        Example = example,
        ExampleNative = exampleNative,
    };
}
