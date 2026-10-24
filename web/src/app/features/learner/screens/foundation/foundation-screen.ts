import { Component, computed, inject, signal } from '@angular/core';
import { SpeechStyle } from '../../../../core/audio-id';
import { SpeechService } from '../../../../core/speech.service';
import { SpeakButton } from '../../../../shared/speak-button/speak-button';
import { LessonWord } from '../../learner.models';
import { LearnerStore, answerState, answerStep } from '../../learner.store';

/**
 * A foundations lesson (letters, numbers, greetings, first words): first all of its items on one
 * screen, to tap and hear in any order, then a practice with questions. Mistakes never hold the
 * learner back: a wrong answer is simply tried again.
 */
@Component({
  selector: 'app-foundation-screen',
  imports: [SpeakButton],
  templateUrl: './foundation-screen.html',
  styleUrl: './foundation-screen.css',
  host: { class: 'screen' },
})
export class FoundationScreen {
  protected readonly store = inject(LearnerStore);
  private readonly speech = inject(SpeechService);

  protected readonly lesson = computed(() => this.store.lesson()!);
  protected readonly items = computed(() => this.lesson().words);
  protected readonly selected = signal<LessonWord | null>(null);

  protected readonly exercise = computed(() => this.store.currentExercise());
  protected readonly total = computed(() => this.lesson().exercises.length);

  protected readonly options = computed(() =>
    (this.exercise()?.options ?? []).map((option, index) => ({
      text: option.text,
      latin: isLatin(option.text),
      state: answerState(this.store.exercisePick() === index, this.store.exerciseChecked(), option.isCorrect),
    })),
  );

  protected readonly step = computed(() =>
    answerStep(this.store.exercisePick(), this.store.exerciseChecked(), this.store.exerciseOk()),
  );

  protected readonly isLast = computed(() => this.store.exerciseIndex() >= this.total() - 1);

  /** Tapping an item shows it and says it: a letter by its name, anything else as written. */
  protected select(item: LessonWord): void {
    this.selected.set(item);
    this.speech.speak(item.target, false, item.isLetter ? 'letter' : '');
  }

  protected sound(item: LessonWord): void {
    this.speech.speak(item.target.toLowerCase(), false, 'sound');
  }

  protected symbolOf(item: LessonWord): string {
    return item.symbol || item.target;
  }

  protected styleOf(style: string): SpeechStyle {
    return style === 'letter' || style === 'sound' ? style : '';
  }

  protected primaryAction(): void {
    switch (this.step()) {
      case 'check':
        this.store.checkExercise();
        break;
      case 'retry':
        this.store.retryExercise();
        break;
      case 'next':
        void this.store.nextExercise();
        break;
    }
  }
}

/** Latin text (English words, letters, digits) reads left to right inside the Arabic page. */
function isLatin(text: string): boolean {
  return /^[A-Za-z0-9]/.test(text.trim());
}
