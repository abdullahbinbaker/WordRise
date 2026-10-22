import { Component, computed, inject } from '@angular/core';
import { SpeechService } from '../../../../core/speech.service';
import { SpeakButton } from '../../../../shared/speak-button/speak-button';
import { LearnerStore, answerState, answerStep } from '../../learner.store';
import { CHAT_OPTIONS, GROCERY_CHAT } from '../../lesson.data';

@Component({
  selector: 'app-chat-screen',
  imports: [SpeakButton],
  templateUrl: './chat-screen.html',
  styleUrl: './chat-screen.css',
  host: { class: 'screen' },
})
export class ChatScreen {
  protected readonly store = inject(LearnerStore);
  private readonly speech = inject(SpeechService);

  protected readonly lines = GROCERY_CHAT;

  protected readonly options = computed(() =>
    CHAT_OPTIONS.map((option, index) => ({
      ...option,
      state: answerState(this.store.chatPick() === index, this.store.chatChecked(), option.ok),
    })),
  );

  protected readonly step = computed(() =>
    answerStep(this.store.chatPick(), this.store.chatChecked(), this.store.chatOk()),
  );

  protected pick(index: number): void {
    if (this.store.chatChecked()) return;
    this.store.pickChat(index);
    this.speech.speak(CHAT_OPTIONS[index].en);
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
        this.store.go('can');
        break;
    }
  }
}
