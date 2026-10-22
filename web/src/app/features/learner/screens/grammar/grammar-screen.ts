import { Component, inject } from '@angular/core';
import { SpeakButton } from '../../../../shared/speak-button/speak-button';
import { LearnerStore } from '../../learner.store';
import { CAN_EXAMPLES } from '../../lesson.data';

@Component({
  selector: 'app-grammar-screen',
  imports: [SpeakButton],
  templateUrl: './grammar-screen.html',
  styleUrl: './grammar-screen.css',
  host: { class: 'screen' },
})
export class GrammarScreen {
  protected readonly store = inject(LearnerStore);
  protected readonly examples = CAN_EXAMPLES;
}
