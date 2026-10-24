import { Injectable, PendingTasks, computed, inject, signal } from '@angular/core';
import { SpeechService } from '../../core/speech.service';
import {
  CompleteResult,
  LearnerData,
  Lesson,
  MyWord,
  Onboarding,
  Rating,
  ReviewCard,
  Today,
  WordFilter,
} from './learner.models';

export type Screen =
  | 'goal'
  | 'home'
  | 'review'
  | 'newword'
  | 'dialogue'
  | 'grammar'
  | 'listen'
  | 'foundation'
  | 'welcome'
  | 'done'
  | 'words';

/** A foundations lesson first shows its items, then practises them with questions. */
export type FoundationPhase = 'items' | 'quiz';
export type AnswerState = 'idle' | 'selected' | 'right' | 'wrong';
/** What the primary button of a quiz screen does next. */
export type AnswerStep = 'choose' | 'check' | 'retry' | 'next';

/** Lesson screens in order; each of the first five is 20% of the progress bar. */
export const LESSON_ORDER: readonly Screen[] = ['review', 'newword', 'dialogue', 'grammar', 'listen', 'done'];

export function answerState(selected: boolean, checked: boolean, ok: boolean): AnswerState {
  if (!selected) return 'idle';
  if (!checked) return 'selected';
  return ok ? 'right' : 'wrong';
}

export function answerStep(pick: number | null, checked: boolean, ok: boolean): AnswerStep {
  if (pick === null) return 'choose';
  if (!checked) return 'check';
  return ok ? 'next' : 'retry';
}

/** State of one learner app instance (provided per `app-learner`). */
@Injectable()
export class LearnerStore {
  private readonly pendingTasks = inject(PendingTasks);
  private readonly speech = inject(SpeechService);
  private data!: LearnerData;

  readonly screen = signal<Screen>('home');
  readonly onboarded = signal(true);
  readonly learnerName = signal('');
  /** Set by the host page when the learner can sign out from inside the app. */
  readonly logoutHandler = signal<(() => void) | null>(null);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly today = signal<Today | null>(null);
  readonly goal = signal(10);
  readonly allowedGoals = signal<number[]>([5, 10, 15]);

  /** What the learner tells us at sign-up: do they know the letters, and where to start. */
  readonly onboarding = signal<Onboarding | null>(null);
  readonly knowsLetters = signal(true);
  readonly startStep = signal(1);

  readonly lesson = signal<Lesson | null>(null);
  readonly reviewCards = signal<ReviewCard[]>([]);
  /** A review-only session: started from home or "My words", it ends without a lesson. */
  readonly reviewOnly = signal(false);
  readonly reviewedCount = signal(0);
  readonly completion = signal<CompleteResult | null>(null);

  readonly words = signal<MyWord[]>([]);
  readonly wordFilter = signal<WordFilter>('all');

  readonly cardIndex = signal(0);
  readonly revealed = signal(false);
  readonly wordIndex = signal(0);
  readonly chatPick = signal<number | null>(null);
  readonly chatChecked = signal(false);
  readonly listenPick = signal<number | null>(null);
  readonly listenChecked = signal(false);

  readonly foundationPhase = signal<FoundationPhase>('items');
  readonly exerciseIndex = signal(0);
  readonly exercisePick = signal<number | null>(null);
  readonly exerciseChecked = signal(false);

  readonly currentCard = computed(() => this.reviewCards()[this.cardIndex()] ?? null);
  readonly currentWord = computed(() => this.lesson()?.words[this.wordIndex()] ?? null);
  readonly inLesson = computed(
    () => (LESSON_ORDER.includes(this.screen()) && this.screen() !== 'done') || this.screen() === 'foundation',
  );

  readonly currentExercise = computed(() => this.lesson()?.exercises[this.exerciseIndex()] ?? null);

  readonly exerciseOk = computed(() => {
    const pick = this.exercisePick();
    return pick !== null && !!this.currentExercise()?.options[pick]?.isCorrect;
  });

  readonly lessonProgress = computed(() => {
    const screen = this.screen();
    const cardProgress =
      (this.cardIndex() + (this.revealed() ? 0.5 : 0)) / Math.max(this.reviewCards().length, 1);
    if (this.reviewOnly()) {
      return screen === 'review' ? cardProgress * 100 : 0;
    }
    if (screen === 'foundation') {
      const total = Math.max(this.lesson()?.exercises.length ?? 1, 1);
      return this.foundationPhase() === 'items' ? 5 : 10 + (this.exerciseIndex() / total) * 90;
    }
    if (screen === 'review') return cardProgress * 20;
    if (screen === 'newword') return 20 + (this.wordIndex() / Math.max(this.lesson()?.words.length ?? 1, 1)) * 20;
    const index = LESSON_ORDER.indexOf(screen);
    return index >= 0 ? index * 20 : 0;
  });

  readonly chatOk = computed(() => {
    const pick = this.chatPick();
    return pick !== null && !!this.lesson()?.chatOptions[pick]?.isCorrect;
  });

  readonly listenOk = computed(() => {
    const pick = this.listenPick();
    return pick !== null && !!this.lesson()?.listening.options[pick]?.isCorrect;
  });

  async init(data: LearnerData, start: Screen): Promise<void> {
    this.data = data;
    this.onboarded.set(start !== 'goal');
    this.screen.set(start);
    await this.refresh();
    // Someone who never said where they start (new, or signed up before this existed) is asked once.
    const today = this.today();
    if (start === 'home' && today && !today.onboardingDone) await this.openWelcome();
  }

  async openWelcome(): Promise<void> {
    await this.run(async () => {
      const onboarding = await this.data.onboarding();
      this.onboarding.set(onboarding);
      this.knowsLetters.set(onboarding.knowsLetters);
      this.startStep.set(onboarding.startStep);
    });
    if (!this.error()) this.go('welcome');
  }

  async saveWelcome(): Promise<void> {
    await this.run(() => this.data.setOnboarding(this.knowsLetters(), this.knowsLetters() ? this.startStep() : 1));
    if (!this.error()) await this.goHome();
  }

  async refresh(): Promise<void> {
    await this.run(async () => {
      const [today, goal] = await Promise.all([this.data.today(), this.data.goal()]);
      this.today.set(today);
      this.goal.set(goal.wordsPerDay);
      this.allowedGoals.set(goal.allowedValues);
    });
  }

  go(screen: Screen): void {
    this.error.set(null);
    this.screen.set(screen);
  }

  async goHome(): Promise<void> {
    this.go('home');
    await this.refresh();
  }

  async saveGoal(): Promise<void> {
    const firstTime = !this.onboarded();
    await this.run(async () => {
      await this.data.setGoal(this.goal());
      this.onboarded.set(true);
    });
    if (this.error()) return;
    // Right after signing up, the next question is where to start.
    if (firstTime) {
      await this.openWelcome();
      if (!this.error()) return;
    }
    await this.goHome();
  }

  /** Today's lesson: due reviews first (when there are any), then the new lesson. */
  async startLesson(): Promise<void> {
    const next = this.today()?.nextLesson;
    if (!next) return;

    await this.run(async () => {
      const reviewsOn = (this.today()?.reviewsEnabled ?? false) && next.track === 'main';
      const [lesson, cards] = await Promise.all([
        this.data.lesson(next.id),
        reviewsOn ? this.data.dueReviews() : Promise.resolve([]),
      ]);
      this.resetSession(lesson, cards, false);
      // Letters and the first words are not reviewed and have their own screen.
      if (lesson.track === 'foundations') {
        this.go('foundation');
        return;
      }
      this.go(cards.length > 0 ? 'review' : 'newword');
    });
  }

  /** Reviews only: when new lessons are paused, or from "My words". */
  async startReview(): Promise<void> {
    await this.run(async () => {
      const cards = await this.data.dueReviews();
      if (cards.length === 0) return;
      this.resetSession(null, cards, true);
      this.go('review');
    });
  }

  revealCard(): void {
    this.revealed.set(true);
  }

  async rateCard(rating: Rating): Promise<void> {
    const card = this.currentCard();
    if (!card) return;

    await this.run(() => this.data.rate(card.wordId, rating));
    if (this.error()) return;

    this.reviewedCount.update((count) => count + 1);
    if (this.cardIndex() < this.reviewCards().length - 1) {
      this.cardIndex.update((index) => index + 1);
      this.revealed.set(false);
    } else if (this.reviewOnly()) {
      await this.refresh();
      this.go('done');
    } else {
      this.go('newword');
    }
  }

  nextWord(): void {
    const total = this.lesson()?.words.length ?? 0;
    if (this.wordIndex() < total - 1) {
      this.wordIndex.update((index) => index + 1);
    } else {
      this.go('dialogue');
    }
  }

  pickChat(index: number): void {
    if (!this.chatChecked()) this.chatPick.set(index);
  }

  checkChat(): void {
    this.chatChecked.set(true);
  }

  retryChat(): void {
    this.chatPick.set(null);
    this.chatChecked.set(false);
  }

  pickListen(index: number): void {
    if (!this.listenChecked()) this.listenPick.set(index);
  }

  checkListen(): void {
    this.listenChecked.set(true);
  }

  retryListen(): void {
    this.listenPick.set(null);
    this.listenChecked.set(false);
  }

  startExercises(): void {
    this.exerciseIndex.set(0);
    this.retryExercise();
    this.foundationPhase.set('quiz');
  }

  pickExercise(index: number): void {
    if (!this.exerciseChecked()) this.exercisePick.set(index);
  }

  checkExercise(): void {
    this.exerciseChecked.set(true);
  }

  /** A wrong answer is never final: the learner tries again, so nobody is held back by a mistake. */
  retryExercise(): void {
    this.exercisePick.set(null);
    this.exerciseChecked.set(false);
  }

  /** The next question, or the end of the lesson after the last one. */
  async nextExercise(): Promise<void> {
    const total = this.lesson()?.exercises.length ?? 0;
    if (this.exerciseIndex() < total - 1) {
      this.exerciseIndex.update((index) => index + 1);
      this.retryExercise();
    } else {
      await this.finishLesson();
    }
  }

  /** The last exercise is done: save the lesson, its words join the reviews. */
  async finishLesson(): Promise<void> {
    const lesson = this.lesson();
    if (!lesson) return;

    await this.run(async () => {
      this.completion.set(await this.data.completeLesson(lesson.id));
      this.go('done');
    });
  }

  async loadWords(filter: WordFilter): Promise<void> {
    this.wordFilter.set(filter);
    await this.run(async () => this.words.set(await this.data.words(filter)));
  }

  private resetSession(lesson: Lesson | null, cards: ReviewCard[], reviewOnly: boolean): void {
    this.lesson.set(lesson);
    if (lesson) {
      this.speech.language = lesson.targetLanguage;
    }
    this.reviewCards.set(cards);
    this.reviewOnly.set(reviewOnly);
    this.reviewedCount.set(0);
    this.completion.set(null);
    this.cardIndex.set(0);
    this.revealed.set(false);
    this.wordIndex.set(0);
    this.retryChat();
    this.retryListen();
    this.foundationPhase.set('items');
    this.exerciseIndex.set(0);
    this.retryExercise();
  }

  /** Runs one data call: tracks loading, keeps server rendering waiting, turns failures into a message. */
  private async run(task: () => Promise<unknown>): Promise<void> {
    // Not PendingTasks.run: it reports failures to the global error handler instead of here.
    const taskDone = this.pendingTasks.add();
    this.loading.set(true);
    this.error.set(null);
    try {
      await task();
    } catch (error) {
      this.error.set(error instanceof Error ? error.message : 'حدث خطأ. حاول مرة أخرى.');
    } finally {
      this.loading.set(false);
      taskDone();
    }
  }
}
