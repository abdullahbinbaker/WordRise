import { Injectable, PendingTasks, computed, inject, signal } from '@angular/core';
import { SpeechService } from '../../core/speech.service';
import { CompleteResult, LearnerData, Lesson, MyWord, Rating, ReviewCard, Today, WordFilter } from './learner.models';

export type Screen = 'goal' | 'home' | 'review' | 'newword' | 'dialogue' | 'grammar' | 'listen' | 'done' | 'words';
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

  readonly currentCard = computed(() => this.reviewCards()[this.cardIndex()] ?? null);
  readonly currentWord = computed(() => this.lesson()?.words[this.wordIndex()] ?? null);
  readonly inLesson = computed(() => LESSON_ORDER.includes(this.screen()) && this.screen() !== 'done');

  readonly lessonProgress = computed(() => {
    const screen = this.screen();
    const cardProgress =
      (this.cardIndex() + (this.revealed() ? 0.5 : 0)) / Math.max(this.reviewCards().length, 1);
    if (this.reviewOnly()) {
      return screen === 'review' ? cardProgress * 100 : 0;
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
    await this.run(async () => {
      await this.data.setGoal(this.goal());
      this.onboarded.set(true);
    });
    if (!this.error()) await this.goHome();
  }

  /** Today's lesson: due reviews first (when there are any), then the new lesson. */
  async startLesson(): Promise<void> {
    const next = this.today()?.nextLesson;
    if (!next) return;

    await this.run(async () => {
      const reviewsOn = this.today()?.reviewsEnabled ?? false;
      const [lesson, cards] = await Promise.all([
        this.data.lesson(next.id),
        reviewsOn ? this.data.dueReviews() : Promise.resolve([]),
      ]);
      this.resetSession(lesson, cards, false);
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
