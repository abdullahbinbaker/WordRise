import { Injectable, computed, signal } from '@angular/core';
import {
  CHAT_OPTIONS,
  LISTEN_ANSWER,
  LISTEN_OPTIONS,
  NEW_WORDS,
  REVIEW_CARDS,
  WordStatus,
} from './lesson.data';

export type Screen = 'goal' | 'home' | 'review' | 'newword' | 'grocery' | 'can' | 'listen' | 'done' | 'words';
export type WordFilter = 'all' | Exclude<WordStatus, 'new'>;
export type AnswerState = 'idle' | 'selected' | 'right' | 'wrong';
/** What the primary button of a quiz screen does next. */
export type AnswerStep = 'choose' | 'check' | 'retry' | 'next';

/** Lesson screens in order; each of the first five is 20% of the progress bar. */
export const LESSON_ORDER: readonly Screen[] = ['review', 'newword', 'grocery', 'can', 'listen', 'done'];

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
  readonly screen = signal<Screen>('goal');
  readonly onboarded = signal(false);
  readonly goal = signal(10);
  readonly lessonDone = signal(false);

  readonly cardIndex = signal(0);
  readonly revealed = signal(false);
  readonly wordIndex = signal(0);
  readonly chatPick = signal<number | null>(null);
  readonly chatChecked = signal(false);
  readonly listenPick = signal<number | null>(null);
  readonly listenChecked = signal(false);
  readonly wordFilter = signal<WordFilter>('all');

  readonly inLesson = computed(() => LESSON_ORDER.includes(this.screen()) && this.screen() !== 'done');

  readonly lessonProgress = computed(() => {
    const screen = this.screen();
    if (screen === 'review') {
      return ((this.cardIndex() + (this.revealed() ? 0.5 : 0)) / REVIEW_CARDS.length) * 20;
    }
    if (screen === 'newword') {
      return 20 + (this.wordIndex() / NEW_WORDS.length) * 20;
    }
    const index = LESSON_ORDER.indexOf(screen);
    return index >= 0 ? index * 20 : 0;
  });

  readonly chatOk = computed(() => {
    const pick = this.chatPick();
    return pick !== null && CHAT_OPTIONS[pick].ok;
  });

  readonly listenOk = computed(() => {
    const pick = this.listenPick();
    return pick !== null && LISTEN_OPTIONS[pick] === LISTEN_ANSWER;
  });

  start(screen: Screen): void {
    this.screen.set(screen);
    this.onboarded.set(screen !== 'goal');
  }

  go(screen: Screen): void {
    this.screen.set(screen);
  }

  finishOnboarding(): void {
    this.onboarded.set(true);
    this.go('home');
  }

  startLesson(): void {
    this.cardIndex.set(0);
    this.revealed.set(false);
    this.wordIndex.set(0);
    this.retryChat();
    this.retryListen();
    this.go('review');
  }

  startReview(): void {
    this.cardIndex.set(0);
    this.revealed.set(false);
    this.go('review');
  }

  revealCard(): void {
    this.revealed.set(true);
  }

  rateCard(): void {
    if (this.cardIndex() < REVIEW_CARDS.length - 1) {
      this.cardIndex.update((index) => index + 1);
      this.revealed.set(false);
    } else {
      this.wordIndex.set(0);
      this.go('newword');
    }
  }

  nextWord(): void {
    if (this.wordIndex() < NEW_WORDS.length - 1) {
      this.wordIndex.update((index) => index + 1);
    } else {
      this.go('grocery');
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

  finishLesson(): void {
    this.lessonDone.set(true);
    this.go('home');
  }
}
