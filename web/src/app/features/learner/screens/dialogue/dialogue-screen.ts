import { Component, computed, inject } from '@angular/core';
import { SpeechService } from '../../../../core/speech.service';
import { SpeakButton } from '../../../../shared/speak-button/speak-button';
import { LearnerStore, answerState, answerStep } from '../../learner.store';

@Component({
  selector: 'app-dialogue-screen',
  imports: [SpeakButton],
  templateUrl: './dialogue-screen.html',
  styleUrl: './dialogue-screen.css',
  host: { class: 'screen' },
})
export class DialogueScreen {
  protected readonly store = inject(LearnerStore);
  private readonly speech = inject(SpeechService);

  protected readonly lesson = computed(() => this.store.lesson()!);

  protected readonly options = computed(() =>
    this.lesson().chatOptions.map((option, index) => ({
      ...option,
      state: answerState(this.store.chatPick() === index, this.store.chatChecked(), option.isCorrect),
    })),
  );

  protected readonly correctAnswer = computed(() => this.lesson().chatOptions.find((o) => o.isCorrect)?.text ?? '');

  protected readonly step = computed(() =>
    answerStep(this.store.chatPick(), this.store.chatChecked(), this.store.chatOk()),
  );

  protected pick(index: number): void {
    if (this.store.chatChecked()) return;
    this.store.pickChat(index);
    this.speech.speak(this.lesson().chatOptions[index].text);
  }

  protected primaryAction(): void {
    switch (this.step()) {
      case 'check':
        this.store.checkChat();
        break;
      case 'retry':
        this.store.retryChat();
        break;
      case 'next':
        this.store.go('grammar');
        break;
    }
  }
}
