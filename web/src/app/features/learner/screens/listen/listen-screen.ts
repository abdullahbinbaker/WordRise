import { Component, computed, inject } from '@angular/core';
import { SpeechService } from '../../../../core/speech.service';
import { LearnerStore, answerState, answerStep } from '../../learner.store';

@Component({
  selector: 'app-listen-screen',
  templateUrl: './listen-screen.html',
  styleUrl: './listen-screen.css',
  host: { class: 'screen' },
})
export class ListenScreen {
  protected readonly store = inject(LearnerStore);
  private readonly speech = inject(SpeechService);

  protected readonly listening = computed(() => this.store.lesson()!.listening);

  protected readonly options = computed(() =>
    this.listening().options.map((option, index) => ({
      text: option.text,
      state: answerState(this.store.listenPick() === index, this.store.listenChecked(), option.isCorrect),
    })),
  );

  protected readonly step = computed(() =>
    answerStep(this.store.listenPick(), this.store.listenChecked(), this.store.listenOk()),
  );

  protected play(slow = false): void {
    this.speech.speak(this.listening().sentence, slow);
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
        void this.store.finishLesson();
        break;
    }
  }
}
