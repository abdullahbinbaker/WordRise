import { Component, computed, inject } from '@angular/core';
import { ArabicDigitsPipe } from '../../../../core/arabic-digits.pipe';
import { SpeakButton } from '../../../../shared/speak-button/speak-button';
import { LearnerStore } from '../../learner.store';
import { REVIEW_CARDS, REVIEW_RATINGS } from '../../lesson.data';

@Component({
  selector: 'app-review-screen',
  imports: [ArabicDigitsPipe, SpeakButton],
  templateUrl: './review-screen.html',
  styleUrl: './review-screen.css',
  host: { class: 'screen' },
})
export class ReviewScreen {
  protected readonly store = inject(LearnerStore);
  protected readonly total = REVIEW_CARDS.length;
  protected readonly ratings = REVIEW_RATINGS;
  protected readonly card = computed(() => REVIEW_CARDS[this.store.cardIndex()]);
}
