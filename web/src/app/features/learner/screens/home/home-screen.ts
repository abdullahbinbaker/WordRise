import { Component, computed, inject } from '@angular/core';
import { ArabicDigitsPipe, toArabicDigits } from '../../../../core/arabic-digits.pipe';
import { LearnerStore } from '../../learner.store';
import { LESSON_STEPS, REVIEW_CARDS } from '../../lesson.data';

@Component({
  selector: 'app-home-screen',
  imports: [ArabicDigitsPipe],
  templateUrl: './home-screen.html',
  styleUrl: './home-screen.css',
  host: { class: 'screen' },
})
export class HomeScreen {
  protected readonly store = inject(LearnerStore);
  protected readonly steps = LESSON_STEPS;

  // Sample learner stats until the progress API exists.
  protected readonly streak = computed(() => (this.store.lessonDone() ? '٦ أيام' : '٥ أيام'));
  protected readonly learnedCount = computed(() => (this.store.lessonDone() ? 45 : 42));
  protected readonly dueCount = computed(() => (this.store.lessonDone() ? 0 : REVIEW_CARDS.length));

  protected readonly progressLabel = computed(() =>
    this.store.lessonDone()
      ? 'اكتمل درس اليوم'
      : `٥ أجزاء · حوالي ${toArabicDigits(this.store.goal())} دقائق`,
  );

  protected primaryAction(): void {
    if (this.store.lessonDone()) {
      this.store.go('words');
    } else {
      this.store.startLesson();
    }
  }
}
