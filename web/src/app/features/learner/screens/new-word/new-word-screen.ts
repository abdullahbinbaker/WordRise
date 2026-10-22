import { Component, computed, inject } from '@angular/core';
import { ArabicDigitsPipe } from '../../../../core/arabic-digits.pipe';
import { SpeakButton } from '../../../../shared/speak-button/speak-button';
import { LearnerStore } from '../../learner.store';
import { NEW_WORDS } from '../../lesson.data';

@Component({
  selector: 'app-new-word-screen',
  imports: [ArabicDigitsPipe, SpeakButton],
  templateUrl: './new-word-screen.html',
  styleUrl: './new-word-screen.css',
  host: { class: 'screen' },
})
export class NewWordScreen {
  protected readonly store = inject(LearnerStore);
  protected readonly total = NEW_WORDS.length;
  protected readonly word = computed(() => NEW_WORDS[this.store.wordIndex()]);
  protected readonly isLast = computed(() => this.store.wordIndex() === NEW_WORDS.length - 1);
}
