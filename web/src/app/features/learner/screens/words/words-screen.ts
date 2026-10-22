import { Component, computed, inject } from '@angular/core';
import { ArabicDigitsPipe } from '../../../../core/arabic-digits.pipe';
import { SpeakButton } from '../../../../shared/speak-button/speak-button';
import { LearnerStore, WordFilter } from '../../learner.store';
import { MY_WORDS, WordStatus } from '../../lesson.data';

const STATUS_LABELS: Record<WordStatus, string> = {
  due: 'للمراجعة',
  new: 'جديدة',
  done: 'أتقنتها',
};

@Component({
  selector: 'app-words-screen',
  imports: [ArabicDigitsPipe, SpeakButton],
  templateUrl: './words-screen.html',
  styleUrl: './words-screen.css',
  host: { class: 'screen' },
})
export class WordsScreen {
  protected readonly store = inject(LearnerStore);
  protected readonly total = MY_WORDS.length;
  protected readonly dueCount = MY_WORDS.filter((word) => word.status === 'due').length;
  protected readonly statusLabels = STATUS_LABELS;

  protected readonly filters: readonly { key: WordFilter; label: string }[] = [
    { key: 'all', label: 'الكل' },
    { key: 'due', label: 'للمراجعة' },
    { key: 'done', label: 'أتقنتها' },
  ];

  protected readonly words = computed(() => {
    const filter = this.store.wordFilter();
    return MY_WORDS.filter((word) => filter === 'all' || word.status === filter);
  });
}
