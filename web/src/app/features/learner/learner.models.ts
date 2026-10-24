// Shapes of the Learning API (/api/v1/learning), shared by the live and demo data sources.

export type Rating = 'hard' | 'good' | 'easy';
export type WordStatus = 'new' | 'due' | 'learning' | 'mastered';
export type WordFilter = 'all' | 'due' | 'mastered';

export type LessonTrack = 'main' | 'foundations';

export interface NextLesson {
  id: string;
  number: number;
  title: string;
  scenario: string;
  grammarPoint: string;
  newWordsCount: number;
  /** Foundations: letters, numbers and the first words, before the main course. */
  track: LessonTrack;
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
  /** The learner started further on and no lesson exists at that level yet. */
  noLessonsAtYourLevelYet: boolean;
  onboardingDone: boolean;
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
  /** What is shown big when it differs from the spoken text: "Aa" for the letter A, "7" for seven. */
  symbol: string;
  /** A letter: it has a name and a sound, and its name is spoken by spelling it. */
  isLetter: boolean;
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

/** One question of a foundations lesson: look, listen, then choose. */
export interface FoundationExercise {
  prompt: string;
  /** What is shown big; empty when the exercise is only listening. */
  stimulus: string;
  /** What the speaker button plays; empty when there is nothing to hear. */
  audioText: string;
  /** Empty for ordinary speech, "letter" for a letter's name, "sound" for its sound. */
  audioStyle: '' | 'letter' | 'sound';
  options: AnswerOption[];
}

export interface Lesson {
  id: string;
  number: number;
  track: LessonTrack;
  /** The grammar curriculum step this lesson teaches, when it has one. */
  grammarStep: number | null;
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
  /** The practice of a foundations lesson; empty in the main course. */
  exercises: FoundationExercise[];
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

export interface StartingPoint {
  step: number;
  title: string;
  example: string;
}

export interface Onboarding {
  knowsLetters: boolean;
  startStep: number;
  done: boolean;
  startingPoints: StartingPoint[];
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
  abstract onboarding(): Promise<Onboarding>;
  abstract setOnboarding(knowsLetters: boolean, startStep: number): Promise<void>;
}
