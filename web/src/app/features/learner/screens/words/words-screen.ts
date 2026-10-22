import { Component, OnInit, computed, inject } from '@angular/core';
import { ArabicDigitsPipe } from '../../../../core/arabic-digits.pipe';
import { SpeakButton } from '../../../../shared/speak-button/speak-button';
import { WordFilter, WordStatus } from '../../learner.models';
import { LearnerStore } from '../../learner.store';

const STATUS_LABELS: Record<WordStatus, string> = {
  new: 'جديدة',
  due: 'للمراجعة',
  learning: 'أتعلّمها',
  mastered: 'أتقنتها',
};

@Component({
  selector: 'app-words-screen',
  imports: [ArabicDigitsPipe, SpeakButton],
  templateUrl: './words-screen.html',
  styleUrl: './words-screen.css',
  host: { class: 'screen' },
})
export class WordsScreen implements OnInit {
  protected readonly store = inject(LearnerStore);
  protected readonly statusLabels = STATUS_LABELS;

  protected readonly filters: readonly { key: WordFilter; label: string }[] = [
    { key: 'all', label: 'الكل' },
    { key: 'due', label: 'للمراجعة' },
    { key: 'mastered', label: 'أتقنتها' },
  ];

  protected readonly dueCount = computed(() => this.store.today()?.dueReviewCount ?? 0);
  protected readonly canReview = computed(() => !!this.store.today()?.reviewsEnabled && this.dueCount() > 0);

  ngOnInit(): void {
    void this.store.loadWords(this.store.wordFilter());
  }
}
