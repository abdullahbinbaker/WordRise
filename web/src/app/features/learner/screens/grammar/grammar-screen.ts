import { Component, computed, inject } from '@angular/core';
import { SpeakButton } from '../../../../shared/speak-button/speak-button';
import { LearnerStore } from '../../learner.store';

@Component({
  selector: 'app-grammar-screen',
  imports: [SpeakButton],
  templateUrl: './grammar-screen.html',
  styleUrl: './grammar-screen.css',
  host: { class: 'screen' },
})
export class GrammarScreen {
  protected readonly store = inject(LearnerStore);
  protected readonly grammar = computed(() => this.store.lesson()!.grammar);

  /** The first example split into word chips, the grammar word highlighted: I | can | read. */
  protected readonly chips = computed(() => {
    const { title, examples } = this.grammar();
    const words = (examples[0]?.english ?? '').replace(/[.?!]/g, '').split(/\s+/).filter(Boolean);
    return words.map((text) => ({ text, key: text.toLowerCase() === title.toLowerCase() }));
  });
}
