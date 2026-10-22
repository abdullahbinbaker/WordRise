// Shapes of the Learning API (/api/v1/learning), shared by the live and demo data sources.

export type Rating = 'hard' | 'good' | 'easy';
export type WordStatus = 'new' | 'due' | 'learning' | 'mastered';
export type WordFilter = 'all' | 'due' | 'mastered';

export interface NextLesson {
  id: string;
  number: number;
  title: string;
  scenario: string;
  grammarPoint: string;
  newWordsCount: number;
}

export interface Today {
  dailyGoal: number;
  streak: number;
  reviewsEnabled: boolean;
  dueReviewCount: number;
  learnedWordsCount: number;
  completedLessonToday: boolean;
  /** Too many reviews are due: today is reviews only. */
  newLessonsPaused: boolean;
  /** Null when the backlog rule is switched off. */
  reviewBacklogLimit: number | null;
  /** Null once every published lesson is completed. */
  nextLesson: NextLesson | null;
}

export interface DailyGoal {
  wordsPerDay: number;
  allowedValues: number[];
}

export interface LessonWord {
  id: string;
  target: string;
  native: string;
  /** How the word sounds, in Arabic letters. */
  pronunciation: string;
  example: string;
  exampleNative: string;
}

export interface DialogueLine {
  speaker: string;
  isLearner: boolean;
  target: string;
  native: string;
}

export interface AnswerOption {
  text: string;
  isCorrect: boolean;
}

export interface GrammarExample {
  target: string;
  native: string;
}

export interface Grammar {
  title: string;
  meaning: string;
  explanation: string;
  pattern: string;
  examples: GrammarExample[];
  note: string;
}

export interface Lesson {
  id: string;
  number: number;
  /** ISO code of the language being taught, for example en. */
  targetLanguage: string;
  /** ISO code of the language the lesson explains it in, for example ar. */
  nativeLanguage: string;
  title: string;
  scenario: string;
  grammarPoint: string;
  words: LessonWord[];
  dialogue: DialogueLine[];
  chatPrompt: string;
  chatOptions: AnswerOption[];
  grammar: Grammar;
  listening: { sentence: string; options: AnswerOption[] };
}

export interface ReviewCard {
  wordId: string;
  target: string;
  native: string;
}

export interface CompleteResult {
  alreadyCompleted: boolean;
  wordsAdded: number;
  streak: number;
}

export interface MyWord {
  wordId: string;
  target: string;
  native: string;
  status: WordStatus;
  nextReviewOn: string;
}

/** Where the learner app gets and saves its data. */
export abstract class LearnerData {
  abstract today(): Promise<Today>;
  abstract goal(): Promise<DailyGoal>;
  abstract setGoal(wordsPerDay: number): Promise<void>;
  abstract lesson(lessonId: string): Promise<Lesson>;
  abstract dueReviews(): Promise<ReviewCard[]>;
  abstract rate(wordId: string, rating: Rating): Promise<void>;
  abstract completeLesson(lessonId: string): Promise<CompleteResult>;
  abstract words(filter: WordFilter): Promise<MyWord[]>;
}
