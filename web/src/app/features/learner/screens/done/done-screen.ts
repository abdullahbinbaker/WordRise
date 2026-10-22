import { Component, computed, inject } from '@angular/core';
import { ArabicDigitsPipe } from '../../../../core/arabic-digits.pipe';
import { LearnerStore } from '../../learner.store';

@Component({
  selector: 'app-done-screen',
  imports: [ArabicDigitsPipe],
  templateUrl: './done-screen.html',
  styleUrl: './done-screen.css',
  host: { class: 'screen' },
})
export class DoneScreen {
  protected readonly store = inject(LearnerStore);

  protected readonly title = computed(() => {
    const name = this.store.learnerName();
    return name ? `أحسنت يا ${name}!` : 'أحسنت!';
  });

  protected readonly subtitle = computed(() =>
    this.store.reviewOnly() ? 'أنهيت مراجعة اليوم.' : 'أنهيت درس اليوم كاملاً.',
  );

  protected readonly newWords = computed(() => this.store.completion()?.wordsAdded ?? 0);

  protected readonly streak = computed(() => this.store.completion()?.streak ?? this.store.today()?.streak ?? 0);
}
