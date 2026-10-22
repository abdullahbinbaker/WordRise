import {
  CompleteResult,
  DailyGoal,
  LearnerData,
  Lesson,
  MyWord,
  Rating,
  ReviewCard,
  Today,
  WordFilter,
} from './learner.models';

/** The sample lesson the landing page demo shows; nothing is saved. */
export const DEMO_LESSON: Lesson = {
  id: 'demo-grocery',
  number: 1,
  targetLanguage: 'en',
  nativeLanguage: 'ar',
  title: 'في البقالة',
  scenario: 'grocery',
  grammarPoint: 'can / can\'t',
  words: [
    { id: 'apple', target: 'apple', native: 'تفاحة', pronunciation: 'آبِل', example: 'I want an apple.', exampleNative: 'أريد تفاحة.' },
    { id: 'milk', target: 'milk', native: 'حليب', pronunciation: 'مِلك', example: 'The milk is cold.', exampleNative: 'الحليب بارد.' },
    { id: 'rice', target: 'rice', native: 'أرز', pronunciation: 'رايس', example: 'I buy rice every week.', exampleNative: 'أشتري الأرز كل أسبوع.' },
  ],
  dialogue: [
    { speaker: 'الكاشير', isLearner: false, target: 'Hello! Can I help you?', native: 'مرحباً! هل أستطيع مساعدتك؟' },
    { speaker: 'أنت', isLearner: true, target: 'Yes. Milk and bread, please.', native: 'نعم. حليب وخبز من فضلك.' },
    { speaker: 'الكاشير', isLearner: false, target: 'That is twelve riyals.', native: 'المبلغ اثنا عشر ريالاً.' },
  ],
  chatPrompt: 'الكاشير ينتظر. ماذا تقول وأنت تعطيه المال؟',
  chatOptions: [
    { text: 'Here you go.', isCorrect: true },
    { text: 'Good night.', isCorrect: false },
  ],
  grammar: {
    title: 'can',
    meaning: 'يستطيع',
    explanation: 'نستخدمها لنقول إننا نقدر على فعل شيء.',
    pattern: 'الشخص + can + الفعل',
    examples: [
      { target: 'I can read.', native: 'أستطيع القراءة.' },
      { target: 'She can cook.', native: 'هي تستطيع الطبخ.' },
      { target: "I can't drive.", native: 'لا أستطيع القيادة.' },
      { target: 'Can you help me?', native: 'هل تستطيع مساعدتي؟' },
    ],
    note: 'الفعل بعد can لا يتغيّر: نقول She can swim وليس swims.',
  },
  listening: {
    sentence: 'I can buy milk.',
    options: [
      { text: 'bread', isCorrect: false },
      { text: 'milk', isCorrect: true },
      { text: 'rice', isCorrect: false },
      { text: 'water', isCorrect: false },
    ],
  },
};

const DEMO_REVIEWS: ReviewCard[] = [
  { wordId: 'water', target: 'water', native: 'ماء' },
  { wordId: 'bread', target: 'bread', native: 'خبز' },
  { wordId: 'thank-you', target: 'thank you', native: 'شكراً' },
  { wordId: 'how-much', target: 'How much?', native: 'بكم؟' },
];

const DEMO_WORDS: MyWord[] = [
  ...DEMO_REVIEWS.map((w): MyWord => ({ ...w, status: 'due', nextReviewOn: '' })),
  ...DEMO_LESSON.words.map((w): MyWord => ({ wordId: w.id, target: w.target, native: w.native, status: 'new', nextReviewOn: '' })),
  ...[
    ['hello', 'مرحباً'],
    ['sugar', 'سكر'],
    ['chicken', 'دجاج'],
    ['please', 'من فضلك'],
    ['tea', 'شاي'],
  ].map(([target, native]): MyWord => ({ wordId: target, target, native, status: 'mastered', nextReviewOn: '' })),
];

/** A self-contained learner for the landing page demo: it behaves like the API in memory. */
export class DemoLearnerData extends LearnerData {
  private goalValue = 10;
  private lessonDone = false;

  today(): Promise<Today> {
    return Promise.resolve({
      dailyGoal: this.goalValue,
      streak: this.lessonDone ? 6 : 5,
      reviewsEnabled: true,
      dueReviewCount: this.lessonDone ? 0 : DEMO_REVIEWS.length,
      learnedWordsCount: this.lessonDone ? 45 : 42,
      completedLessonToday: this.lessonDone,
      newLessonsPaused: false,
      reviewBacklogLimit: 30,
      nextLesson: this.lessonDone
        ? null
        : {
            id: DEMO_LESSON.id,
            number: DEMO_LESSON.number,
            title: DEMO_LESSON.title,
            scenario: DEMO_LESSON.scenario,
            grammarPoint: DEMO_LESSON.grammarPoint,
            newWordsCount: DEMO_LESSON.words.length,
          },
    });
  }

  goal(): Promise<DailyGoal> {
    return Promise.resolve({ wordsPerDay: this.goalValue, allowedValues: [5, 10, 15] });
  }

  setGoal(wordsPerDay: number): Promise<void> {
    this.goalValue = wordsPerDay;
    return Promise.resolve();
  }

  lesson(): Promise<Lesson> {
    return Promise.resolve(DEMO_LESSON);
  }

  dueReviews(): Promise<ReviewCard[]> {
    return Promise.resolve(this.lessonDone ? [] : DEMO_REVIEWS);
  }

  rate(_wordId: string, _rating: Rating): Promise<void> {
    return Promise.resolve();
  }

  completeLesson(): Promise<CompleteResult> {
    const alreadyCompleted = this.lessonDone;
    this.lessonDone = true;
    return Promise.resolve({ alreadyCompleted, wordsAdded: alreadyCompleted ? 0 : DEMO_LESSON.words.length, streak: 6 });
  }

  words(filter: WordFilter): Promise<MyWord[]> {
    return Promise.resolve(filter === 'all' ? DEMO_WORDS : DEMO_WORDS.filter((w) => w.status === filter));
  }
}
