import { Component, computed, inject } from '@angular/core';
import { SpeechService } from '../../../../core/speech.service';
import { LearnerStore, answerState, answerStep } from '../../learner.store';
import { LISTEN_ANSWER, LISTEN_OPTIONS, LISTEN_SENTENCE } from '../../lesson.data';

@Component({
  selector: 'app-listen-screen',
  templateUrl: './listen-screen.html',
  styleUrl: './listen-screen.css',
  host: { class: 'screen' },
})
export class ListenScreen {
  protected readonly store = inject(LearnerStore);
  private readonly speech = inject(SpeechService);

  protected readonly sentence = LISTEN_SENTENCE;

  protected readonly options = computed(() =>
    LISTEN_OPTIONS.map((en, index) => ({
      en,
      state: answerState(this.store.listenPick() === index, this.store.listenChecked(), en === LISTEN_ANSWER),
    })),
  );

  protected readonly step = computed(() =>
    answerStep(this.store.listenPick(), this.store.listenChecked(), this.store.listenOk()),
  );

  protected play(slow = false): void {
    this.speech.speak(LISTEN_SENTENCE, slow);
  }

  protected primaryAction(): void {
    switch (this.step()) {
      case 'check':
        this.store.checkListen();
        break;
      case 'retry':
        this.store.retryListen();
        break;
      case 'next':
        this.store.go('done');
        break;
    }
  }
}
