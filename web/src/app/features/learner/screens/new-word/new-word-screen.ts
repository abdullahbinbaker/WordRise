import { Component, computed, inject } from '@angular/core';
import { ArabicDigitsPipe } from '../../../../core/arabic-digits.pipe';
import { SpeakButton } from '../../../../shared/speak-button/speak-button';
import { LearnerStore } from '../../learner.store';

@Component({
  selector: 'app-new-word-screen',
  imports: [ArabicDigitsPipe, SpeakButton],
  templateUrl: './new-word-screen.html',
  styleUrl: './new-word-screen.css',
  host: { class: 'screen' },
})
export class NewWordScreen {
  protected readonly store = inject(LearnerStore);
  protected readonly total = computed(() => this.store.lesson()?.words.length ?? 0);
  protected readonly isLast = computed(() => this.store.wordIndex() === this.total() - 1);
}
