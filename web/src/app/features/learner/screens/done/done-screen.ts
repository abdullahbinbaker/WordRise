import { Component, inject } from '@angular/core';
import { ArabicDigitsPipe } from '../../../../core/arabic-digits.pipe';
import { LearnerStore } from '../../learner.store';
import { NEW_WORDS, REVIEW_CARDS } from '../../lesson.data';

@Component({
  selector: 'app-done-screen',
  imports: [ArabicDigitsPipe],
  templateUrl: './done-screen.html',
  styleUrl: './done-screen.css',
  host: { class: 'screen' },
})
export class DoneScreen {
  protected readonly store = inject(LearnerStore);
  protected readonly newCount = NEW_WORDS.length;
  protected readonly reviewedCount = REVIEW_CARDS.length;
  // Sample streak until the progress API exists.
  protected readonly streakDays = 6;
}
