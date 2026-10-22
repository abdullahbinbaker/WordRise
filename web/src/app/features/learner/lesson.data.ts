// Sample lesson content ("At the grocery store"). Replaced by the lessons API later.

export interface ReviewCard {
  en: string;
  ar: string;
}

export interface NewWord {
  en: string;
  /** Pronunciation written in Arabic letters. */
  tr: string;
  ar: string;
  ex: string;
  exAr: string;
}

export interface ChatLine {
  who: string;
  en: string;
  ar: string;
  /** True when the learner says the line. */
  me: boolean;
}

export interface ChatOption {
  en: string;
  ok: boolean;
}

export interface GrammarExample {
  en: string;
  ar: string;
}

export type WordStatus = 'due' | 'new' | 'done';

export interface VocabularyItem {
  en: string;
  ar: string;
  status: WordStatus;
}

export interface GoalOption {
  count: number;
  title: string;
  sub: string;
}

export const GOAL_OPTIONS: readonly GoalOption[] = [
  { count: 5, title: 'بداية هادئة', sub: 'حوالي ٥ دقائق يومياً' },
  { count: 10, title: 'متوازن', sub: 'حوالي ١٠ دقائق يومياً' },
  { count: 15, title: 'طموح', sub: 'حوالي ١٥ دقيقة يومياً' },
];

export const LESSON_STEPS: readonly string[] = [
  'مراجعة ٤ كلمات',
  '٣ كلمات جديدة',
  'محادثة: في البقالة',
  'قاعدة: can',
  'تمرين استماع',
];

export const REVIEW_CARDS: readonly ReviewCard[] = [
  { en: 'water', ar: 'ماء' },
  { en: 'bread', ar: 'خبز' },
  { en: 'thank you', ar: 'شكراً' },
  { en: 'How much?', ar: 'بكم؟' },
];

export const REVIEW_RATINGS: readonly { label: string; when: string }[] = [
  { label: 'صعبة', when: 'غداً' },
  { label: 'جيدة', when: 'بعد ٣ أيام' },
  { label: 'سهلة', when: 'بعد أسبوع' },
];

export const NEW_WORDS: readonly NewWord[] = [
  { en: 'apple', tr: 'آبِل', ar: 'تفاحة', ex: 'I want an apple.', exAr: 'أريد تفاحة.' },
  { en: 'milk', tr: 'مِلك', ar: 'حليب', ex: 'The milk is cold.', exAr: 'الحليب بارد.' },
  { en: 'rice', tr: 'رايس', ar: 'أرز', ex: 'I buy rice every week.', exAr: 'أشتري الأرز كل أسبوع.' },
];

export const GROCERY_CHAT: readonly ChatLine[] = [
  { who: 'الكاشير', en: 'Hello! Can I help you?', ar: 'مرحباً! هل أستطيع مساعدتك؟', me: false },
  { who: 'أنت', en: 'Yes. Milk and bread, please.', ar: 'نعم. حليب وخبز من فضلك.', me: true },
  { who: 'الكاشير', en: 'That is twelve riyals.', ar: 'المبلغ اثنا عشر ريالاً.', me: false },
];

export const CHAT_OPTIONS: readonly ChatOption[] = [
  { en: 'Here you go.', ok: true },
  { en: 'Good night.', ok: false },
];

export const CAN_EXAMPLES: readonly GrammarExample[] = [
  { en: 'I can read.', ar: 'أستطيع القراءة.' },
  { en: 'She can cook.', ar: 'هي تستطيع الطبخ.' },
  { en: "I can't drive.", ar: 'لا أستطيع القيادة.' },
  { en: 'Can you help me?', ar: 'هل تستطيع مساعدتي؟' },
];

export const LISTEN_SENTENCE = 'I can buy milk.';
export const LISTEN_OPTIONS: readonly string[] = ['bread', 'milk', 'rice', 'water'];
export const LISTEN_ANSWER = 'milk';

export const MY_WORDS: readonly VocabularyItem[] = [
  { en: 'water', ar: 'ماء', status: 'due' },
  { en: 'bread', ar: 'خبز', status: 'due' },
  { en: 'thank you', ar: 'شكراً', status: 'due' },
  { en: 'How much?', ar: 'بكم؟', status: 'due' },
  { en: 'apple', ar: 'تفاحة', status: 'new' },
  { en: 'milk', ar: 'حليب', status: 'new' },
  { en: 'rice', ar: 'أرز', status: 'new' },
  { en: 'hello', ar: 'مرحباً', status: 'done' },
  { en: 'sugar', ar: 'سكر', status: 'done' },
  { en: 'chicken', ar: 'دجاج', status: 'done' },
  { en: 'please', ar: 'من فضلك', status: 'done' },
  { en: 'tea', ar: 'شاي', status: 'done' },
];
