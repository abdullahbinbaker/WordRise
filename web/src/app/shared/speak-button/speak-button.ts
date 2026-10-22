import { Component, computed, inject, input } from '@angular/core';
import { SpeechService } from '../../core/speech.service';

@Component({
  selector: 'app-speak-button',
  template: `
    <button
      type="button"
      class="speak"
      [style.width.px]="size()"
      [style.height.px]="size()"
      [attr.aria-label]="label()"
      (click)="speech.speak(text())"
    >
      <svg [attr.width]="iconSize()" [attr.height]="iconSize()" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path d="M4 9v6h4l5 4V5L8 9H4z" fill="currentColor" />
        <path [attr.d]="wavesPath()" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
      </svg>
    </button>
  `,
  styles: `
    .speak {
      flex-shrink: 0;
      border-radius: 50%;
      border: none;
      background: var(--blue-soft);
      color: var(--blue);
      display: flex;
      align-items: center;
      justify-content: center;
    }
  `,
})
export class SpeakButton {
  protected readonly speech = inject(SpeechService);

  readonly text = input.required<string>();
  readonly size = input(48);
  readonly iconSize = input(22);
  readonly label = input('استمع');

  /** Large buttons show two sound waves, small ones show one. */
  protected readonly wavesPath = computed(() =>
    this.size() >= 64 ? 'M16 8.5a5 5 0 0 1 0 7M18.5 6a8.5 8.5 0 0 1 0 12' : 'M16 8.5a5 5 0 0 1 0 7',
  );
}
