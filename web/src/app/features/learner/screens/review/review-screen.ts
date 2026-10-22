import { Component, inject } from '@angular/core';
import { ArabicDigitsPipe } from '../../../../core/arabic-digits.pipe';
import { SpeakButton } from '../../../../shared/speak-button/speak-button';
import { Rating } from '../../learner.models';
import { LearnerStore } from '../../learner.store';

@Component({
  selector: 'app-review-screen',
  imports: [ArabicDigitsPipe, SpeakButton],
  templateUrl: './review-screen.html',
  styleUrl: './review-screen.css',
  host: { class: 'screen' },
})
export class ReviewScreen {
  protected readonly store = inject(LearnerStore);

  protected readonly ratings: readonly { rating: Rating; label: string; when: string }[] = [
    { rating: 'hard', label: 'صعبة', when: 'غداً' },
    { rating: 'good', label: 'جيدة', when: 'بعد ٣ أيام' },
    { rating: 'easy', label: 'سهلة', when: 'بعد أسبوع' },
  ];
}
